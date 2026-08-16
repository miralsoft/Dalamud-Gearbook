using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Gearbook.Adapters;
using Gearbook.Core.Arranging;
using Gearbook.Core.Filtering;
using Gearbook.Core.Localization;
using Gearbook.Core.Settings;

namespace Gearbook.UI;

/// <summary>
/// Sorts the game's own gearset list, on request.
/// </summary>
/// <remarks>
/// <para>
/// A window of its own rather than a button in the library, because this is the one place in the
/// plugin that writes to something the player built by hand and cannot type back in. It shows
/// what it is about to do before it does it, and it keeps the order it found so the player can
/// have it back.
/// </para>
/// <para>
/// Nothing here runs by itself. There is no setting to sort on login, no sort after a gearset is
/// created, and none is planned: a list that rearranges itself while somebody is looking at it
/// is the behaviour this window exists to make deliberate.
/// </para>
/// </remarks>
internal sealed class ArrangeWindow : Window
{
    private const string WindowId = "Gearbook: sort the game's list###GearbookArrange";

    private readonly GearbookState state;

    public ArrangeWindow(GearbookState state, Action openLibrary, Action openSettings)
        : base(WindowId)
    {
        this.state = state;

        TitleBarButtons.Add(UiTheme.Link(
            FontAwesomeIcon.ListUl,
            () => state.Loc.Get(LocKeys.WindowLibraryTitle),
            openLibrary,
            priority: 0));

        TitleBarButtons.Add(UiTheme.Link(
            FontAwesomeIcon.Cog,
            () => state.Loc.Get(LocKeys.WindowSettingsTitle),
            openSettings,
            priority: 1));

        Size = new Vector2(460, 520);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    /// <inheritdoc />
    public override void PreDraw() => WindowName = $"{state.Loc.Get(LocKeys.WindowArrangeTitle)}###GearbookArrange";

    /// <inheritdoc />
    public override void Draw()
    {
        var character = state.Character;
        if (character is null)
        {
            return;
        }

        var loc = state.Loc;

        UiTheme.Wrapped(loc.Get(LocKeys.ArrangeIntro));
        ImGui.Spacing();

        DrawOrderChoice(character);
        ImGui.Spacing();

        var target = TargetKeys(character);
        var current = CurrentKeys();
        var moves = ListArrangement.MovesRemaining(current, target);

        DrawPreview(character, target, moves);

        ImGui.Separator();
        ImGui.Spacing();

        DrawApply(character, target, moves);

        ImGui.Spacing();
        DrawRestore(character);

        if (state.LastArrangeResult is not null)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            UiTheme.Wrapped(ResultText(state.LastArrangeResult.Value));
        }
    }

    private void DrawOrderChoice(CharacterSettings character)
    {
        var loc = state.Loc;

        // The same orders the library offers, minus the one that means "leave it as the game has
        // it". Sorting a list into the order it is already in is not an option, it is the absence
        // of one, and offering it would put a button on screen that does nothing.
        GearsetSortOrder[] orders =
        [
            GearsetSortOrder.Role,
            GearsetSortOrder.Job,
            GearsetSortOrder.Name,
            GearsetSortOrder.ItemLevel,
            GearsetSortOrder.LastUsed,
        ];

        var labels = orders.Select(SortName).ToArray();
        var index = Math.Max(0, Array.IndexOf(orders, character.ArrangeOrder));

        ImGui.SetNextItemWidth(-1f);
        if (ImGui.Combo($"##arrangeorder", ref index, labels, labels.Length))
        {
            character.ArrangeOrder = orders[index];
            state.Save();
        }

        UiTheme.Caption(loc.Get(LocKeys.ArrangeOrderHelp));
    }

    private void DrawPreview(CharacterSettings character, IReadOnlyList<string> target, int moves)
    {
        var loc = state.Loc;

        ImGui.TextUnformatted(string.Format(
            CultureInfo.CurrentCulture,
            loc.Get(LocKeys.ArrangeMoves),
            moves));

        using var child = ImRaii.Child("##arrangepreview", new Vector2(-1f, 220f), border: true);
        if (!child)
        {
            return;
        }

        var sorted = FilterEngine.Sort(
            state.Gearsets,
            character.ArrangeOrder,
            state.Jobs,
            character.RoleOrder,
            character.JobOrder);

        var current = CurrentKeys();

        for (var i = 0; i < sorted.Count; i++)
        {
            // Marked where the entry is not already where it would end up, so the preview reads
            // as a list of changes rather than as a wall of names to compare by eye.
            var settled = i < current.Count
                          && i < target.Count
                          && string.Equals(current[i], target[i], StringComparison.Ordinal);

            var line = string.Create(
                CultureInfo.CurrentCulture,
                $"{i + 1}. {sorted[i].Gearset.Name}");

            if (settled)
            {
                UiTheme.Muted(line);
            }
            else
            {
                ImGui.TextUnformatted(line);
            }
        }
    }

