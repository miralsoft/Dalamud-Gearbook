using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Gearbook.Core.Identity;
using Gearbook.Core.Localization;
using Gearbook.Services;

namespace Gearbook.UI;

/// <summary>
/// Drawing helpers shared by the windows, so that the same thing looks the same everywhere.
/// </summary>
internal static class UiTheme
{
    private static readonly Vector4 FavouriteColour = new(1.00f, 0.82f, 0.30f, 1f);
    private static readonly Vector4 IncompleteColour = new(0.90f, 0.45f, 0.40f, 1f);
    private static readonly Vector4 ActiveOutline = new(0.45f, 0.80f, 1.00f, 1f);
    private static readonly Vector4 MutedColour = new(0.65f, 0.65f, 0.65f, 1f);

    /// <summary>
    /// Draws text with a dark outline behind it.
    /// </summary>
    /// <remarks>
    /// A small glyph drawn over content whose brightness cannot be relied on needs the outline.
    /// Job icons range from near-black to near-white, and without this the item level disappears
    /// into some of them. For something meant to be glanced at, that is the whole failure rather
    /// than a blemish.
    /// </remarks>
    public static void OutlinedText(ImDrawListPtr drawList, Vector2 position, string text, Vector4 colour)
    {
        var outline = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.85f));
        var foreground = ImGui.GetColorU32(colour);

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                drawList.AddText(position + new Vector2(dx, dy), outline, text);
            }
        }

        drawList.AddText(position, foreground, text);
    }

    /// <summary>
    /// A question mark that explains a setting on hover.
    /// </summary>
    /// <remarks>
    /// Behind an affordance rather than permanently under the control, so a settings window with
    /// twenty options is still readable. A help text says what a setting does; it does not defend
    /// its default.
    /// </remarks>
    public static void HelpMarker(string text)
    {
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, MutedColour))
        {
            ImGui.TextUnformatted("(?)");
        }

        if (ImGui.IsItemHovered())
        {
            using var tooltip = ImRaii.Tooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 28f);
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
        }
    }

    /// <summary>Text in the muted colour, for anything secondary.</summary>
    public static void Muted(string text)
    {
        using var colour = ImRaii.PushColor(ImGuiCol.Text, MutedColour);
        Wrapped(text);
    }

    /// <summary>
    /// Text that wraps at the edge of whatever it is drawn in, rather than running past it.
    /// </summary>
    /// <remarks>
    /// The default is not to wrap, so a caption that fits in English disappears off the side in
    /// German, where the same sentence is reliably longer. Layout is checked in the longest
    /// shipped language for exactly this reason, and wrapping is what makes that check pass
    /// without shortening the German into something clipped in meaning instead of in pixels.
    /// </remarks>
    public static void Wrapped(string text)
    {
        ImGui.PushTextWrapPos(0f);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
    }

    /// <summary>
    /// A caption above a control, with an optional explanation behind a help affordance.
    /// </summary>
    /// <remarks>
    /// Above rather than beside. ImGui puts a widget's own label to its right, which adds the
    /// caption's width to the widget's and makes the pair as wide as the longest translation. A
    /// caption on its own line wraps instead, so the window can be any width and nothing is cut
    /// off.
    /// </remarks>
    public static void Caption(string text, string? help = null)
    {
        Wrapped(text);

        if (help is not null)
        {
            HelpMarker(help);
        }
    }

    /// <summary>
    /// A checkbox whose caption wraps instead of running off the edge.
    /// </summary>
    /// <remarks>
    /// The caption is drawn as text beside a label-less box, because an ImGui checkbox label
    /// does not wrap at all. The cost is that only the box is clickable rather than the whole
    /// line. That is a smaller loss than a sentence with its ending cut off, which is what the
    /// alternative produced in German.
    /// </remarks>
    public static bool WrappedCheckbox(string id, string caption, ref bool value)
    {
        var changed = ImGui.Checkbox($"##{id}", ref value);

        ImGui.SameLine();
        Wrapped(caption);

        return changed;
    }

    /// <summary>
    /// A cross-link for a window's title bar.
    /// </summary>
    /// <remarks>
    /// The title bar rather than the content area, because that is where the host puts its own
    /// controls and therefore where a player already looks. The tooltip is a function rather
    /// than a string so that it follows the language while the window is open.
    /// </remarks>
    public static TitleBarButton Link(
        FontAwesomeIcon icon,
        Func<string> tooltip,
        Action click,
        int priority)
    {
        ArgumentNullException.ThrowIfNull(tooltip);
        ArgumentNullException.ThrowIfNull(click);

        return new TitleBarButton
        {
            Icon = icon,
            Priority = priority,
            IconOffset = new Vector2(2f, 1f),
            Click = _ => click(),
            ShowTooltip = () => ImGui.SetTooltip(tooltip()),
        };
    }

    /// <summary>The colour a title-bar link takes while it has something unread behind it.</summary>
    public static Vector4 UnreadColour { get; } = new(1f, 0.85f, 0.35f, 1f);

    /// <summary>
    /// The right-click menu a gearset carries wherever it appears.
    /// </summary>
    /// <remarks>
    /// The same menu on the bar and in the library, because a player who learns it in one place
    /// should not have to discover it again in the other. Marking a favourite belongs here above
    /// all: it is the one action that decides what the bar holds, and reaching it through a
    /// window is a detour from the thing you are already pointing at.
    /// </remarks>
    public static void GearsetContextMenu(GearbookState state, ReconciledGearset gearset)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(gearset);

        using var context = ImRaii.ContextPopupItem($"##gearset{gearset.Record.Id}");
        if (!context)
        {
            return;
        }

        var loc = state.Loc;
        var record = gearset.Record;

        var favourite = record.IsFavourite;
        if (ImGui.MenuItem(loc.Get(LocKeys.LibraryDetailFavourite), string.Empty, favourite))
        {
            state.UpdateRecords(records =>
                [.. records.Select(r => r.Id == record.Id ? r with { IsFavourite = !favourite } : r)]);
        }

        var onBar = record.BarPosition is not null;
        if (ImGui.MenuItem(loc.Get(LocKeys.LibraryDetailOnBar), string.Empty, onBar))
        {
            state.UpdateRecords(records => Core.Sorting.BarOrder.SetOnBar(records, record.Id, !onBar));
        }
    }

    /// <summary>
    /// One gearset as a clickable icon.
    /// </summary>
    /// <returns>True when the player clicked it and it can be equipped.</returns>
    public static bool GearsetTile(
        GearbookState state,
        ReconciledGearset gearset,
        float size,
        bool showItemLevel,
        bool showFavourite,
        bool highlightActive)
    {
        var blocked = state.CheckCanEquip(gearset);
        var clickable = blocked is Adapters.EquipOutcome.Sent;

        var iconId = state.IconFor(gearset.Gearset.Slot);
        var origin = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        var pressed = false;

        using (ImRaii.PushId(gearset.Record.Id))
        {
            // Deliberately not disabled while a switch is blocked. A disabled item takes no
            // input at all, which would also swallow the right-click menu and the modifier
            // click, and neither of those has anything to do with being in combat. The tile is
            // dimmed below instead, so it still looks unavailable and still answers.
            if (iconId != 0)
            {
                var texture = GearbookServices.Textures
                    .GetFromGameIcon(new GameIconLookup(iconId))
                    .GetWrapOrEmpty();

                pressed = ImGui.ImageButton(texture.Handle, new Vector2(size, size));
            }
            else
            {
                // No icon, which happens for a job the game data does not describe. A button
                // with the job number on it still switches.
                pressed = ImGui.Button($"{gearset.Gearset.Slot + 1}", new Vector2(size, size));
            }
        }

        // Bound to the icon that was just drawn, so right-clicking a tile reaches the same menu
        // right-clicking a row in the library does.
        GearsetContextMenu(state, gearset);

        var min = origin;
        var max = origin + new Vector2(size, size) + (ImGui.GetStyle().FramePadding * 2f);

        if (!clickable)
        {
            // The dimming a disabled item would have given, drawn by hand so the tile keeps
            // taking input. Same picture, without the side effect.
            drawList.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.45f)));
        }

        if (pressed && FavouriteModifierHeld)
        {
            // The modifier is what makes this safe. A plain click on the bar equips, and that is
            // the whole point of the bar, so a plain click cannot also change what the bar shows.
            ToggleFavourite(state, gearset);
            return false;
        }

        if (highlightActive && state.CurrentSlot == gearset.Gearset.Slot)
        {
            drawList.AddRect(min, max, ImGui.GetColorU32(ActiveOutline), 2f, ImDrawFlags.None, 2.5f);
        }

        if (showFavourite && gearset.Record.IsFavourite)
        {
            OutlinedText(drawList, min + new Vector2(2f, 0f), "*", FavouriteColour);
        }

        if (showItemLevel && gearset.Gearset.ItemLevel > 0)
        {
            var text = gearset.Gearset.ItemLevel.ToString(System.Globalization.CultureInfo.CurrentCulture);
            var textSize = ImGui.CalcTextSize(text);
            var colour = gearset.Gearset.IsIncomplete ? IncompleteColour : new Vector4(1f, 1f, 1f, 1f);

            OutlinedText(drawList, max - textSize - new Vector2(3f, 2f), text, colour);
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            DrawTooltip(state, gearset, blocked);
        }

        return pressed && clickable;
    }

    /// <summary>
    /// True when the player is holding the key that turns a click into a favourite toggle.
    /// </summary>
    /// <remarks>
    /// One place, so the bar and the library agree about which key it is. A gesture that means
    /// two different things in two windows of the same plugin is worse than not having it.
    /// </remarks>
    public static bool FavouriteModifierHeld => ImGui.GetIO().KeyCtrl;

    /// <summary>Toggles the favourite mark on one gearset.</summary>
    public static void ToggleFavourite(GearbookState state, ReconciledGearset gearset)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(gearset);

        var record = gearset.Record;
        state.UpdateRecords(records =>
            [.. records.Select(r => r.Id == record.Id ? r with { IsFavourite = !record.IsFavourite } : r)]);
    }

    private static void DrawTooltip(
        GearbookState state,
        ReconciledGearset gearset,
        Adapters.EquipOutcome blocked)
    {
        using var tooltip = ImRaii.Tooltip();

        var loc = state.Loc;

        ImGui.TextUnformatted(gearset.Gearset.Name);

        var job = state.Jobs.TryGetValue(gearset.Gearset.ClassJobId, out var found)
            ? found.Name
            : loc.Get(LocKeys.CommonUnknownJob);

        Muted($"{job}   {gearset.Gearset.ItemLevel}");

        if (!string.IsNullOrWhiteSpace(gearset.Record.Note))
        {
            ImGui.Separator();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 24f);
            ImGui.TextUnformatted(gearset.Record.Note);
            ImGui.PopTextWrapPos();
        }

        if (gearset.Record.Tags.Count > 0)
        {
            Muted(string.Join(", ", gearset.Record.Tags));
        }

        // Said out loud rather than left to the colour of a number, and phrased as what will
        // happen rather than as a refusal, because the set is still perfectly equippable.
        if (gearset.Gearset.IsIncomplete)
        {
            ImGui.Separator();
            using var incomplete = ImRaii.PushColor(ImGuiCol.Text, IncompleteColour);
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 24f);
            ImGui.TextUnformatted(loc.Get(LocKeys.GearsetIncomplete));
            ImGui.PopTextWrapPos();
        }

        var badge = state.Bis.For(gearset.Gearset.Slot);
        if (badge is not null)
        {
            ImGui.Separator();
            ImGui.TextUnformatted($"{badge.Matched} / {badge.Total}   {badge.Target}");
        }

        // Said in the tooltip, because a modifier nobody is told about is a modifier nobody uses.
        ImGui.Separator();
        Muted(loc.Get(LocKeys.GearsetFavouriteHint));

        var reason = BlockedReason(loc, blocked);
        if (reason is not null)
        {
            ImGui.Separator();
            using var colour = ImRaii.PushColor(ImGuiCol.Text, IncompleteColour);
            ImGui.TextUnformatted(reason);
        }
    }

    /// <summary>
    /// The sentence explaining why a gearset cannot be equipped, or null when it can.
    /// </summary>
    /// <remarks>
    /// One place, so the reason in the tooltip and the reason in the log are the same answer
    /// from the same code rather than two texts that drift.
    /// </remarks>
    public static string? BlockedReason(Localizer loc, Adapters.EquipOutcome outcome) => outcome switch
    {
        Adapters.EquipOutcome.Sent => null,
        Adapters.EquipOutcome.NotLoggedIn => loc.Get(LocKeys.SwitchBlockedNotLoggedIn),
        Adapters.EquipOutcome.InCombat => loc.Get(LocKeys.SwitchBlockedInCombat),
        Adapters.EquipOutcome.InCutscene => loc.Get(LocKeys.SwitchBlockedInCutscene),
        Adapters.EquipOutcome.Occupied => loc.Get(LocKeys.SwitchBlockedOccupied),
        Adapters.EquipOutcome.AlreadyWorn => loc.Get(LocKeys.SwitchAlreadyActive),
        Adapters.EquipOutcome.Refused => loc.Get(LocKeys.SwitchFailed),
        _ => null,
    };
}
