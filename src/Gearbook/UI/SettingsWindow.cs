using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Gearbook.Core.Filtering;
using Gearbook.Core.Localization;
using Gearbook.Core.Model;
using Gearbook.Core.Settings;
using Gearbook.Core.Sorting;

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

    /// <summary>Which row is being dragged, or -1. One window, one drag at a time.</summary>
    private int draggedRole = -1;

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
    public override void PreDraw()
    {
        WindowName = $"{state.Loc.Get(LocKeys.WindowSettingsTitle)}###GearbookSettings";

        // A settings window is read, not scanned, and the host's default spacing is tuned for
        // dense game windows. Loosening it here rather than adding a blank line between every
        // pair of controls keeps the change in one place and means a control added later is
        // spaced like the rest without anybody remembering to do it.
        //
        // Pushed before anything is drawn and popped unconditionally in PostDraw. An unbalanced
        // style stack corrupts every window drawn after this one, including other plugins'.
        var style = ImGui.GetStyle();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, style.WindowPadding * 1.6f);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(
            style.ItemSpacing.X * 1.4f,
            style.ItemSpacing.Y * 2.0f));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, style.FramePadding * 1.3f);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, style.ItemInnerSpacing * 1.6f);
    }

    /// <inheritdoc />
    public override void PostDraw() => ImGui.PopStyleVar(4);

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

        // First, and separated from the rest, because this is the setting somebody comes here
        // looking for. A locked bar has no title bar, so once it is locked this tab is one of
        // the few places that can undo it, and it used to sit last under seven other boxes.
        var locked = bar.Locked;
        if (UiTheme.WrappedCheckbox("locked", loc.Get(LocKeys.SettingsBarLocked), ref locked))
        {
            bar.Locked = locked;
            changed = true;
        }

        UiTheme.HelpMarker(loc.Get(LocKeys.SettingsBarLockedHelp));

        ImGui.Separator();
        ImGui.Spacing();

        // What the bar shows is chosen on the bar itself, where the effect is visible and where
        // the choice is actually made, in the middle of doing something else. What belongs here
        // is only whether that control is drawn at all.
        var showSwitcher = bar.ShowViewSwitcher;
        if (UiTheme.WrappedCheckbox("showswitcher", loc.Get(LocKeys.SettingsBarShowViewSwitcher), ref showSwitcher))
        {
            bar.ShowViewSwitcher = showSwitcher;
            changed = true;
        }

        UiTheme.HelpMarker(loc.Get(LocKeys.SettingsBarContentsHelp));

        ImGui.Spacing();

        // One switch where there used to be a list of orders. The bar takes the order chosen in
        // the library now, so the only question left here is the one the library cannot answer:
        // whether the arrangement the player dragged into shape still wins over it.
        UiTheme.Caption(loc.Get(LocKeys.SettingsBarSort), loc.Get(LocKeys.SettingsBarSortHelp));

        var useArrangement = bar.UseArrangement;
        if (UiTheme.WrappedCheckbox("barsort", loc.Get(LocKeys.SettingsBarSortManual), ref useArrangement))
        {
            bar.UseArrangement = useArrangement;
            changed = true;
        }

        UiTheme.Caption(loc.Get(LocKeys.SettingsBarSortFollows));

        ImGui.Spacing();

        // Off by default and stated plainly, because this is the one setting that puts something
        // on the bar which is not a gearset.
        var tools = bar.ShowExternalTools;
        if (UiTheme.WrappedCheckbox("bartools", loc.Get(LocKeys.SettingsBarTools), ref tools))
        {
            bar.ShowExternalTools = tools;
            changed = true;
        }

        UiTheme.Caption(loc.Get(LocKeys.SettingsBarToolsHelp));

        // The individual choices sit under the switch that turns the whole idea on, and only
        // while it is on. Offering them above it would ask which shortcuts to show before
        // establishing that any should be.
        if (bar.ShowExternalTools)
        {
            ImGui.Indent();

            foreach (var tool in Enum.GetValues<ExternalTool>())
            {
                var shown = !bar.HiddenTools.Contains(tool);
                var available = state.Tools.IsAvailable(tool);

                if (UiTheme.WrappedCheckbox($"tool{tool}", state.Tools.NameOf(tool), ref shown))
                {
                    // Stored as what is hidden, so a shortcut added later shows up for somebody
                    // who has already said yes to the idea.
                    bar.HiddenTools.Remove(tool);

                    if (!shown)
                    {
                        bar.HiddenTools.Add(tool);
                    }

                    changed = true;
                }

                // Said rather than left to be worked out. A ticked box for a plugin that is not
                // there would otherwise look like a shortcut that does not work.
                if (!available)
                {
                    ImGui.SameLine();
                    UiTheme.Muted(loc.Get(LocKeys.SettingsBarToolsMissing));
                }
            }

            ImGui.Unindent();
        }

        ImGui.Spacing();

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

        // This decides how much of this window's own filter panel is drawn, so it belongs here
        // rather than under general settings. It has nothing to do with the bar, which shows
        // favourites and does not consult the filter at all.
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
            changed = true;
        }

        ImGui.Spacing();

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

        ImGui.Spacing();
        ImGui.Separator();

        UiTheme.Caption(loc.Get(LocKeys.SettingsRoleOrder), loc.Get(LocKeys.SettingsRoleOrderHelp));
        UiTheme.Muted(loc.Get(LocKeys.SettingsRoleOrderDragHint));

        if (ImGui.SmallButton($"{loc.Get(LocKeys.SettingsOrderReset)}##roleorder"))
        {
            character.RoleOrder = [.. CharacterSettings.DefaultRoleOrder];
            changed = true;
        }

        ImGui.Spacing();

        var order = character.RoleOrder;
        var rowHeight = ImGui.GetFrameHeight();

        // Measured rather than guessed: two square buttons, the gap between them, the gap before
        // them, and a margin so the second one does not sit against the window edge. A factor
        // typed here would be wrong at the next font size, which is how they ended up flush
        // against the edge in the first place.
        var arrows = ImGui.GetStyle();
        var reserved = (rowHeight * 2f)
                       + (arrows.ItemSpacing.X * 2f)
                       + arrows.WindowPadding.X;

        for (var i = 0; i < order.Count; i++)
        {
            using var id = ImRaii.PushId(i);

            // Full-width rows rather than a label trailing two tiny buttons. The row is the
            // thing being moved, so the row is what you take hold of.
            var rowWidth = Math.Max(rowHeight * 3f, ImGui.GetContentRegionAvail().X - reserved);

            ImGui.Selectable(RoleName(order[i]), false, ImGuiSelectableFlags.None,
                new Vector2(rowWidth, rowHeight));

            // Dragging is the obvious gesture for a list you rearrange, and the arrows stay
            // because a drag is invisible until somebody tries it, and impossible for anyone
            // who cannot hold a button down while moving a mouse.
            using (var source = ImRaii.DragDropSource())
            {
                if (source)
                {
                    // The payload is only a type tag. The index travels in a field instead,
                    // because marshalling four bytes through the payload buys nothing here and
                    // costs an unsafe block: one window, one drag at a time.
                    ImGui.SetDragDropPayload("GearbookRoleOrder", ReadOnlySpan<byte>.Empty);
                    draggedRole = i;
                    ImGui.TextUnformatted(RoleName(order[i]));
                }
            }

            using (var target = ImRaii.DragDropTarget())
            {
                if (target)
                {
                    var dropped = !ImGui.AcceptDragDropPayload("GearbookRoleOrder").IsNull;

                    if (dropped && draggedRole >= 0 && draggedRole < order.Count && draggedRole != i)
                    {
                        var moving = order[draggedRole];
                        order.RemoveAt(draggedRole);
                        order.Insert(i, moving);
                        draggedRole = -1;
                        changed = true;
                    }
                }
            }

            ImGui.SameLine();

            using (ImRaii.Disabled(i == 0))
            {
                if (ImGui.ArrowButton("##up", ImGuiDir.Up))
                {
                    (order[i - 1], order[i]) = (order[i], order[i - 1]);
                    changed = true;
                }
            }

            ImGui.SameLine();

            using (ImRaii.Disabled(i == order.Count - 1))
            {
                if (ImGui.ArrowButton("##down", ImGuiDir.Down))
                {
                    (order[i + 1], order[i]) = (order[i], order[i + 1]);
                    changed = true;
                }
            }
        }

        DrawJobOrder(character, ref changed);

        if (changed)
        {
            state.Save();
        }
    }

    /// <summary>
    /// The order the jobs take inside their own role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Grouped under the roles rather than shown as one list, because that is the only place the
    /// setting has any effect: sorting by role separates the roles first, so a job moved past a
    /// role boundary would not move on screen at all. A flat list would offer a gesture that
    /// silently does nothing, which is worse than not offering it.
    /// </para>
    /// <para>
    /// Only the jobs this character has gearsets for appear. The rest would be rows nobody can
    /// see the effect of, and there are forty of them.
    /// </para>
    /// </remarks>
    private void DrawJobOrder(CharacterSettings character, ref bool changed)
    {
        var loc = state.Loc;

        ImGui.Spacing();
        ImGui.Separator();

        UiTheme.Caption(loc.Get(LocKeys.SettingsJobOrder), loc.Get(LocKeys.SettingsJobOrderHelp));

        var order = character.JobOrder;
        if (order.Count == 0)
        {
            UiTheme.Muted(loc.Get(LocKeys.SettingsJobOrderEmpty));
            return;
        }

        // The game's own order, which is what every reset here puts back. Read from the job
        // table rather than kept, so it stays right when a patch adds a job.
        var gameOrder = order
            .OrderBy(id => state.Jobs.TryGetValue(id, out var job) ? job.SortablePriority : int.MaxValue)
            .ThenBy(id => id)
            .ToList();

        if (ImGui.SmallButton($"{loc.Get(LocKeys.SettingsOrderResetAll)}##joborderall"))
        {
            character.JobOrder = [.. gameOrder];
            changed = true;
        }

        ImGui.Spacing();

        var rowHeight = ImGui.GetFrameHeight();
        var style = ImGui.GetStyle();
        var reserved = (rowHeight * 2f) + (style.ItemSpacing.X * 2f) + style.WindowPadding.X;

        foreach (var role in character.RoleOrder)
        {
            // Read fresh for each role from the one stored list, so a move writes back into that
            // list and there is no second copy of the order to keep in step.
            var inRole = order.Where(id => RoleOf(id) == role).ToList();
            if (inRole.Count == 0)
            {
                continue;
            }

            ImGui.Spacing();
            UiTheme.Muted(RoleName(role));
            ImGui.SameLine();

            // One reset per role, because that is the unit somebody arranges. Resetting the
            // tanks leaves the healers exactly where they were, which an assignment would not.
            if (ImGui.SmallButton($"{loc.Get(LocKeys.SettingsOrderReset)}##reset{role}"))
            {
                character.JobOrder = [.. JobOrderEditing.Reset(order, inRole, gameOrder)];
                changed = true;
            }

            for (var i = 0; i < inRole.Count; i++)
            {
                using var id = ImRaii.PushId($"job{inRole[i]}");

                var rowWidth = Math.Max(rowHeight * 3f, ImGui.GetContentRegionAvail().X - reserved);
                ImGui.Selectable(JobName(inRole[i]), false, ImGuiSelectableFlags.None,
                    new Vector2(rowWidth, rowHeight));

                ImGui.SameLine();

                // The arrows swap with the neighbour inside this role, never across the boundary
                // above or below it. There is nothing on the other side of that boundary that a
                // move could change.
                using (ImRaii.Disabled(i == 0))
                {
                    if (ImGui.ArrowButton("##up", ImGuiDir.Up))
                    {
                        Swap(order, inRole[i - 1], inRole[i]);
                        changed = true;
                    }
                }

                ImGui.SameLine();

                using (ImRaii.Disabled(i == inRole.Count - 1))
                {
                    if (ImGui.ArrowButton("##down", ImGuiDir.Down))
                    {
                        Swap(order, inRole[i], inRole[i + 1]);
                        changed = true;
                    }
                }
            }
        }
    }

    /// <summary>Swaps two jobs where they sit in the stored order.</summary>
    private static void Swap(List<uint> order, uint first, uint second)
    {
        var a = order.IndexOf(first);
        var b = order.IndexOf(second);

        if (a >= 0 && b >= 0)
        {
            (order[a], order[b]) = (order[b], order[a]);
        }
    }

    private JobRole RoleOf(uint classJobId) =>
        state.Jobs.TryGetValue(classJobId, out var job) ? job.Role : JobRole.Unknown;

    private string JobName(uint classJobId) =>
        state.Jobs.TryGetValue(classJobId, out var job)
            ? job.Name
            : state.Loc.Get(LocKeys.CommonUnknownJob);

    private string SortName(GearsetSortOrder order) => state.Loc.Get(order switch
    {
        GearsetSortOrder.Name => LocKeys.SortByName,
        GearsetSortOrder.Job => LocKeys.SortByJob,
        GearsetSortOrder.ItemLevel => LocKeys.SortByItemLevel,
        GearsetSortOrder.LastUsed => LocKeys.SortByLastUsed,
        GearsetSortOrder.Role => LocKeys.SortByRole,
        _ => LocKeys.SortBySlot,
    });

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