    private void DrawApply(CharacterSettings character, IReadOnlyList<string> target, int moves)
    {
        var loc = state.Loc;

        // Said before the button rather than after the failure. The game refuses a reordering
        // while it is busy by doing nothing at all, so somebody who starts a craft halfway
        // through gets a run that stops with half the list moved and no obvious cause.
        UiTheme.Caption(loc.Get(LocKeys.ArrangeUndisturbed));

        var allowed = state.CanArrange();

        using (ImRaii.Disabled(moves == 0 || allowed != ArrangeOutcome.Ready))
        {
            if (ImGui.Button(loc.Get(LocKeys.ArrangeApply), new Vector2(-1f, 0f)))
            {
                // The order as it stands is kept before the first move, not after, and it is
                // kept every time rather than only the first: somebody who sorts by job and then
                // by name wants the list from before the second sort back, not the one from
                // before the first.
                character.ListBackup = [.. CurrentKeys()];
                state.Save();

                state.RequestArrange(target);
            }
        }

        UiTheme.Caption(loc.Get(LocKeys.ArrangeApplyHelp));

        // The reason under the button it explains, in the warning colour, because a control that
        // is greyed out without saying why reads as a fault in the plugin.
        if (allowed != ArrangeOutcome.Ready)
        {
            using var colour = ImRaii.PushColor(ImGuiCol.Text, UiTheme.UnreadColour);
            UiTheme.Wrapped(ResultText(new ArrangeResult(allowed, 0)));
        }
    }

    private void DrawRestore(CharacterSettings character)
    {
        var loc = state.Loc;
        var hasBackup = character.ListBackup.Count > 0;

        using (ImRaii.Disabled(!hasBackup))
        {
            if (ImGui.Button(loc.Get(LocKeys.ArrangeRestore), new Vector2(-1f, 0f)))
            {
                state.RequestArrange(character.ListBackup);
            }
        }

        UiTheme.Caption(loc.Get(hasBackup ? LocKeys.ArrangeRestoreHelp : LocKeys.ArrangeRestoreNone));
    }

    private IReadOnlyList<string> CurrentKeys() =>
        [.. state.Gearsets.OrderBy(g => g.Gearset.Slot).Select(g => ListArrangement.KeyFor(g.Gearset))];

    private IReadOnlyList<string> TargetKeys(CharacterSettings character) =>
        [.. FilterEngine
            .Sort(state.Gearsets, character.ArrangeOrder, state.Jobs, character.RoleOrder, character.JobOrder)
            .Select(g => ListArrangement.KeyFor(g.Gearset))];

    private string ResultText(ArrangeResult result)
    {
        var loc = state.Loc;

        var key = result.Outcome switch
        {
            ArrangeOutcome.Done => LocKeys.ArrangeResultDone,
            ArrangeOutcome.NothingToDo => LocKeys.ArrangeResultNothing,
            ArrangeOutcome.NotLoggedIn => LocKeys.ArrangeResultNotLoggedIn,
            ArrangeOutcome.Unavailable => LocKeys.ArrangeResultUnavailable,
            ArrangeOutcome.Busy => LocKeys.ArrangeResultBusy,
            _ => LocKeys.ArrangeResultStopped,
        };

        return string.Format(CultureInfo.CurrentCulture, loc.Get(key), result.MovesApplied);
    }

    private string SortName(GearsetSortOrder order) => state.Loc.Get(order switch
    {
        GearsetSortOrder.Name => LocKeys.SortByName,
        GearsetSortOrder.Job => LocKeys.SortByJob,
        GearsetSortOrder.ItemLevel => LocKeys.SortByItemLevel,
        GearsetSortOrder.LastUsed => LocKeys.SortByLastUsed,
        GearsetSortOrder.Role => LocKeys.SortByRole,
        _ => LocKeys.SortBySlot,
    });
}
