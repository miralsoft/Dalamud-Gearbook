using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Gearbook.Adapters;
using Gearbook.Core.Filtering;
using Gearbook.Core.Identity;
using Gearbook.Core.Localization;
using Gearbook.Core.Settings;
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
    private readonly Action openSettings;

    public BarWindow(GearbookState state, Action openLibrary, Action openSettings)
        : base(WindowId, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.state = state;
        this.openLibrary = openLibrary;
        this.openSettings = openSettings;

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

        // Unlocked, the window has a title bar, and the bar sizes itself to its contents. Two
        // icons are narrower than the title and its buttons, so the buttons end up half off the
        // window and cannot be pressed, which is exactly when they are needed.
        //
        // The floor is measured rather than guessed: the title, one square per button plus the
        // close button, the spacing between them, and the window's own padding. A number typed
        // here would be wrong at the next font size or the next added button.
        SizeConstraints = locked
            ? null
            : new WindowSizeConstraints
            {
                MinimumSize = new Vector2(MinimumUnlockedWidth(), 0f),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
            };

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

    /// <summary>
    /// How wide the window has to be for its own title bar to fit.
    /// </summary>
    private float MinimumUnlockedWidth()
    {
        var style = ImGui.GetStyle();

        // One square per cross-link, plus the close button the host draws itself.
        var buttons = TitleBarButtons.Count + 1;
        var buttonWidth = buttons * (ImGui.GetFrameHeight() + style.ItemInnerSpacing.X);

        var title = ImGui.CalcTextSize(state.Loc.Get(LocKeys.WindowBarTitle)).X;

        return title + buttonWidth + (style.WindowPadding.X * 4f);
    }

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

        var shown = Contents();

        // Built before anything is drawn, because the empty bar needs it too. An empty bar is a
        // normal state now that the fallbacks are gone, and it would otherwise be the one place
        // with no menu at all, which is exactly where somebody arrives wondering what to do.
        var barControls = new UiTheme.BarContextActions(
            settings.Locked,
            ToggleLock,
            openLibrary,
            openSettings);

        if (shown.Count == 0)
        {
            UiTheme.Muted(loc.Get(LocKeys.BarEmpty));
            UiTheme.HelpMarker(loc.Get(LocKeys.BarAddHint));

            if (ImGui.IsItemClicked())
            {
                openLibrary();
            }
        }
        else
        {
            var columns = Math.Max(1, settings.Columns);

            for (var i = 0; i < shown.Count; i++)
            {
                if (i % columns != 0)
                {
                    ImGui.SameLine();
                }

                // Every icon carries the bar's own controls as well as its own, because a locked
                // bar has no title bar and the window menu below declines to open over an icon.
                if (UiTheme.GearsetTile(
                        state,
                        shown[i],
                        settings.IconSize,
                        settings.ShowItemLevel,
                        settings.ShowFavourite,
                        settings.HighlightActive,
                        barControls))
                {
                    state.RequestEquip(shown[i].Gearset.Slot, EquipTrigger.Bar);
                }
            }
        }

        // The bar's own menu, and only where no tile is under the pointer. Without that flag it
        // opens over the icons too and swallows the right-click that was meant for the gearset,
        // so the per-tile menu would look as though it did not exist.
        //
        // The same entries the icons carry, minus the favourite mark, which has no meaning on
        // empty space. Drawn from one method so the two menus cannot offer different things.
        using var context = ImRaii.ContextPopup(
            "##GearbookBarContext",
            ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems);
        if (context)
        {
            UiTheme.BarContextEntries(state, barControls);
        }
    }

    /// <summary>
    /// Locks or unlocks the bar. One method, called from the window's own menu, from every
    /// icon's menu and from the settings, so the three cannot drift apart.
    /// </summary>
    private void ToggleLock()
    {
        var character = state.Character;
        if (character is null)
        {
            return;
        }

        character.Bar.Locked = !character.Bar.Locked;
        state.Save();
    }

    /// <summary>
    /// What the bar shows: the favourites, in the order the player arranged them.
    /// </summary>
    /// <remarks>
    /// One rule, with no fallback behind it. There used to be two more: favourites when nothing
    /// was placed explicitly, then the whole filtered list when there were no favourites either.
    /// Both were there to keep the bar from starting empty, and the price was that nobody could
    /// say what the bar was showing without knowing which of the three cases they were in. An
    /// empty bar that says how to fill it is easier to understand than a full one that cannot
    /// explain itself.
    /// </remarks>
    private IReadOnlyList<ReconciledGearset> Contents()
    {
        var character = state.Character;

        var favourites = BarOrder.OnBar(
            state.Gearsets,
            includeEverything: character?.Bar.Contents == BarContents.All);

        if (character?.Bar.Sort is not { } sort)
        {
            return favourites;
        }

        // The role order is the one from the settings, so the bar and the library group things
        // the same way. Two orders called "by role" that disagreed would be worse than not
        // offering it on the bar at all.
        return FilterEngine.Sort(favourites, sort, state.Jobs, character.RoleOrder);
    }
}
