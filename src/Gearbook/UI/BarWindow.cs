using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Gearbook.Adapters;
using Gearbook.Core.Filtering;
using Gearbook.Core.Identity;
using Gearbook.Core.Localization;
using Gearbook.Core.Model;
using Gearbook.Core.Settings;
using Gearbook.Core.Sorting;
using Gearbook.Services;

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

    /// <summary>The identity of the view menu, used by both the opener and the popup itself.</summary>
    private const string ViewMenuId = "##gearbookviewmenu";

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
    /// The icons per row to actually use, once the screen has had its say.
    /// </summary>
    /// <remarks>
    /// Measured against the host's work area rather than the whole screen, so the bar stays
    /// inside the part of the game somebody can actually reach.
    ///
    /// The room taken off the top for a title bar exists only while the bar is unlocked. A locked
    /// bar has none, and subtracting one anyway would wrap a row earlier than needed for the
    /// state the bar spends nearly all its time in.
    /// </remarks>
    private static int FittedColumns(BarSettings settings, int tiles)
    {
        var style = ImGui.GetStyle();
        var cell = new Vector2(settings.IconSize, settings.IconSize)
                   + (style.FramePadding * 2f)
                   + style.ItemSpacing;

        var work = ImGui.GetMainViewport().WorkSize;
        var chrome = style.WindowPadding * 2f;
        var titleBar = settings.Locked ? 0f : ImGui.GetFrameHeight();

        return BarLayout.Columns(
            tiles,
            settings.Columns,
            cell.X,
            cell.Y,
            work.X - chrome.X,
            work.Y - chrome.Y - titleBar);
    }

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

        var drawn = 0;

        if (settings.ShowViewSwitcher)
        {
            DrawViewSwitcher(settings);
            drawn = 1;
        }

        var columns = FittedColumns(settings, shown.Count + drawn);

        if (shown.Count == 0)
        {
            if (drawn > 0)
            {
                ImGui.SameLine();
            }

            UiTheme.Muted(loc.Get(LocKeys.BarEmpty));
            UiTheme.HelpMarker(loc.Get(LocKeys.BarAddHint));

            if (ImGui.IsItemClicked())
            {
                openLibrary();
            }
        }
        else
        {
            for (var i = 0; i < shown.Count; i++)
            {
                if ((i + drawn) % columns != 0)
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
    /// The first tile: what the bar is showing, and a menu to change it.
    /// </summary>
    /// <remarks>
    /// On the bar rather than in the settings, because this is a choice made in the middle of
    /// doing something else. Walking to a settings window to say "show me the tanks" costs more
    /// than reading the whole list would have.
    ///
    /// It is a separate idea from the library's filter and deliberately does not touch it.
    /// Switching the bar to the tanks must not silently rewrite a filter somebody spent a minute
    /// building next door.
    /// </remarks>
    private void DrawViewSwitcher(BarSettings settings)
    {
        var loc = state.Loc;

        // The same footprint as a gearset tile. An image button treats its size as the picture
        // and adds the frame padding around it, while a plain button treats it as the whole
        // control, so passing the icon size to both makes the switcher smaller than everything
        // beside it. The padding is added here to match.
        var size = new Vector2(settings.IconSize, settings.IconSize)
                   + (ImGui.GetStyle().FramePadding * 2f);

        bool pressed;

        using (ImRaii.PushId("##gearbookview"))
        {
            pressed = ViewSymbol(ViewGameIcon(settings), ViewIcon(settings), settings.IconSize, size);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"{loc.Get(LocKeys.BarViewTooltip)}\n{ViewLabel(settings)}");
        }

        // The left button only, and that is a fix rather than a preference. A right click opens
        // this popup on the press and the bar's own context menu on the release, so the symbols
        // appeared for a frame and were then replaced, which reads as a glitch rather than as
        // two menus disagreeing.
        //
        // Nothing is lost by giving it up: the tile has one job, and one button is enough for a
        // control with one job.
        if (pressed)
        {
            ImGui.OpenPopup(ViewMenuId);
        }

        using var popup = ImRaii.Popup(ViewMenuId);
        if (!popup)
        {
            return;
        }

        // The views as a fold-out strip of the same symbols, at the same size as the tiles, so
        // choosing one is the same gesture as pressing a gearset rather than reading a list of
        // words about pictures.
        var iconSize = settings.IconSize;

        foreach (var role in BarView.SelectableRoles)
        {
            if (ViewButton(
                    iconSize,
                    RoleIcons.For(role),
                    ViewIconFor(BarViewKind.Role, role, JobCategory.Combat),
                    RoleName(role),
                    settings.ViewKind == BarViewKind.Role && settings.ViewRole == role))
            {
                settings.ViewRole = role;
                SetView(settings, BarViewKind.Role);
            }
        }

        ImGui.Separator();

        foreach (var category in BarView.SelectableCategories)
        {
            if (ViewButton(
                    iconSize,
                    RoleIcons.For(category),
                    ViewIconFor(BarViewKind.Category, JobRole.Unknown, category),
                    CategoryName(category),
                    settings.ViewKind == BarViewKind.Category && settings.ViewCategory == category))
            {
                settings.ViewCategory = category;
                SetView(settings, BarViewKind.Category);
            }
        }

        ImGui.Separator();

        // No game symbol for these two, and none is borrowed. The game has stars and grids, but
        // each of them already means something else in it, and a player who knows what a symbol
        // means there reads it as that here too. A plain glyph says less and misleads nobody.
        if (ViewButton(iconSize, RoleIcons.None, FontAwesomeIcon.Star,
                loc.Get(LocKeys.SettingsBarContentsFavourites),
                settings.ViewKind == BarViewKind.Favourites))
        {
            SetView(settings, BarViewKind.Favourites);
        }

        if (ViewButton(iconSize, RoleIcons.None, FontAwesomeIcon.ThLarge,
                loc.Get(LocKeys.SettingsBarContentsAll),
                settings.ViewKind == BarViewKind.All))
        {
            SetView(settings, BarViewKind.All);
        }

        // The player's own words, so this part grows without anybody adding a feature: tag a few
        // sets as glamour and the bar can show exactly those. Tags stay as text, because one
        // symbol repeated for every tag would say nothing about which is which.
        var tags = FilterEngine.CollectTags(state.Gearsets);
        if (tags.Count == 0)
        {
            return;
        }

        ImGui.Separator();

        foreach (var tag in tags)
        {
            var active = settings.ViewKind == BarViewKind.Tag
                         && string.Equals(settings.ViewTag, tag, StringComparison.CurrentCultureIgnoreCase);

            if (ImGui.MenuItem(tag, string.Empty, active))
            {
                settings.ViewTag = tag;
                SetView(settings, BarViewKind.Tag);
            }
        }
    }

    /// <summary>
    /// One entry of the fold-out strip: the symbol, its name beside it, and a frame when it is
    /// the view currently showing.
    /// </summary>
    /// <remarks>
    /// The name is kept beside the symbol rather than left to a tooltip. A strip of unfamiliar
    /// glyphs is a guessing game the first few times, and the words cost one row of width in a
    /// menu that opens on demand.
    /// </remarks>
    private static bool ViewButton(
        float size,
        uint gameIcon,
        FontAwesomeIcon fallback,
        string label,
        bool active)
    {
        using var id = ImRaii.PushId(label);

        var origin = ImGui.GetCursorScreenPos();
        var box = new Vector2(size, size) + (ImGui.GetStyle().FramePadding * 2f);
        var pressed = ViewSymbol(gameIcon, fallback, size, box);

        if (active)
        {
            ImGui.GetWindowDrawList().AddRect(
                origin,
                origin + box,
                ImGui.GetColorU32(UiTheme.UnreadColour),
                2f,
                ImDrawFlags.None,
                2.5f);
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);

        return pressed;
    }

    /// <summary>
    /// One symbol as a button, from the game where the game has one and from the host's symbol
    /// font where it does not.
    /// </summary>
    /// <param name="gameIcon">The game's icon number, or <see cref="RoleIcons.None"/>.</param>
    /// <param name="fallback">The glyph to draw when there is no game icon.</param>
    /// <param name="size">The picture's edge length.</param>
    /// <param name="box">The whole control's size, picture plus frame padding.</param>
    /// <remarks>
    /// Both sizes are passed because the two controls measure differently: an image button is
    /// given the picture and adds the padding around it, while a plain button is given the
    /// finished control. Handing either one the other's number makes this tile a different size
    /// from the gearset tiles beside it.
    /// </remarks>
    private static bool ViewSymbol(uint gameIcon, FontAwesomeIcon fallback, float size, Vector2 box)
    {
        if (gameIcon != RoleIcons.None)
        {
            var texture = GearbookServices.Textures
                .GetFromGameIcon(new GameIconLookup(gameIcon))
                .GetWrapOrEmpty();

            return ImGui.ImageButton(texture.Handle, new Vector2(size, size));
        }

        using var font = GearbookState.IconFont.Push();
        return ImGui.Button(fallback.ToIconString(), box);
    }

    private static FontAwesomeIcon ViewIconFor(BarViewKind kind, JobRole role, JobCategory category) =>
        ViewIcon(new BarSettings { ViewKind = kind, ViewRole = role, ViewCategory = category });

    /// <summary>
    /// Switches the bar to a view and puts the fold-out strip away.
    /// </summary>
    /// <remarks>
    /// Closing belongs here rather than at each of the five places that choose a view, because
    /// every one of them is a finished decision and none of them wants the strip left standing.
    /// It has to be said at all: the strip is drawn from buttons, and a button inside a popup
    /// does not dismiss it the way a menu entry does, so the strip stayed open over the bar it
    /// had just rearranged and needed a second click somewhere else to go away.
    /// </remarks>
    private void SetView(BarSettings settings, BarViewKind kind)
    {
        settings.ViewKind = kind;
        state.Save();
        ImGui.CloseCurrentPopup();
    }

    /// <summary>The game's own symbol for the current view, or <see cref="RoleIcons.None"/>.</summary>
    private static uint ViewGameIcon(BarSettings settings) => settings.ViewKind switch
    {
        BarViewKind.Role => RoleIcons.For(settings.ViewRole),
        BarViewKind.Category => RoleIcons.For(settings.ViewCategory),
        _ => RoleIcons.None,
    };

    /// <summary>
    /// The symbol on the switcher tile when the game has none for that view.
    /// </summary>
    /// <remarks>
    /// Every role and every category is drawn from the game's own pictures now, so what is left
    /// here is the handful of views the game has no picture for at all, plus the case of a role
    /// the job table does not describe.
    /// </remarks>
    private static FontAwesomeIcon ViewIcon(BarSettings settings) => settings.ViewKind switch
    {
        BarViewKind.All => FontAwesomeIcon.ThLarge,
        BarViewKind.Tag => FontAwesomeIcon.Tag,

        BarViewKind.Role => settings.ViewRole switch
        {
            JobRole.Tank => FontAwesomeIcon.Shield,
            JobRole.Healer => FontAwesomeIcon.Plus,
            JobRole.MeleeDps => FontAwesomeIcon.FistRaised,
            JobRole.PhysicalRangedDps => FontAwesomeIcon.Bullseye,
            JobRole.MagicalRangedDps => FontAwesomeIcon.HatWizard,
            JobRole.Crafter => FontAwesomeIcon.Hammer,
            JobRole.Gatherer => FontAwesomeIcon.Leaf,
            _ => FontAwesomeIcon.Filter,
        },

        BarViewKind.Category => settings.ViewCategory switch
        {
            JobCategory.Combat => FontAwesomeIcon.Khanda,
            JobCategory.Crafting => FontAwesomeIcon.Hammer,
            JobCategory.Gathering => FontAwesomeIcon.Leaf,
            _ => FontAwesomeIcon.Filter,
        },

        _ => FontAwesomeIcon.Star,
    };

    /// <summary>The full name of the active view, for the tooltip.</summary>
    private string ViewLabel(BarSettings settings) => settings.ViewKind switch
    {
        BarViewKind.All => state.Loc.Get(LocKeys.SettingsBarContentsAll),
        BarViewKind.Role => RoleName(settings.ViewRole),
        BarViewKind.Category => CategoryName(settings.ViewCategory),
        BarViewKind.Tag => settings.ViewTag,
        _ => state.Loc.Get(LocKeys.SettingsBarContentsFavourites),
    };

    private static string Shorten(string text) =>
        string.IsNullOrWhiteSpace(text) ? "?" : text.Trim()[..Math.Min(3, text.Trim().Length)];

    private string RoleName(JobRole role) => state.Loc.Get(role switch
    {
        JobRole.Tank => LocKeys.RoleTank,
        JobRole.Healer => LocKeys.RoleHealer,
        JobRole.MeleeDps => LocKeys.RoleMeleeDps,
        JobRole.PhysicalRangedDps => LocKeys.RolePhysicalRangedDps,
        JobRole.MagicalRangedDps => LocKeys.RoleMagicalRangedDps,
        JobRole.Crafter => LocKeys.RoleCrafter,
        JobRole.Gatherer => LocKeys.RoleGatherer,
        _ => LocKeys.RoleUnknown,
    });

    private string CategoryName(JobCategory category) => state.Loc.Get(category switch
    {
        JobCategory.Combat => LocKeys.CategoryCombat,
        JobCategory.Crafting => LocKeys.CategoryCrafting,
        JobCategory.Gathering => LocKeys.CategoryGathering,
        _ => LocKeys.CategoryUnknown,
    });

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
        if (character is null)
        {
            return [];
        }

        var bar = character.Bar;

        var chosen = BarView.Select(
            state.Gearsets,
            bar.ViewKind,
            bar.ViewRole,
            bar.ViewCategory,
            bar.ViewTag,
            state.Jobs);

        // Outside the favourites view the arrangement orders a handful of what is on screen and
        // leaves the rest in an arbitrary tail, which reads as a fault rather than a rule. So a
        // view that is not the favourites falls back to the game's own numbering unless a sort
        // was chosen.
        if (bar.Sort is not { } sort)
        {
            return BarView.UsesArrangement(bar.ViewKind)
                ? chosen
                : FilterEngine.Sort(chosen, GearsetSortOrder.Slot, state.Jobs, character.RoleOrder);
        }

        // The role order is the one from the settings, so the bar and the library group things
        // the same way. Two orders called "by role" that disagreed would be worse than not
        // offering it on the bar at all.
        return FilterEngine.Sort(chosen, sort, state.Jobs, character.RoleOrder);
    }
}
