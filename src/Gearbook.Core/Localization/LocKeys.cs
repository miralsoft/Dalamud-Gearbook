using System.Reflection;

namespace Gearbook.Core.Localization;

/// <summary>
/// Every translatable key, as a constant. Nothing anywhere passes a loose string to the
/// catalogue.
/// </summary>
/// <remarks>
/// Two things follow from keeping them here rather than at the call sites. A typo becomes a
/// build error instead of a string that quietly renders as itself. And the completeness test
/// has an authoritative list to compare both catalogues against, which is what makes "this
/// language is missing a key" a failing build rather than a gap somebody notices in a
/// screenshot.
/// </remarks>
public static class LocKeys
{
    public const string WindowBarTitle = "window.bar.title";
    public const string WindowLibraryTitle = "window.library.title";
    public const string WindowSettingsTitle = "window.settings.title";
    public const string WindowNewsTitle = "window.news.title";

    public const string CommonClose = "common.close";
    public const string CommonSearch = "common.search";
    public const string CommonClear = "common.clear";
    public const string CommonAll = "common.all";
    public const string CommonNone = "common.none";
    public const string CommonCancel = "common.cancel";
    public const string CommonDelete = "common.delete";
    public const string CommonRename = "common.rename";
    public const string CommonSave = "common.save";
    public const string CommonUnknownJob = "common.unknownJob";
    public const string CommonNever = "common.never";

    public const string LibraryCount = "library.count";
    public const string LibraryEmpty = "library.empty";
    public const string LibraryNoMatches = "library.noMatches";
    public const string LibraryColumnNumber = "library.column.number";
    public const string LibraryColumnName = "library.column.name";
    public const string LibraryColumnJob = "library.column.job";
    public const string LibraryColumnItemLevel = "library.column.itemLevel";
    public const string LibraryColumnTags = "library.column.tags";
    public const string LibraryColumnBis = "library.column.bis";
    public const string LibraryDetailNote = "library.detail.note";
    public const string LibraryDetailTags = "library.detail.tags";
    public const string LibraryDetailTagsHint = "library.detail.tagsHint";
    public const string LibraryDetailFavourite = "library.detail.favourite";
    public const string LibraryDetailOnBar = "library.detail.onBar";
    public const string LibraryDetailPieces = "library.detail.pieces";
    public const string LibraryDetailGlamourPlate = "library.detail.glamourPlate";
    public const string LibraryDetailLastUsed = "library.detail.lastUsed";
    public const string LibraryDetailNothingSelected = "library.detail.nothingSelected";
    public const string LibraryDuplicateWarning = "library.duplicateWarning";
    public const string LibraryOrphanHeading = "library.orphan.heading";
    public const string LibraryOrphanExplain = "library.orphan.explain";
    public const string LibraryOrphanForget = "library.orphan.forget";

    public const string FilterHeading = "filter.heading";
    public const string FilterFavouritesOnly = "filter.favouritesOnly";
    public const string FilterIncompleteOnly = "filter.incompleteOnly";
    public const string FilterOnBarOnly = "filter.onBarOnly";
    public const string FilterNotOnBarOnly = "filter.notOnBarOnly";
    public const string FilterGlamourLinkedOnly = "filter.glamourLinkedOnly";
    public const string FilterRoles = "filter.roles";
    public const string FilterCategories = "filter.categories";
    public const string FilterTags = "filter.tags";
    public const string FilterUnusedSince = "filter.unusedSince";

    public const string RoleTank = "role.tank";
    public const string RoleHealer = "role.healer";
    public const string RoleMeleeDps = "role.meleeDps";
    public const string RolePhysicalRangedDps = "role.physicalRangedDps";
    public const string RoleMagicalRangedDps = "role.magicalRangedDps";
    public const string RoleCrafter = "role.crafter";
    public const string RoleGatherer = "role.gatherer";
    public const string RoleUnknown = "role.unknown";

    public const string CategoryCombat = "category.combat";
    public const string CategoryCrafting = "category.crafting";
    public const string CategoryGathering = "category.gathering";
    public const string CategoryUnknown = "category.unknown";

    public const string ViewActive = "view.active";
    public const string ViewNew = "view.new";
    public const string ViewSaveAs = "view.saveAs";
    public const string ViewNameTaken = "view.nameTaken";
    public const string ViewNameEmpty = "view.nameEmpty";
    public const string ViewEverything = "view.everything";

