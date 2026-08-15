using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Gearbook.Core.Identity;
using Gearbook.Core.Localization;
using Gearbook.Core.Model;
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
    public static void OutlinedText(
        ImDrawListPtr drawList,
        Vector2 position,
        string text,
        Vector4 colour,
        float fontSize = 0f)
    {
        var outline = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.85f));
        var foreground = ImGui.GetColorU32(colour);

        var font = ImGui.GetFont();
        var drawn = fontSize > 0f ? fontSize : ImGui.GetFontSize();

        // The outline thins with the text. A one pixel ring around a nine pixel glyph is a third
        // of its stroke and swallows it; around an eighteen pixel one it disappears.
        var ring = Math.Max(1f, drawn / 12f);

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                drawList.AddText(font, drawn, position + (new Vector2(dx, dy) * ring), outline, text);
            }
        }

        drawList.AddText(font, drawn, position, foreground, text);
    }

    /// <summary>
    /// How large the marks drawn over a tile are, for a tile of a given size.
    /// </summary>
    /// <remarks>
    /// Tied to the icon rather than to the interface font. The item level and the favourite mark
    /// are painted onto the picture, so they have to hold a fixed share of it: at the interface
    /// font size they filled a third of a small tile, and on a large one they looked like a
    /// caption that had been left behind.
    ///
    /// The floor keeps a number legible on the smallest tile anybody sets, and the ceiling stops
    /// a very large tile from carrying a number bigger than the window's own text.
    /// </remarks>
    public static float TileMarkFontSize(float tileSize) =>
        Math.Clamp(tileSize * 0.52f, 12f, ImGui.GetFontSize() * 1.6f);


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
    /// <param name="state">The plugin state.</param>
    /// <param name="gearset">The gearset the menu belongs to.</param>
    /// <param name="barControls">Where to reach the bar's own controls from this menu, or null
    /// when the menu is not on the bar.</param>
    /// <param name="selection">Everything currently selected, or null where there is no
    /// selection. Right-clicking inside it acts on all of it.</param>
    public static void GearsetContextMenu(
        GearbookState state,
        ReconciledGearset gearset,
        BarContextActions? barControls = null,
        IReadOnlySet<int>? selection = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(gearset);

        // Right-clicking inside a selection acts on the selection. Acting on the one row under
        // the pointer instead would silently ignore the other thirty, which is the failure that
        // makes somebody stop trusting a multi-selection anywhere in the product.
        //
        // Right-clicking outside it means the one gearset, because that is plainly what was
        // pointed at.
        var targets = selection is { Count: > 1 } && selection.Contains(gearset.Record.Id)
            ? selection
            : null;

        // Control and right-click marks a favourite without opening anything. The menu is not
        // drawn at all while the modifier is held, so the two cannot both fire from one click.
        if (FavouriteModifierHeld)
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                ToggleFavourite(state, gearset, targets);
            }

            return;
        }

        using var context = ImRaii.ContextPopupItem($"##gearset{gearset.Record.Id}");
        if (!context)
        {
            return;
        }

        var loc = state.Loc;
        var record = gearset.Record;

        var label = targets is null
            ? loc.Get(LocKeys.LibraryDetailFavourite)
            : $"{loc.Get(LocKeys.LibraryDetailFavourite)} ({targets.Count})";

        if (ImGui.MenuItem(label, string.Empty, record.IsFavourite))
        {
            ToggleFavourite(state, gearset, targets);
        }

        if (barControls is null)
        {
            return;
        }

        ImGui.Separator();
        BarContextEntries(state, barControls);
    }

    /// <summary>
    /// The bar's own entries: lock, library, settings.
    /// </summary>
    /// <remarks>
    /// One method, drawn both on an icon and on the empty space beside the icons, so the two
    /// menus cannot offer different things. They already had: the empty-space menu was missing
    /// the settings, which nobody would notice until they went looking for it in the wrong half
    /// of the same window.
    ///
    /// It exists at all because a locked bar has no title bar and its window menu declines to
    /// open over an icon. On a full bar that leaves almost nowhere to click, so the control that
    /// unlocks it has to be on the icons as well as beside them.
    /// </remarks>
    public static void BarContextEntries(GearbookState state, BarContextActions actions)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(actions);

        var loc = state.Loc;

        if (ImGui.MenuItem(actions.Locked
                ? loc.Get(LocKeys.BarUnlock)
                : loc.Get(LocKeys.BarLock)))
        {
            actions.ToggleLock();
        }

        if (ImGui.MenuItem(loc.Get(LocKeys.WindowLibraryTitle)))
        {
            actions.OpenLibrary();
        }

        if (ImGui.MenuItem(loc.Get(LocKeys.WindowSettingsTitle)))
        {
            actions.OpenSettings();
        }
    }

    /// <summary>
    /// The bar's own controls, as the tile menu needs them.
    /// </summary>
    /// <param name="Locked">Whether the bar is currently locked.</param>
    /// <param name="ToggleLock">Locks or unlocks it.</param>
    /// <param name="OpenLibrary">Opens the library.</param>
    /// <param name="OpenSettings">Opens the settings.</param>
    public sealed record BarContextActions(
        bool Locked,
        Action ToggleLock,
        Action OpenLibrary,
        Action OpenSettings);

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
        bool highlightActive,
        BarContextActions? barControls = null)
    {
        var blocked = state.CheckCanEquip(gearset);
        var clickable = blocked is Adapters.EquipOutcome.Sent;

        var iconId = state.IconFor(gearset.Gearset.Slot);
        var origin = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        var pressed = false;

        // The highlight under the pointer carries the role, in the colours the game uses for it
        // everywhere else. It costs nothing: the tile is already lighting up, and the light may
        // as well say something. A job the job table does not describe keeps the host's own
        // colour rather than being painted a deliberate-looking grey.
        var roleColour = RoleColours.For(RoleOf(state, gearset));

        using (ImRaii.PushColor(ImGuiCol.ButtonHovered, RoleColours.Vivid(roleColour ?? default), roleColour is not null))
        using (ImRaii.PushColor(ImGuiCol.ButtonActive, RoleColours.Vivid(roleColour ?? default), roleColour is not null))
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
        // right-clicking a row in the library does, plus the bar's own controls.
        GearsetContextMenu(state, gearset, barControls);

        var min = origin;
        var max = origin + new Vector2(size, size) + (ImGui.GetStyle().FramePadding * 2f);

        // The role's colour laid over the tile while the pointer is on it, not merely behind it.
        // A picture button draws its background under the picture, and a job icon is opaque, so
        // the pushed hover colour only ever showed as a hairline in the frame padding. It was
        // there and it could not be seen, which is the same as not being there.
        //
        // The wash tints the icon and the outline states the colour at full strength, because a
        // wash alone has to stay pale enough to leave the picture readable.
        if (roleColour is { } hovered && ImGui.IsItemHovered())
        {
            var vivid = RoleColours.Vivid(hovered);

            drawList.AddRectFilled(min, max, ImGui.GetColorU32(RoleColours.WithAlpha(vivid, 0.45f)));
            drawList.AddRect(
                min,
                max,
                ImGui.GetColorU32(RoleColours.WithAlpha(vivid, 1f)),
                2f,
                ImDrawFlags.None,
                3f);
        }

        if (!clickable)
        {
            // The dimming a disabled item would have given, drawn by hand so the tile keeps
            // taking input. Same picture, without the side effect.
            drawList.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.45f)));
        }

        // The left button does one thing here and one thing only: it equips. Marking a favourite
        // moved to control and right-click, which is the same gesture in the library, where
        // control and left-click had to become multi-select the way it is in every file list.

        if (highlightActive && state.CurrentSlot == gearset.Gearset.Slot)
        {
            drawList.AddRect(min, max, ImGui.GetColorU32(ActiveOutline), 2f, ImDrawFlags.None, 2.5f);
        }

        var markSize = TileMarkFontSize(size);
        var markScale = markSize / ImGui.GetFontSize();
        var margin = Math.Max(2f, size * 0.07f);

        if (showFavourite && gearset.Record.IsFavourite)
        {
            OutlinedText(drawList, min + new Vector2(margin, 0f), "*", FavouriteColour, markSize);
        }

        if (showItemLevel && gearset.Gearset.ItemLevel > 0)
        {
            var text = gearset.Gearset.ItemLevel.ToString(System.Globalization.CultureInfo.CurrentCulture);

            // Measured at the interface font and scaled, rather than measured at the drawn size.
            // The two agree because the font is the same shape at every size, and this way the
            // measurement does not depend on which overload the host's binding exposes.
            var textSize = ImGui.CalcTextSize(text) * markScale;
            var colour = gearset.Gearset.IsIncomplete ? IncompleteColour : new Vector4(1f, 1f, 1f, 1f);

            OutlinedText(drawList, max - textSize - new Vector2(margin, margin * 0.7f), text, colour, markSize);
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

    /// <summary>
    /// Toggles the favourite mark, which is the same thing as putting the gearset on the bar or
    /// taking it off. One mark, one meaning.
    /// </summary>
    /// <param name="state">The plugin state.</param>
    /// <param name="gearset">The gearset that was clicked, which decides which way the toggle
    /// goes.</param>
    /// <param name="targets">Everything the change applies to, or null for just this one.</param>
    /// <remarks>
    /// With several targets the one that was clicked decides the direction for all of them,
    /// rather than each flipping its own state. Flipping each would turn a mixed selection into
    /// its own inverse, which is a result nobody asks for and nobody can undo with one click.
    /// </remarks>
    public static void ToggleFavourite(
        GearbookState state,
        ReconciledGearset gearset,
        IReadOnlySet<int>? targets = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(gearset);

        var record = gearset.Record;
        var wanted = !record.IsFavourite;

        if (targets is null)
        {
            state.UpdateRecords(records =>
                Core.Sorting.BarOrder.SetFavourite(records, record.Id, wanted));

            return;
        }

        var selected = new HashSet<int>(targets);
        state.UpdateRecords(records => Core.Editing.BulkEdit.SetFavourite(records, selected, wanted));
    }

    private static void DrawTooltip(
        GearbookState state,
        ReconciledGearset gearset,
        Adapters.EquipOutcome blocked)
    {
        using var tooltip = ImRaii.Tooltip();

        // A width in letters, not the window's edge. Wrapping at the edge asks how wide the
        // window is, and a tooltip on its first frame has no answer yet, so every line broke
        // against a provisional width and the whole thing appeared once as a tall narrow column
        // before settling. It read as a second dialog flashing past.
        //
        // Everything below inherits this, which is also why none of it wraps on its own any
        // more: a nested wrap position at the window edge would bring the flicker back for that
        // one line.
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 24f);

        var loc = state.Loc;

        ImGui.TextUnformatted(gearset.Gearset.Name);

        var job = state.Jobs.TryGetValue(gearset.Gearset.ClassJobId, out var found)
            ? found.Name
            : loc.Get(LocKeys.CommonUnknownJob);

        MutedInherited($"{job}   {gearset.Gearset.ItemLevel}");

        if (!string.IsNullOrWhiteSpace(gearset.Record.Note))
        {
            ImGui.Separator();
            ImGui.TextUnformatted(gearset.Record.Note);
        }

        if (gearset.Record.Tags.Count > 0)
        {
            MutedInherited(string.Join(", ", gearset.Record.Tags));
        }

        // Said out loud rather than left to the colour of a number, and phrased as what will
        // happen rather than as a refusal, because the set is still perfectly equippable.
        if (gearset.Gearset.IsIncomplete)
        {
            ImGui.Separator();
            using var incomplete = ImRaii.PushColor(ImGuiCol.Text, IncompleteColour);
            ImGui.TextUnformatted(loc.Get(LocKeys.GearsetIncomplete));
        }

        var badge = state.Bis.For(gearset.Gearset.Slot);
        if (badge is not null)
        {
            ImGui.Separator();
            ImGui.TextUnformatted($"{badge.Matched} / {badge.Total}   {badge.Target}");
        }

        // Said in the tooltip, because a modifier nobody is told about is a modifier nobody uses.
        ImGui.Separator();
        MutedInherited(loc.Get(LocKeys.GearsetFavouriteHint));

        var reason = BlockedReason(loc, blocked);
        if (reason is not null)
        {
            ImGui.Separator();
            using var colour = ImRaii.PushColor(ImGuiCol.Text, IncompleteColour);
            ImGui.TextUnformatted(reason);
        }

        ImGui.PopTextWrapPos();
    }

    /// <summary>The role of a gearset's job, or unknown when the job table does not describe it.</summary>
    private static Core.Model.JobRole RoleOf(GearbookState state, ReconciledGearset gearset) =>
        state.Jobs.TryGetValue(gearset.Gearset.ClassJobId, out var job)
            ? job.Role
            : Core.Model.JobRole.Unknown;

    /// <summary>Muted text that wraps wherever the caller already said it should.</summary>
    private static void MutedInherited(string text)
    {
        using var colour = ImRaii.PushColor(ImGuiCol.Text, MutedColour);
        ImGui.TextUnformatted(text);
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
