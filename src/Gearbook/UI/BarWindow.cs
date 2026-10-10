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

    private IReadOnlyList<ReconciledGearset>? cachedContents;
    private (int, BarViewKind, JobRole, JobCategory, string, bool, GearsetSortOrder) cachedKey;
    private IReadOnlySet<JobCategory> cachedCategories = new HashSet<JobCategory>();
    private IReadOnlyList<ExternalTool>? cachedTools;
    private bool cachedToolsInCosmic;

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
        if (character.Bar.HideInCutscene && state.IsInCutscene)
        {
            return false;
        }

        if (character.Bar.HideInCombat && state.IsInCombat)
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

        DrawExternalTools(settings, shown.Count + drawn, columns);

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
    /// Shortcuts to other plugins, at the end of the bar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Decided by the gearsets on the bar, not by the view that put them there
    /// (<see cref="ExternalToolPlacement"/>), so a favourites or tag bar holding crafters offers
    /// Artisan just as the crafting view does. A shortcut to a crafting plugin beside a row of
    /// tanks only, by the same rule, does not appear.
    /// </para>
    /// <para>
    /// The cosmic one additionally waits until the player is standing in that content. It has
    /// nothing to offer anywhere else, and a permanent tile for a place somebody visits
    /// occasionally is the kind of clutter this bar exists to avoid.
    /// </para>
    /// <para>
    /// Drawn with a glyph rather than the other plugin's own icon. Dalamud publishes which
    /// plugins are installed and lets one open another's window, but not their pictures: those
    /// are fetched from the web by the installer, and this plugin does not reach the network at
    /// all. One of them happens to ship an icon file locally, the others do not, so taking that
    /// route would give one shortcut a picture and the rest a placeholder.
    /// </para>
    /// </remarks>
    private void DrawExternalTools(BarSettings settings, int drawnSoFar, int columns)
    {
        if (!settings.ShowExternalTools)
        {
            return;
        }

        var drawn = drawnSoFar;

        foreach (var tool in ToolsForContents())
        {
            DrawToolTile(tool, GlyphFor(tool), settings, ref drawn, columns);
        }
    }

    /// <summary>
    /// The shortcuts for what the bar currently holds, recomputed only when the contents or the
    /// cosmic exploration state change rather than every frame.
    /// </summary>
    private IReadOnlyList<ExternalTool> ToolsForContents()
    {
        var inCosmic = state.IsInCosmicExploration;

        if (cachedTools is null || cachedToolsInCosmic != inCosmic)
        {
            cachedTools = ExternalToolPlacement.For(cachedCategories, inCosmic);
            cachedToolsInCosmic = inCosmic;
        }

        return cachedTools;
    }

    private static FontAwesomeIcon GlyphFor(ExternalTool tool) => tool switch
    {
        ExternalTool.Artisan => FontAwesomeIcon.Hammer,
        ExternalTool.Arsenal => FontAwesomeIcon.Bullseye,
        ExternalTool.Cosmic => FontAwesomeIcon.Moon,
        _ => FontAwesomeIcon.QuestionCircle,
    };

    /// <summary>One shortcut tile, or nothing at all when the plugin behind it is not there.</summary>
    private void DrawToolTile(
        ExternalTool tool,
        FontAwesomeIcon glyph,
        BarSettings settings,
        ref int drawn,
        int columns)
    {
        if (settings.HiddenTools.Contains(tool) || !state.IsToolAvailable(tool))
        {
            return;
        }

        if (drawn % columns != 0)
        {
            ImGui.SameLine();
        }

        var size = new Vector2(settings.IconSize, settings.IconSize)
                   + (ImGui.GetStyle().FramePadding * 2f);

        using (ImRaii.PushId($"##tool{tool}"))
        {
            if (ViewSymbol(RoleIcons.None, glyph, settings.IconSize, size))
            {
                state.Tools.Open(tool);
            }
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"{state.ToolName(tool)}\n{state.Loc.Get(LocKeys.BarToolTooltip)}");
        }

        drawn++;
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

        // The menu gets its own spacing rather than the bar's. The bar pushes a very small window
        // padding and the player's own icon spacing, because a row of icons is meant to sit
        // tight, and a popup opened inside that scope inherits both. That is right for the bar
        // and wrong here: this is a list of words, and the same numbers that make icons look
        // neat make text look pressed against the frame.
        //
        // Pushed before the popup begins, because window padding is read when the window is
        // created and a push afterwards would arrive too late to matter.
        using var menuPadding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10f, 10f));
        using var menuSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(10f, 6f));
        using var menuInner = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(8f, 4f));

        using var popup = ImRaii.Popup(ViewMenuId);
        if (!popup)
        {
            return;
        }

        // The views as a fold-out strip of the same symbols, at the same size as the tiles, so
        // choosing one is the same gesture as pressing a gearset rather than reading a list of
        // words about pictures.
        var iconSize = settings.IconSize;

        // Every row is drawn to one width, worked out from the longest name in the menu. Letting
        // each row take the width of the window instead would ask how wide the window is, and a
        // popup on its first frame has no answer yet, which is the same flicker the tooltips had.
        var rowWidth = MenuWidth(settings, iconSize);

        foreach (var role in BarView.SelectableRoles)
        {
            if (ViewButton(
                    iconSize,
                    rowWidth,
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
                    rowWidth,
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
        if (ViewButton(iconSize, rowWidth, RoleIcons.None, FontAwesomeIcon.Star,
                loc.Get(LocKeys.SettingsBarContentsFavourites),
                settings.ViewKind == BarViewKind.Favourites))
        {
            SetView(settings, BarViewKind.Favourites);
        }

        if (ViewButton(iconSize, rowWidth, RoleIcons.None, FontAwesomeIcon.ThLarge,
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

            // The same symbol on every tag, deliberately. A row of bare words under a column of
            // pictures reads as a list that ran out, and numbering them instead would claim an
            // order among the player's own words that nothing here means.
            if (ViewButton(iconSize, rowWidth, RoleIcons.None, FontAwesomeIcon.Tag, tag, active))
            {
                settings.ViewTag = tag;
                SetView(settings, BarViewKind.Tag);
            }
        }
    }

    /// <summary>
    /// One entry of the fold-out strip: the symbol, its name beside it, and a frame when it is
    /// the view currently showing. The whole row answers, not only the symbol.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The name is kept beside the symbol rather than left to a tooltip. A strip of unfamiliar
    /// glyphs is a guessing game the first few times, and the words cost one row of width in a
    /// menu that opens on demand.
    /// </para>
    /// <para>
    /// The row is one selectable with the picture and the words drawn over it, rather than a
    /// button with a label next to it. A menu entry whose name is inert is a menu entry that
    /// misses half the presses aimed at it, and the name is the part somebody reads before
    /// pressing. Neither the picture nor the text takes input, so nothing competes with the row
    /// underneath.
    /// </para>
    /// </remarks>
    private static bool ViewButton(
        float size,
        float width,
        RoleSymbol gameIcon,
        FontAwesomeIcon fallback,
        string label,
        bool active)
    {
        using var id = ImRaii.PushId(label);

        var style = ImGui.GetStyle();
        var origin = ImGui.GetCursorScreenPos();
        var start = ImGui.GetCursorPos();
        var box = new Vector2(size, size) + (style.FramePadding * 2f);

        var pressed = ImGui.Selectable("##row", false, ImGuiSelectableFlags.None, new Vector2(width, box.Y));
        var afterRow = ImGui.GetCursorPos();

        // Back over the selectable to lay the contents on top of it. Drawn after, so they appear
        // above the highlight rather than under it.
        ImGui.SetCursorPos(start);
        ViewSymbol(gameIcon, fallback, size, box, asButton: false);

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

        // Centred against the tile rather than against a text frame. The usual alignment call
        // assumes the thing beside the text is one line tall, and these tiles are as tall as the
        // player's icon size, so it left every name sitting near the top of its own row.
        ImGui.SetCursorPos(new Vector2(
            start.X + box.X + style.ItemSpacing.X,
            start.Y + ((box.Y - ImGui.GetTextLineHeight()) * 0.5f)));

        ImGui.TextUnformatted(label);

        ImGui.SetCursorPos(afterRow);
        return pressed;
    }

    /// <summary>One of the game's own pictures, ready to draw.</summary>
    private static Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap Picture(uint icon) =>
        GearbookServices.Textures.GetFromGameIcon(new GameIconLookup(icon)).GetWrapOrEmpty();

    /// <summary>
    /// How wide every row in the menu is drawn.
    /// </summary>
    /// <remarks>
    /// Measured from the longest name the menu will hold rather than taken from the window. A row
    /// that stretches to the window's edge has to ask how wide the window is, and a popup on its
    /// first frame has no answer yet, which is the flicker the tooltips had for the same reason.
    /// </remarks>
    private float MenuWidth(BarSettings settings, float iconSize)
    {
        var loc = state.Loc;
        var widest = 0f;

        void Consider(string label) => widest = Math.Max(widest, ImGui.CalcTextSize(label).X);

        foreach (var role in BarView.SelectableRoles)
        {
            Consider(RoleName(role));
        }

        foreach (var category in BarView.SelectableCategories)
        {
            Consider(CategoryName(category));
        }

        Consider(loc.Get(LocKeys.SettingsBarContentsFavourites));
        Consider(loc.Get(LocKeys.SettingsBarContentsAll));

        foreach (var tag in FilterEngine.CollectTags(state.Gearsets))
        {
            Consider(tag);
        }

        var style = ImGui.GetStyle();
        var tile = iconSize + (style.FramePadding.X * 2f);

        return tile + style.ItemSpacing.X + widest;
    }

    /// <summary>
    /// One symbol as a button, from the game where the game has one and from the host's symbol
    /// font where it does not.
    /// </summary>
    /// <param name="symbol">The game pictures to stack, or <see cref="RoleIcons.None"/>.</param>
    /// <param name="fallback">The glyph to draw when the game has nothing.</param>
    /// <param name="size">The picture's edge length.</param>
    /// <param name="box">The whole control's size, picture plus frame padding.</param>
    /// <param name="asButton">True for a control that takes the click itself, false for a picture
    /// drawn over something else that does. The menu rows use the second form, so that the row
    /// underneath answers rather than competing with the symbol on top of it.</param>
    /// <remarks>
    /// Both sizes are passed because the two controls measure differently: an image button is
    /// given the picture and adds the padding around it, while a plain button is given the
    /// finished control. Handing either one the other's number makes this tile a different size
    /// from the gearset tiles beside it.
    /// </remarks>
    private static bool ViewSymbol(
        RoleSymbol symbol,
        FontAwesomeIcon fallback,
        float size,
        Vector2 box,
        bool asButton = true)
    {
        if (!symbol.IsNone)
        {
            var style = ImGui.GetStyle();
            bool pressed;
            Vector2 pictureAt;

            if (asButton)
            {
                var origin = ImGui.GetCursorScreenPos();
                pressed = ImGui.ImageButton(Picture(symbol.Base).Handle, new Vector2(size, size));
                pictureAt = origin + style.FramePadding;
            }
            else
            {
                // Inside the frame padding an image button would have added, so the picture sits
                // where it would have sat and the two forms cannot drift apart.
                ImGui.SetCursorPos(ImGui.GetCursorPos() + style.FramePadding);
                pictureAt = ImGui.GetCursorScreenPos();
                ImGui.Image(Picture(symbol.Base).Handle, new Vector2(size, size));
                pressed = false;
            }

            if (symbol.Ground is { } ground)
            {
                // Inside the frame only. The frame is what makes this look like a tile the game
                // drew, and covering the whole square would trade that away for the colour it was
                // meant to add. Rounded to follow the corners the frame already has.
                var edge = size * 0.10f;

                ImGui.GetWindowDrawList().AddRectFilled(
                    pictureAt + new Vector2(edge, edge),
                    pictureAt + new Vector2(size - edge, size - edge),
                    ImGui.GetColorU32(ground),
                    size * 0.14f);
            }

            if (symbol.IsLayered)
            {
                // Drawn straight onto the list rather than as another item, because a second item
                // here would sit in the layout beside the first instead of on top of it, and
                // would take its own share of the hovering.
                //
                // Inset so the tool keeps clear of the frame the ground already carries. A tool
                // that touches the frame reads as a picture that did not fit.
                var inset = size * 0.16f;

                ImGui.GetWindowDrawList().AddImage(
                    Picture(symbol.Overlay).Handle,
                    pictureAt + new Vector2(inset, inset),
                    pictureAt + new Vector2(size - inset, size - inset));
            }

            return pressed;
        }

        using var font = GearbookState.IconFont.Push();

        if (asButton)
        {
            return ImGui.Button(fallback.ToIconString(), box);
        }

        // Centred in the same box the button form would have filled, by hand, because plain text
        // has no alignment of its own.
        var glyph = fallback.ToIconString();
        ImGui.SetCursorPos(ImGui.GetCursorPos() + ((box - ImGui.CalcTextSize(glyph)) * 0.5f));
        ImGui.TextUnformatted(glyph);
        return false;
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
    private static RoleSymbol ViewGameIcon(BarSettings settings) => settings.ViewKind switch
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
    /// <summary>
    /// What the bar holds, worked out when something changes rather than every frame.
    /// </summary>
    /// <remarks>
    /// The bar is on screen permanently, so anything it does per frame it does forever. Selecting
    /// a view and sorting it is not expensive, but it was arriving at an answer it already had
    /// sixty times a second, and against the plugin statistics window this was the most expensive
    /// drawer of thirteen plugins loaded at the time. The others cost what they cost only while a
    /// window is open.
    ///
    /// The key holds everything the answer depends on: the gearset list, through the state's
    /// revision, and each setting that selects or orders. A setting missing from it would show a
    /// stale bar until the next read of the game, which is the failure this shape has to be read
    /// carefully for.
    /// </remarks>
    private IReadOnlyList<ReconciledGearset> Contents()
    {
        var character = state.Character;
        if (character is null)
        {
            return [];
        }

        var bar = character.Bar;

        var key = (
            state.Revision,
            bar.ViewKind,
            bar.ViewRole,
            bar.ViewCategory,
            bar.ViewTag,
            bar.UseArrangement,
            character.CurrentFilter.Sort);

        if (cachedContents is not null && cachedKey == key)
        {
            return cachedContents;
        }

        cachedContents = Select(character, bar);
        cachedKey = key;

        // Derived here, with the contents, because it depends on nothing else. The shortcuts
        // follow from it once the cosmic state is known, which can change without the contents.
        cachedCategories = ExternalToolPlacement.CategoriesShown(cachedContents, state.Jobs);
        cachedTools = null;

        return cachedContents;
    }

    private IReadOnlyList<ReconciledGearset> Select(CharacterSettings character, BarSettings bar)
    {

        var chosen = BarView.Select(
            state.Gearsets,
            bar.ViewKind,
            bar.ViewRole,
            bar.ViewCategory,
            bar.ViewTag,
            state.Jobs);

        // The hand-made arrangement wins where there is one, which is the favourites view and
        // nowhere else. Every other view holds gearsets that were never arranged, so it would
        // order a handful of them and leave the rest in an arbitrary tail.
        if (bar.UseArrangement && BarView.UsesArrangement(bar.ViewKind))
        {
            return chosen;
        }

        // Otherwise the order is the one chosen in the library, deliberately the same setting
        // rather than a second one beside it. The bar had its own sort until it turned out that
        // two controls answering "in what order" can disagree, and that the one nobody is looking
        // at is the one that will: a bar quietly left on "by job" while the library was grouped by
        // role looked like the sorting was simply broken.
        return FilterEngine.Sort(
            chosen,
            character.CurrentFilter.Sort,
            state.Jobs,
            character.RoleOrder,
            character.JobOrder);
    }
}
