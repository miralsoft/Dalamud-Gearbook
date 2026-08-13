namespace Gearbook.Core.Bis;

/// <summary>
/// What the best-in-slot provider has to say about itself.
/// </summary>
/// <remarks>
/// The state exists because a single absent badge and an entirely absent column mean different
/// things. For one gearset, "no account", "no target for this job", "unknown set" and "still
/// loading" all correctly mean no badge. For every gearset at once they do not: without a state,
/// an empty column cannot be told apart from an absent account, and a feature that switches
/// itself off has to say so somewhere the player can find.
/// </remarks>
public enum BisProviderState
{
    /// <summary>No provider is installed, or it is not answering. Nothing is shown, anywhere.</summary>
    Unavailable = 0,

    /// <summary>Answering, with data.</summary>
    Ok,

    /// <summary>Installed and answering, but no account is connected.</summary>
    NoAccount,

    /// <summary>Installed and answering, but still loading.</summary>
    Loading,

    /// <summary>Installed, connected, and no targets are set up.</summary>
    NoData,
}

/// <summary>
/// How far one gearset is from its target, as the provider reports it.
/// </summary>
/// <remarks>
/// This plugin computes none of this. Rebuilding the rules on this side, which slot counts, how
/// rings are handled in pairs, when a piece is close enough, would create a second truth that
/// drifts from the first.
/// </remarks>
/// <param name="Slot">The gearset this refers to, by the game's own number.</param>
/// <param name="Matched">Pieces already matching the target.</param>
/// <param name="Total">Pieces the target names.</param>
/// <param name="Target">What the target is called, for the tooltip.</param>
public sealed record BisEntry(int Slot, int Matched, int Total, string Target);

/// <summary>
/// One answer from the provider: its state, plus whatever it knows, keyed by gearset number.
/// </summary>
public sealed class BisSnapshot
{
    /// <summary>Nothing installed, nothing to show. The state every session starts in.</summary>
    public static BisSnapshot Unavailable { get; } =
        new(BisProviderState.Unavailable, new Dictionary<int, BisEntry>());

    /// <summary>Creates a snapshot.</summary>
    public BisSnapshot(BisProviderState state, IReadOnlyDictionary<int, BisEntry> entriesBySlot)
    {
        ArgumentNullException.ThrowIfNull(entriesBySlot);

        State = state;
        EntriesBySlot = entriesBySlot;
    }

    /// <summary>What the provider says about itself.</summary>
    public BisProviderState State { get; }

    /// <summary>What it knows, by gearset number.</summary>
    public IReadOnlyDictionary<int, BisEntry> EntriesBySlot { get; }

    /// <summary>The entry for a gearset, or null. Null is the normal case, not an error.</summary>
    public BisEntry? For(int slot) =>
        EntriesBySlot.TryGetValue(slot, out var entry) ? entry : null;

    /// <summary>
    /// True when there is at least one badge to draw. What the library window asks before it
    /// gives the column any width at all (GB-06).
    /// </summary>
    public bool HasAnything => EntriesBySlot.Count > 0;
}
