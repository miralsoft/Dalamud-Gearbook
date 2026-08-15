using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Gearbook.Core.Filtering;
using Gearbook.Core.Localization;
using Gearbook.Core.Model;
using Gearbook.Core.Settings;

namespace Gearbook.UI;

/// <summary>
/// The settings, grouped into tabs by topic.
/// </summary>
/// <remarks>
/// Dependent settings are greyed out rather than hidden, because a window that changes height
/// while you are using it is disorienting. Explanations sit behind a help affordance rather than
/// permanently under the control.
/// </remarks>
internal sealed class SettingsWindow : Window
{
    private const string WindowId = "Gearbook settings###GearbookSettings";

    private readonly GearbookState state;

    public SettingsWindow(GearbookState state, Action openLibrary, Action openNews)
        : base(WindowId)
    {
        this.state = state;

        TitleBarButtons.Add(UiTheme.Link(
            FontAwesomeIcon.ListUl,
            () => state.Loc.Get(LocKeys.WindowLibraryTitle),
            openLibrary,
            priority: 0));

        TitleBarButtons.Add(UiTheme.Link(
            FontAwesomeIcon.Book,
            () => state.Loc.Get(LocKeys.WindowNewsTitle),
            openNews,
            priority: 1));

        Size = new Vector2(520, 470);
        SizeCondition = ImGuiCond.FirstUseEver;

        // A floor, because below it even wrapped captions become a column of single words. The
        // wrapping is what makes any width above this one work, so there is no ceiling.
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360, 260),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    /// <inheritdoc />
    public override void PreDraw() =>
        WindowName = $"{state.Loc.Get(LocKeys.WindowSettingsTitle)}###GearbookSettings";

    /// <inheritdoc />
    public override void Draw()
    {
        var loc = state.Loc;
        var character = state.Character;

        if (character is null)
        {
            UiTheme.Muted(loc.Get(LocKeys.LibraryEmpty));
            return;
        }

        UiTheme.Muted(loc.Get(LocKeys.SettingsPerCharacterNote));
        ImGui.Separator();

        using var tabs = ImRaii.TabBar("##settings");
        if (!tabs)
        {
            return;
        }

        DrawGeneralTab(character);
        DrawBarTab(character);
        DrawLibraryTab(character);
        DrawAboutTab();
    }

    private void DrawGeneralTab(CharacterSettings character)
    {
        var loc = state.Loc;

        using var tab = ImRaii.TabItem(loc.Get(LocKeys.SettingsTabGeneral));
        if (!tab)
        {
            return;
        }

        var codes = new List<string> { LanguageResolver.Automatic };
        codes.AddRange(loc.AvailableCodes);

        var labels = codes
            .Select(c => c == LanguageResolver.Automatic ? loc.Get(LocKeys.SettingsLanguageAuto) : c)
            .ToList();

        var index = Math.Max(0, codes.FindIndex(c =>
            string.Equals(c, character.Language, StringComparison.OrdinalIgnoreCase)));

        UiTheme.Caption(loc.Get(LocKeys.SettingsLanguage), loc.Get(LocKeys.SettingsLanguageHelp));
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.Combo("##language", ref index, labels, labels.Count))
        {
            character.Language = codes[index];
            state.ApplyLanguage();
            state.Save();
        }

        ImGui.Spacing();

        var levels = new[] { FilterLevel.FavouritesOnly, FilterLevel.Simple, FilterLevel.Full };
        var levelLabels = new List<string>
        {
            loc.Get(LocKeys.SettingsFilterLevelFavourites),
            loc.Get(LocKeys.SettingsFilterLevelSimple),
            loc.Get(LocKeys.SettingsFilterLevelFull),
        };

        var levelIndex = Array.IndexOf(levels, character.FilterLevel);
        levelIndex = levelIndex < 0 ? 1 : levelIndex;