    public const string SortHeading = "sort.heading";
    public const string SortBySlot = "sort.bySlot";
    public const string SortByName = "sort.byName";
    public const string SortByJob = "sort.byJob";
    public const string SortByItemLevel = "sort.byItemLevel";
    public const string SortByLastUsed = "sort.byLastUsed";

    public const string BarLock = "bar.lock";
    public const string BarUnlock = "bar.unlock";
    public const string BarEmpty = "bar.empty";
    public const string BarAddHint = "bar.addHint";

    public const string SwitchBlockedInCombat = "switch.blocked.inCombat";
    public const string SwitchBlockedInCutscene = "switch.blocked.inCutscene";
    public const string SwitchBlockedOccupied = "switch.blocked.occupied";
    public const string SwitchBlockedIncomplete = "switch.blocked.incomplete";
    public const string SwitchBlockedNotLoggedIn = "switch.blocked.notLoggedIn";
    public const string SwitchAlreadyActive = "switch.alreadyActive";
    public const string SwitchFailed = "switch.failed";

    public const string SettingsTabGeneral = "settings.tab.general";
    public const string SettingsTabBar = "settings.tab.bar";
    public const string SettingsTabLibrary = "settings.tab.library";
    public const string SettingsTabAbout = "settings.tab.about";
    public const string SettingsLanguage = "settings.language";
    public const string SettingsLanguageAuto = "settings.language.auto";
    public const string SettingsLanguageHelp = "settings.language.help";
    public const string SettingsFilterLevel = "settings.filterLevel";
    public const string SettingsFilterLevelHelp = "settings.filterLevel.help";
    public const string SettingsFilterLevelFavourites = "settings.filterLevel.favourites";
    public const string SettingsFilterLevelSimple = "settings.filterLevel.simple";
    public const string SettingsFilterLevelFull = "settings.filterLevel.full";
    public const string SettingsBarColumns = "settings.bar.columns";
    public const string SettingsBarIconSize = "settings.bar.iconSize";
    public const string SettingsBarShowItemLevel = "settings.bar.showItemLevel";
    public const string SettingsBarShowFavourite = "settings.bar.showFavourite";
    public const string SettingsBarHighlightActive = "settings.bar.highlightActive";
    public const string SettingsBarHideInCombat = "settings.bar.hideInCombat";
    public const string SettingsBarHideInCutscene = "settings.bar.hideInCutscene";
    public const string SettingsBarLocked = "settings.bar.locked";
    public const string SettingsBarLockedHelp = "settings.bar.locked.help";
    public const string SettingsBarShowOnStart = "settings.bar.showOnStart";
    public const string SettingsLibraryShowGameNumber = "settings.library.showGameNumber";
    public const string SettingsLibraryShowOrphans = "settings.library.showOrphans";
    public const string SettingsNewsAutoOpen = "settings.news.autoOpen";
    public const string SettingsPerCharacterNote = "settings.perCharacterNote";
    public const string SettingsAboutVersion = "settings.about.version";
    public const string SettingsAboutRepository = "settings.about.repository";
    public const string SettingsAboutNoAutomation = "settings.about.noAutomation";

    public const string NewsUnread = "news.unread";
    public const string NewsOpen = "news.open";
    public const string NewsEmpty = "news.empty";
    public const string NewsCategoryAdded = "news.category.added";
    public const string NewsCategoryChanged = "news.category.changed";
    public const string NewsCategoryFixed = "news.category.fixed";
    public const string NewsCategoryRemoved = "news.category.removed";

    public const string BisStateNoAccount = "bis.state.noAccount";
    public const string BisStateLoading = "bis.state.loading";
    public const string BisStateNoData = "bis.state.noData";

    public const string CommandHelpMain = "command.help.main";
    public const string CommandHelpShort = "command.help.short";
    public const string CommandUnknownSubcommand = "command.unknownSubcommand";
    public const string CommandSwitchNoMatch = "command.switch.noMatch";
    public const string CommandSwitchAmbiguous = "command.switch.ambiguous";
    public const string CommandViewNoMatch = "command.view.noMatch";

    public const string DtrTooltip = "dtr.tooltip";

    /// <summary>
    /// Every key declared above, read once by reflection.
    /// </summary>
    /// <remarks>
    /// Derived rather than written down a second time (C-12). A hand-maintained list beside the
    /// constants is wrong at the next addition, and wrong invisibly, because the thing that
    /// would have caught it is the list itself.
    /// </remarks>
    public static IReadOnlyCollection<string> All { get; } =
        typeof(LocKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
}
