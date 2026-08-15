using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Gearbook.Adapters;
using Gearbook.Core.Filtering;
using Gearbook.Core.Identity;
using Gearbook.Core.Localization;
using Gearbook.Core.Sorting;

namespace Gearbook.UI;

/// <summary>
/// The hotbar replacement: job icons, one click each.
/// </summary>
/// <remarks>
/// A window of its own rather than a panel inside the library. The two are different objects:
/// this one is permanently visible, has no title bar once locked and holds nothing but icons,
/// while the library is opened rarely and is dense. Folding them together would produce one
/// window that is either too heavy to leave on screen or too thin to manage a hundred gearsets.
/// </remarks>
internal sealed class BarWindow : Window
{
    /// <summary>
    /// A stable identity suffix, so the window keeps its position and size when the title
    /// changes. Any window with a translated title has a changing title, and with the plain form
    /// ImGui derives the identity from the whole string and forgets everything on every change.
    /// </summary>
    private const string WindowId = "Gearbook bar###GearbookBar";

    private readonly GearbookState state;
    private readonly Action openLibrary;

    public BarWindow(GearbookState state, Action openLibrary, Action openSettings)
        : base(WindowId, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.state = state;
        this.openLibrary = openLibrary;

        RespectCloseHotkey = false;

        // Only visible while the bar is unlocked, because a locked bar has no title bar at all.
        // That is the point of locking it, and both places stay reachable from the right-click
        // menu, so locking costs nothing.
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
    }

    /// <inheritdoc />
    public override bool DrawConditions()
    {
        var character = state.Character;
        if (character is null)
        {
            return false;
        }

        // Nothing sits over a cutscene, and it is also the one moment the player is definitely
        // not switching gear.
        if (character.Bar.HideInCutscene && state.GameState.IsInCutscene)
        {
            return false;
        }

        if (character.Bar.HideInCombat && state.GameState.IsInCombat)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override void PreDraw()
    {
        // The visible half of the title is re-resolved every frame, because the language can
        // change while the window is up. The identity after the two hashes never changes, which
        // is what stops the window from forgetting its position and size when it does.
        WindowName = $"{state.Loc.Get(LocKeys.WindowBarTitle)}###GearbookBar";

        var character = state.Character;
        var locked = character?.Bar.Locked ?? false;

        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize;

        if (locked)
        {
            Flags |= ImGuiWindowFlags.NoTitleBar
                     | ImGuiWindowFlags.NoResize
                     | ImGuiWindowFlags.NoMove
                     | ImGuiWindowFlags.NoBackground;
        }

        // Pushed before any early return in Draw, and popped unconditionally in PostDraw. An
        // unbalanced style stack does not corrupt this window, it corrupts every window drawn
        // after it, including other plugins'. That is the one drawing mistake you cannot find by
        // testing your own product.
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4f, 4f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(
            character?.Bar.IconSpacing ?? 4f,
            character?.Bar.IconSpacing ?? 4f));
    }

    /// <inheritdoc />
    public override void PostDraw() => ImGui.PopStyleVar(2);

    /// <inheritdoc />
    public override void Draw()
    {
        var character = state.Character;
        if (character is null)
        {
            return;
        }

        var loc = state.Loc;
        var settings = character.Bar;

        var shown = Contents(character.FilterLevel, character.CurrentFilter);

        if (shown.Count == 0)
        {
            UiTheme.Muted(loc.Get(LocKeys.BarEmpty));
            UiTheme.HelpMarker(loc.Get(LocKeys.BarAddHint));

            if (ImGui.IsItemClicked())
            {
                openLibrary();
            }

            return;
        }

        var columns = Math.Max(1, settings.Columns);

        for (var i = 0; i < shown.Count; i++)
        {
            if (i % columns != 0)
            {
                ImGui.SameLine();
            }

            if (UiTheme.GearsetTile(
                    state,
                    shown[i],
                    settings.IconSize,
                    settings.ShowItemLevel,
                    settings.ShowFavourite,
                    settings.HighlightActive))
            {
                state.RequestEquip(shown[i].Gearset.Slot, EquipTrigger.Bar);
            }
        }

        // The bar's own menu, and only where no tile is under the pointer. Without that flag it
        // opens over the icons too and swallows the right-click that was meant for the gearset,
        // so the per-tile menu would look as though it did not exist.
        using var context = ImRaii.ContextPopup(
            "##GearbookBarContext",
            ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems);
        if (context)
        {
            if (ImGui.MenuItem(loc.Get(LocKeys.WindowLibraryTitle)))
            {
                openLibrary();
            }

            ImGui.Separator();

            if (ImGui.MenuItem(
                    settings.Locked ? loc.Get(LocKeys.BarUnlock) : loc.Get(LocKeys.BarLock)))
            {
                settings.Locked = !settings.Locked;
                state.Save();
            }
        }
    }

    /// <summary>
    /// What the bar shows: the sets the player put on it, or, if they put none there, whatever
    /// the current filter lets through.
    /// </summary>
    /// <remarks>
    /// The fallback is what makes the bar useful on the first run, before anybody has arranged
    /// anything. A bar that starts empty and stays empty until its owner reads the manual is a
    /// bar nobody keeps.
    /// </remarks>
    private IReadOnlyList<ReconciledGearset> Contents(FilterLevel level, FilterSpec filter)
    {
        var arranged = BarOrder.OnBar(state.Gearsets);
        if (arranged.Count > 0)
        {
            return arranged;
        }

        var favourites = state.Gearsets.Where(g => g.Record.IsFavourite).ToList();
        if (favourites.Count > 0)
        {
            return FilterEngine.Sort(favourites, GearsetSortOrder.Slot, state.Jobs);
        }

        return FilterEngine.Apply(state.Gearsets, filter.AtLevel(level), state.Jobs, DateTimeOffset.UtcNow);
    }
}
