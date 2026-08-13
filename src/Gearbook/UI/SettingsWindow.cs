using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Gearbook.Core.Filtering;
using Gearbook.Core.Localization;
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

    public SettingsWindow(GearbookState state)
        : base(WindowId)
    {
        this.state = state;

        Size = new Vector2(470, 430);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

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

        ImGui.SetNextItemWidth(200f);
        if (ImGui.Combo(loc.Get(LocKeys.SettingsLanguage), ref index, labels, labels.Count))
        {
            character.Language = codes[index];
            state.ApplyLanguage();
            state.Save();
        }

        UiTheme.HelpMarker(loc.Get(LocKeys.SettingsLanguageHelp));

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

        ImGui.SetNextItemWidth(200f);
        if (ImGui.Combo(loc.Get(LocKeys.SettingsFilterLevel), ref levelIndex, levelLabels, levelLabels.Count))
        {
            character.FilterLevel = levels[levelIndex];
            state.Save();
        }

        UiTheme.HelpMarker(loc.Get(LocKeys.SettingsFilterLevelHelp));

        ImGui.Spacing();

        var openNews = character.OpenNewsAfterUpdate;
        if (ImGui.Checkbox(loc.Get(LocKeys.SettingsNewsAutoOpen), ref openNews))
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

        var columns = bar.Columns;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderInt(loc.Get(LocKeys.SettingsBarColumns), ref columns, 1, 24))
        {
            bar.Columns = columns;
            changed = true;
        }

        var size = bar.IconSize;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderFloat(loc.Get(LocKeys.SettingsBarIconSize), ref size, 16f, 96f))
        {
            bar.IconSize = size;
            changed = true;
        }

        ImGui.Spacing();

        var showItemLevel = bar.ShowItemLevel;
        if (ImGui.Checkbox(loc.Get(LocKeys.SettingsBarShowItemLevel), ref showItemLevel))
        {
            bar.ShowItemLevel = showItemLevel;
            changed = true;
        }

        var showFavourite = bar.ShowFavourite;
        if (ImGui.Checkbox(loc.Get(LocKeys.SettingsBarShowFavourite), ref showFavourite))
        {
            bar.ShowFavourite = showFavourite;
            changed = true;
        }

        var highlight = bar.HighlightActive;
        if (ImGui.Checkbox(loc.Get(LocKeys.SettingsBarHighlightActive), ref highlight))
        {
            bar.HighlightActive = highlight;
            changed = true;
        }

        ImGui.Spacing();

        var hideCutscene = bar.HideInCutscene;
        if (ImGui.Checkbox(loc.Get(LocKeys.SettingsBarHideInCutscene), ref hideCutscene))
        {
            bar.HideInCutscene = hideCutscene;
            changed = true;
        }

        var hideCombat = bar.HideInCombat;
        if (ImGui.Checkbox(loc.Get(LocKeys.SettingsBarHideInCombat), ref hideCombat))
        {
            bar.HideInCombat = hideCombat;
            changed = true;
        }

        ImGui.Spacing();

        var locked = bar.Locked;
        if (ImGui.Checkbox(loc.Get(LocKeys.SettingsBarLocked), ref locked))
        {
            bar.Locked = locked;
            changed = true;
        }

        UiTheme.HelpMarker(loc.Get(LocKeys.SettingsBarLockedHelp));

        var showOnStart = bar.ShowOnStart;
        if (ImGui.Checkbox(loc.Get(LocKeys.SettingsBarShowOnStart), ref showOnStart))
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
        if (ImGui.Checkbox(loc.Get(LocKeys.SettingsLibraryShowGameNumber), ref showNumber))
        {
            library.ShowGameNumber = showNumber;
            changed = true;
        }

        var showOrphans = library.ShowOrphans;
        if (ImGui.Checkbox(loc.Get(LocKeys.SettingsLibraryShowOrphans), ref showOrphans))
        {
            library.ShowOrphans = showOrphans;
            changed = true;
        }

        UiTheme.HelpMarker(loc.Get(LocKeys.LibraryOrphanExplain));

        var warnDuplicates = library.WarnAboutDuplicates;
        if (ImGui.Checkbox(loc.Get(LocKeys.LibraryDuplicateWarning), ref warnDuplicates))
        {
            library.WarnAboutDuplicates = warnDuplicates;
            changed = true;
        }

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
