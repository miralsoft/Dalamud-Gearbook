using Gearbook.Adapters;
using Gearbook.Configuration;
using Gearbook.Core.Identity;
using Gearbook.Core.Localization;
using Gearbook.Core.Model;
using Gearbook.Core.Settings;
using Gearbook.Core.Sorting;
using Gearbook.Services;

namespace Gearbook;

/// <summary>
/// What the interface draws from, and the only place the game is read.
/// </summary>
/// <remarks>
/// <para>
/// Game memory is read on the framework thread into an immutable list, and the draw callback
/// reads that list and nothing else (GB-03). A click does not call anything; it records an
/// intent that the next framework tick carries out. That is not caution about performance, it is
/// the only arrangement that survives a hot reload happening while a window is open.
/// </para>
/// <para>
/// Re-reading happens when something can have changed rather than every frame. The slow tick
/// underneath is the net: the game has ways of changing a gearset that this plugin cannot
/// observe directly, and a list that is three seconds stale is better than one that is stale
/// until the next login.
/// </para>
/// </remarks>
internal sealed class GearbookState : IDisposable
{
    /// <summary>
    /// How long between the unconditional re-reads that sit underneath the event-driven ones.
    /// Three seconds is far below what a person notices in a window they opened deliberately and
    /// far above what costs anything on a frame.
    /// </summary>
    private static readonly TimeSpan SlowRefreshInterval = TimeSpan.FromSeconds(3);

    private readonly IGearsetReader reader;
    private readonly IGearsetEquipper equipper;
    private readonly IGearsetArranger arranger;
    private readonly IGameStateProbe gameState;
    private readonly IJobDataSource jobData;
    private readonly IBisProvider bis;
    private readonly ConfigurationStore configuration;
    private readonly Localizer localizer;

    private readonly Queue<EquipRequest> pending = new();
    private readonly object gate = new();

    private Dictionary<int, uint> icons = [];
    private IReadOnlyList<string>? pendingArrange;
    private DateTime lastRefresh = DateTime.MinValue;
    private bool refreshRequested = true;
    private bool tearingDown;
    private string lastListSignature = string.Empty;

    public GearbookState(
        IGearsetReader reader,
        IGearsetEquipper equipper,
        IGearsetArranger arranger,
        IGameStateProbe gameState,
        IJobDataSource jobData,
        IBisProvider bis,
        ConfigurationStore configuration,
        Localizer localizer)
    {
        this.reader = reader;
        this.equipper = equipper;
        this.arranger = arranger;
        this.gameState = gameState;
        this.jobData = jobData;
        this.bis = bis;
        this.configuration = configuration;
        this.localizer = localizer;
    }

    /// <summary>The gearsets as of the last read. Safe to enumerate from the draw callback.</summary>
    public IReadOnlyList<ReconciledGearset> Gearsets { get; private set; } = [];

    /// <summary>Records with no gearset behind them right now. Kept, never deleted.</summary>
    public IReadOnlyList<GearsetRecord> Orphans { get; private set; } = [];

    /// <summary>The job table.</summary>
    public IReadOnlyDictionary<uint, JobInfo> Jobs => jobData.Jobs;

    /// <summary>The gearset currently worn, by the game's own number, or null.</summary>
    public int? CurrentSlot { get; private set; }

    /// <summary>The settings for the character currently logged in, or null when logged out.</summary>
    public CharacterSettings? Character { get; private set; }

    /// <summary>The active character's own content id, or null.</summary>
    public ulong? CharacterId { get; private set; }

    /// <summary>Whether this is the first time the plugin has seen this character.</summary>
    public bool IsFirstSightOfCharacter { get; private set; }

    /// <summary>The interface language.</summary>
    public Localizer Loc => localizer;

    /// <summary>The last best-in-slot answer. Unavailable until something answers.</summary>
    public Core.Bis.BisSnapshot Bis => bis.Current;

    /// <summary>The game state, for greying controls out with a reason.</summary>
    public IGameStateProbe GameState => gameState;

    /// <summary>
    /// The icon the game itself uses for a gearset, as collected on the last read.
    /// </summary>
    /// <remarks>
    /// A lookup rather than a call into the game, because the only caller is the draw callback
    /// and that reads this plugin's own lists and nothing else (GB-03). Zero for a slot that was
    /// not in the last read, which the tile already handles by drawing the number instead.
    /// </remarks>
    public uint IconFor(int slot) => icons.TryGetValue(slot, out var icon) ? icon : 0u;

    /// <summary>The host's symbol font, for the controls that are not gearsets.</summary>
    public static Dalamud.Interface.ManagedFontAtlas.IFontHandle IconFont =>
        GearbookServices.PluginInterface.UiBuilder.IconFontHandle;

    /// <summary>Asks for a re-read on the next tick.</summary>
    public void RequestRefresh() => refreshRequested = true;