        UiTheme.Caption(loc.Get(LocKeys.SettingsFilterLevel), loc.Get(LocKeys.SettingsFilterLevelHelp));
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.Combo("##filterlevel", ref levelIndex, levelLabels, levelLabels.Count))
        {
            character.FilterLevel = levels[levelIndex];
            state.Save();
        }

        ImGui.Spacing();

        var openNews = character.OpenNewsAfterUpdate;
        if (UiTheme.WrappedCheckbox("newsauto", loc.Get(LocKeys.SettingsNewsAutoOpen), ref openNews))
        {
            character.OpenNewsAfterUpdate = openNews;
            state.Save();
        }
    }

    private void DrawBarTab(CharacterSettings character)
    {
        var loc = state.Loc;

        using var tab = ImRaii.TabItem(loc.Get(LocKeys.SettingsTabBar));
        if (!tab)
        {
            return;
        }

        var bar = character.Bar;
        var changed = false;

        // The ceiling is the gearset limit rather than a round number somebody liked. One column
        // gives a vertical bar and the limit gives a single row whatever the character owns, so
        // between them every shape is reachable and no setting is wasted on the impossible.
        var columns = bar.Columns;
        UiTheme.Caption(loc.Get(LocKeys.SettingsBarColumns));
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.SliderInt("##columns", ref columns, 1, GameLimits.MaxGearsets))
        {
            bar.Columns = columns;
            changed = true;
        }

        var size = bar.IconSize;
        UiTheme.Caption(loc.Get(LocKeys.SettingsBarIconSize));
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.SliderFloat("##iconsize", ref size, 16f, 96f))
        {
            bar.IconSize = size;
            changed = true;
        }

        ImGui.Spacing();

        var showItemLevel = bar.ShowItemLevel;
        if (UiTheme.WrappedCheckbox("showitemlevel", loc.Get(LocKeys.SettingsBarShowItemLevel), ref showItemLevel))
        {
            bar.ShowItemLevel = showItemLevel;
            changed = true;
        }

        var showFavourite = bar.ShowFavourite;
        if (UiTheme.WrappedCheckbox("showfavourite", loc.Get(LocKeys.SettingsBarShowFavourite), ref showFavourite))
        {
            bar.ShowFavourite = showFavourite;
            changed = true;
        }

        var highlight = bar.HighlightActive;
        if (UiTheme.WrappedCheckbox("highlight", loc.Get(LocKeys.SettingsBarHighlightActive), ref highlight))
        {
            bar.HighlightActive = highlight;
            changed = true;
        }

        ImGui.Spacing();

        var hideCutscene = bar.HideInCutscene;
        if (UiTheme.WrappedCheckbox("hidecutscene", loc.Get(LocKeys.SettingsBarHideInCutscene), ref hideCutscene))
        {
            bar.HideInCutscene = hideCutscene;
            changed = true;
        }

        var hideCombat = bar.HideInCombat;
        if (UiTheme.WrappedCheckbox("hidecombat", loc.Get(LocKeys.SettingsBarHideInCombat), ref hideCombat))
        {
            bar.HideInCombat = hideCombat;
            changed = true;
        }

        ImGui.Spacing();

        var locked = bar.Locked;
        if (UiTheme.WrappedCheckbox("locked", loc.Get(LocKeys.SettingsBarLocked), ref locked))
        {
            bar.Locked = locked;
            changed = true;
        }

        UiTheme.HelpMarker(loc.Get(LocKeys.SettingsBarLockedHelp));

        var showOnStart = bar.ShowOnStart;
        if (UiTheme.WrappedCheckbox("showonstart", loc.Get(LocKeys.SettingsBarShowOnStart), ref showOnStart))
        {
            bar.ShowOnStart = showOnStart;
            changed = true;
        }

        if (changed)
        {
            state.Save();
        }
    }

    private void DrawLibraryTab(CharacterSettings character)
    {
        var loc = state.Loc;

        using var tab = ImRaii.TabItem(loc.Get(LocKeys.SettingsTabLibrary));
        if (!tab)
        {
            return;
        }

        var library = character.Library;
        var changed = false;

        var showNumber = library.ShowGameNumber;
        if (UiTheme.WrappedCheckbox("shownumber", loc.Get(LocKeys.SettingsLibraryShowGameNumber), ref showNumber))
        {
            library.ShowGameNumber = showNumber;
            changed = true;
        }

        var showOrphans = library.ShowOrphans;
        if (UiTheme.WrappedCheckbox("showorphans", loc.Get(LocKeys.SettingsLibraryShowOrphans), ref showOrphans))
        {
            library.ShowOrphans = showOrphans;
            changed = true;
        }

        UiTheme.HelpMarker(loc.Get(LocKeys.LibraryOrphanExplain));

        // The setting has a caption of its own rather than borrowing the warning it switches on.
        // A checkbox labelled with a whole explanation is a checkbox nobody reads twice.
        var warnDuplicates = library.WarnAboutDuplicates;
        if (UiTheme.WrappedCheckbox("warnduplicates", loc.Get(LocKeys.SettingsLibraryWarnDuplicates), ref warnDuplicates))
        {
            library.WarnAboutDuplicates = warnDuplicates;
            changed = true;
        }

        UiTheme.HelpMarker(loc.Get(LocKeys.LibraryDuplicateWarning));

        if (changed)
        {
            state.Save();
        }
    }

    private void DrawAboutTab()
    {
        var loc = state.Loc;

        using var tab = ImRaii.TabItem(loc.Get(LocKeys.SettingsTabAbout));
        if (!tab)
        {
            return;
        }

        ImGui.TextUnformatted(loc.Get(LocKeys.SettingsAboutVersion, GearbookVersion.Current));

        ImGui.Spacing();
        ImGui.PushTextWrapPos(0f);
        ImGui.TextUnformatted(loc.Get(LocKeys.SettingsAboutNoAutomation));
        ImGui.PopTextWrapPos();

        ImGui.Spacing();
        UiTheme.Muted($"{loc.Get(LocKeys.SettingsAboutRepository)}: https://github.com/miralsoft/Dalamud-Gearbook");
    }
}
