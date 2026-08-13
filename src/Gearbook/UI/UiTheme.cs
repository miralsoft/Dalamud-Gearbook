using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
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
        ImGui.TextUnformatted(text);
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
            // Greyed out rather than hidden, and still drawn at full size, so the bar does not
            // change shape the moment combat starts.
            using (ImRaii.Disabled(!clickable))
            {
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
        }

        var min = origin;
        var max = origin + new Vector2(size, size) + (ImGui.GetStyle().FramePadding * 2f);

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

        var badge = state.Bis.For(gearset.Gearset.Slot);
        if (badge is not null)
        {
            ImGui.Separator();
            ImGui.TextUnformatted($"{badge.Matched} / {badge.Total}   {badge.Target}");
        }

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
        Adapters.EquipOutcome.Incomplete => loc.Get(LocKeys.SwitchBlockedIncomplete),
        Adapters.EquipOutcome.AlreadyWorn => loc.Get(LocKeys.SwitchAlreadyActive),
        Adapters.EquipOutcome.Refused => loc.Get(LocKeys.SwitchFailed),
        _ => null,
    };
}