    /// <summary>
    /// Asks for a gearset to be equipped. Records the intent; the change happens on the next
    /// framework tick, because this is called from the draw callback.
    /// </summary>
    public void RequestEquip(int slot, EquipTrigger trigger)
    {
        lock (gate)
        {
            pending.Enqueue(new EquipRequest(slot, trigger));
        }
    }

    /// <summary>
    /// Why a gearset cannot be equipped right now, or <see cref="EquipOutcome.Sent"/>. The same
    /// answer the equipper will give, so the tooltip and the log cannot disagree.
    /// </summary>
    public EquipOutcome CheckCanEquip(ReconciledGearset gearset) =>
        equipper.CheckCanEquip(gearset.Gearset.Slot);

    /// <summary>Saves the configuration.</summary>
    public void Save() => configuration.Save();

    /// <summary>
    /// Re-resolves the interface language from the player's choice and the host's setting.
    /// </summary>
    /// <remarks>
    /// Called when the player changes the setting and when the host raises its own language
    /// change. The handler for the latter only reaches this when the setting is on automatic,
    /// because otherwise an explicit choice would be silently overwritten the next time the
    /// player changed the host's language, which reads as the plugin forgetting a setting.
    /// </remarks>
    public void ApplyLanguage() =>
        localizer.SetLanguage(
            Character?.Language ?? LanguageResolver.Automatic,
            GearbookServices.PluginInterface.UiLanguage);

    /// <summary>
    /// Applies a change to the current character's records and saves. Everything the interface
    /// edits goes through here, so that normalising the bar and writing the file happen in one
    /// place rather than being remembered at eight call sites.
    /// </summary>
    public void UpdateRecords(Func<IReadOnlyList<GearsetRecord>, IReadOnlyList<GearsetRecord>> change)
    {
        ArgumentNullException.ThrowIfNull(change);

        var character = Character;
        if (character is null)
        {
            return;
        }

        var updated = BarOrder.Normalise([.. change(character.ToRecords())]);
        character.SetRecords(updated);
        configuration.Save();

        // The lists the interface is drawing from now hold stale records, so rebuild them from
        // what was just saved rather than waiting for the next read of the game.
        Reattach(updated);
    }

    /// <summary>
    /// The framework tick. The only place game memory is read and the only place a gearset is
    /// changed.
    /// </summary>
    public void OnTick()
    {
        if (tearingDown)
        {
            return;
        }

        try
        {
            ProcessPendingRequests();
            ProcessPendingArrange();

            if (!gameState.IsLoggedIn)
            {
                if (Character is not null)
                {
                    Character = null;
                    CharacterId = null;
                    Gearsets = [];
                    icons = [];
                    Orphans = [];
                    CurrentSlot = null;
                }

                return;
            }

            var due = refreshRequested || DateTime.UtcNow - lastRefresh > SlowRefreshInterval;
            if (!due)
            {
                return;
            }

            refreshRequested = false;
            lastRefresh = DateTime.UtcNow;

            Refresh();
        }
        catch (Exception ex)
        {
            // The framework thread is the host's. An exception escaping here is a crash, not a
            // logged error, so the boundary catches everything and reports it.
            GearbookServices.Log.Error(ex, "The gearset update failed. The previous list is kept.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Set first, so that anything still reading stops before the rest of the teardown runs.
        tearingDown = true;

        lock (gate)
        {
            pending.Clear();
        }

        Gearsets = [];
        Orphans = [];
    }

    /// <summary>
    /// Asks for the game's own gearset list to be sorted into this order.
    /// </summary>
    /// <remarks>
    /// Recorded rather than done, like every other thing this plugin asks of the game, because
    /// the caller is a draw callback and a draw callback does not touch game memory (GB-03).
    /// It matters more here than for a gearset change: this one runs a loop of reads and writes,
    /// and a hot reload landing in the middle of it would leave the list half sorted.
    /// </remarks>
    public void RequestArrange(IReadOnlyList<string> wantedOrder)
    {
        ArgumentNullException.ThrowIfNull(wantedOrder);

        lock (gate)
        {
            pendingArrange = [.. wantedOrder];
        }
    }

    /// <summary>How the last rearrangement of the game's list ended, or null if none has run.</summary>
    public ArrangeResult? LastArrangeResult { get; private set; }

    /// <summary>
    /// Whether the game's list could be sorted right now, or what is preventing it.
    /// </summary>
    /// <remarks>
    /// Safe from the draw callback: it reads the host's condition flags and never the game's own
    /// memory. The arranger asks the same question again on the framework thread, because the
    /// answer can change between a player reading a button and pressing it.
    /// </remarks>
    public ArrangeOutcome CanArrange() => arranger.CanArrange();

    private void ProcessPendingArrange()
    {
        IReadOnlyList<string>? wanted;

        lock (gate)
        {
            wanted = pendingArrange;
            pendingArrange = null;
        }

        if (wanted is null)
        {
            return;
        }

        LastArrangeResult = arranger.Arrange(wanted);
        refreshRequested = true;
    }

    private void ProcessPendingRequests()
    {
        while (true)
        {
            EquipRequest request;

            lock (gate)
            {
                if (pending.Count == 0)
                {
                    return;
                }

                request = pending.Dequeue();
            }

            var outcome = equipper.Equip(request.Slot, request.Trigger);

            if (outcome == EquipOutcome.Sent)
            {
                RecordUse(request.Slot);
                refreshRequested = true;
            }
        }
    }

    private void RecordUse(int slot)
    {
        var character = Character;
        if (character is null)
        {
            return;
        }

        var match = Gearsets.FirstOrDefault(g => g.Gearset.Slot == slot);
        if (match is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var records = character.ToRecords()
            .Select(r => r.Id == match.Record.Id ? r with { LastUsedUtc = now } : r)
            .ToList();

        character.SetRecords(records);
        configuration.Save();
        Reattach(records);
    }

    private void Refresh()
    {
        var contentId = reader.CharacterContentId();
        if (contentId is null)
        {
            return;
        }

        if (CharacterId != contentId)
        {
            IsFirstSightOfCharacter = !configuration.Settings.Knows(contentId.Value);
            CharacterId = contentId;
            Character = configuration.Settings.For(contentId.Value);
            lastListSignature = string.Empty;
        }

        var character = Character;
        if (character is null)
        {
            return;
        }

        var gearsets = GearsetReader.ReadSafely(reader);
        CurrentSlot = reader.CurrentSlot();

        // The icons are collected here, on the framework thread, rather than asked for while
        // drawing. They come out of the same module as everything else, and the draw callback
        // reads this plugin's own lists and nothing else (GB-03). Asked for during a frame it
        // was also one game call per tile per frame, which is the wrong cost in the wrong place
        // for a number that changes only when the list does.
        icons = gearsets.ToDictionary(g => g.Slot, g => jobData.IconIdFor(g.Slot));

        // The job order is kept to the jobs this character actually has gearsets for, which is
        // what makes the setting a short list somebody can work with rather than forty rows of
        // which thirty do nothing. Done here rather than at load, because it depends on the
        // gearsets and those are only known once the game has been read.
        //
        // An unarranged job arrives in the game's own list position, the one the character
        // window uses, rather than alphabetically. Read from the job table, so a job added in a
        // later patch takes its place without anything here changing.
        character.NormaliseJobOrder(gearsets
            .Select(g => g.ClassJobId)
            .Distinct()
            .OrderBy(id => jobData.Jobs.TryGetValue(id, out var job) ? job.SortablePriority : int.MaxValue)
            .ThenBy(id => id));

        var result = GearsetReconciler.Reconcile(
            character.ToRecords(),
            gearsets,
            character.NextGearsetId,
            DateTimeOffset.UtcNow);

        foreach (var ambiguity in result.Ambiguities)
        {
            GearbookServices.Log.Warning("{Ambiguity}", ambiguity);
        }

        Gearsets = result.Present;
        Orphans = result.Orphans;

        var records = BarOrder.Normalise(result.AllRecords);

        var changed = character.NextGearsetId != result.NextId
                      || !records.SequenceEqual(character.ToRecords());

        character.NextGearsetId = result.NextId;

        if (changed)
        {
            character.SetRecords(records);
            configuration.Save();
            Reattach(records);
        }

        // The best-in-slot answer is keyed by the game's gearset number, so any change to the
        // list makes every key suspect. A wrong badge is worse than no badge: it looks like an
        // answer.
        var signature = string.Join(
            '|',
            gearsets.Select(g => $"{g.Slot}:{g.ClassJobId}:{g.Name}"));

        if (!string.Equals(signature, lastListSignature, StringComparison.Ordinal))
        {
            lastListSignature = signature;
            bis.Invalidate();
        }

        bis.RefreshIfStale();
    }

    /// <summary>
    /// Rebuilds the drawn list against a new set of records without touching the game again.
    /// </summary>
    private void Reattach(IReadOnlyList<GearsetRecord> records)
    {
        var byId = records.ToDictionary(r => r.Id);

        Gearsets =
        [
            .. Gearsets.Select(g =>
                byId.TryGetValue(g.Record.Id, out var updated)
                    ? g with { Record = updated }
                    : g)
        ];

        var present = Gearsets.Select(g => g.Record.Id).ToHashSet();
        Orphans = [.. records.Where(r => !present.Contains(r.Id))];
    }

    private readonly record struct EquipRequest(int Slot, EquipTrigger Trigger);
}
