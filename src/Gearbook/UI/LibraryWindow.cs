using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Gearbook.Adapters;
using Gearbook.Core.Bis;
using Gearbook.Core.Filtering;
using Gearbook.Core.Identity;
using Gearbook.Core.Localization;
using Gearbook.Core.Model;
using Gearbook.Core.Sorting;
using Gearbook.Core.Views;

namespace Gearbook.UI;

/// <summary>
/// The management view: search, filters, and everything the player owns about a gearset.
/// </summary>
internal sealed class LibraryWindow : Window
{
    private const string WindowId = "Gearbook###GearbookLibrary";
    private const int NoteMaxLength = 2000;
    private const int TagsMaxLength = 500;

    private readonly GearbookState state;

    private int selectedRecordId = -1;
    private string noteBuffer = string.Empty;
    private string tagsBuffer = string.Empty;
    private int editingRecordId = -1;
    private string newViewName = string.Empty;
    private readonly TitleBarButton newsLink;
    private bool unreadNow;

    public LibraryWindow(GearbookState state, Action openBar, Action openSettings, Action openNews)
        : base(WindowId)
    {
        this.state = state;

        TitleBarButtons.Add(UiTheme.Link(
            FontAwesomeIcon.ThLarge,
            () => state.Loc.Get(LocKeys.WindowBarTitle),
            openBar,
            priority: 0));

        TitleBarButtons.Add(UiTheme.Link(
            FontAwesomeIcon.Cog,
            () => state.Loc.Get(LocKeys.WindowSettingsTitle),
            openSettings,
            priority: 1));

        // Kept as a field so its colour can be changed every frame. After the notes appear once
        // by themselves, this is the only signal that there is anything to come back to, and a
        // control that never says it has something is one nobody presses.
        newsLink = UiTheme.Link(
            FontAwesomeIcon.Book,
            () => state.Loc.Get(unreadNow ? LocKeys.NewsUnread : LocKeys.WindowNewsTitle),
            openNews,
            priority: 2);

        TitleBarButtons.Add(newsLink);

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(760, 420),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    /// <inheritdoc />
    public override void PreDraw()
    {
        WindowName = $"{state.Loc.Get(LocKeys.WindowLibraryTitle)}###GearbookLibrary";

        // Every frame rather than once, because it has to go out the moment the notes are
        // opened and the language underneath it can change while the window is up.
        var character = state.Character;
        unreadNow = character is not null && ReleaseNotesState.HasUnread(character);
        newsLink.IconColor = unreadNow ? UiTheme.UnreadColour : null;
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

        var filter = character.CurrentFilter;
        var effective = filter.AtLevel(character.FilterLevel);
        var shown = FilterEngine.Apply(state.Gearsets, effective, state.Jobs, DateTimeOffset.UtcNow);

        DrawHeader(character, filter, shown.Count);
        ImGui.Separator();

        var showSidebar = character.FilterLevel != FilterLevel.FavouritesOnly;
        var sidebarWidth = showSidebar ? 230f : 0f;

        if (showSidebar)
        {
            using (var sidebar = ImRaii.Child("##filters", new Vector2(sidebarWidth, 0), true))
            {
                if (sidebar)
                {
                    DrawFilters(character, filter);
                }
            }

            ImGui.SameLine();
        }

        using (var list = ImRaii.Child("##list", new Vector2(-300f, 0), true))
        {
            if (list)
            {
                DrawList(character, shown);
            }
        }

        ImGui.SameLine();

        using var detail = ImRaii.Child("##detail", new Vector2(0, 0), true);
        if (detail)
        {
            DrawDetail(character, shown);
        }
    }

    private void DrawHeader(Core.Settings.CharacterSettings character, FilterSpec filter, int shownCount)
    {
        var loc = state.Loc;

        ImGui.SetNextItemWidth(240f);
        var text = filter.Text;
        if (ImGui.InputTextWithHint("##search", loc.Get(LocKeys.CommonSearch), ref text, 200))
        {
            filter.Text = text;
            state.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button(loc.Get(LocKeys.CommonClear)))
        {
            filter.Text = string.Empty;
            state.Save();
        }

        ImGui.SameLine();
        UiTheme.Muted(loc.Get(LocKeys.LibraryCount, shownCount, state.Gearsets.Count));

        // The order the list is read in. Stored with the filter, so a saved view remembers how
        // it was being looked at as well as what it showed.
        ImGui.SameLine();
        var orders = Enum.GetValues<GearsetSortOrder>();
        var orderLabels = orders.Select(SortName).ToList();
        var orderIndex = Math.Max(0, Array.IndexOf(orders, filter.Sort));

        // The caption goes inside the control rather than beside it, because the header is a
        // single row and a label to the right of a combo pushes everything after it off the end.
        ImGui.SetNextItemWidth(190f);
        if (ImGui.Combo("##sort", ref orderIndex, orderLabels, orderLabels.Count))
        {
            filter.Sort = orders[orderIndex];
            state.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(loc.Get(LocKeys.SortHeading));
        }

        // The ways to the other windows used to sit here as buttons. They live in the title bar
        // now, which is where the host puts its own controls and therefore where a player looks,
        // and having them in both places was two answers to one question.
    }

    private void DrawFilters(Core.Settings.CharacterSettings character, FilterSpec filter)
    {
        var loc = state.Loc;
        var changed = false;

        ImGui.TextUnformatted(loc.Get(LocKeys.FilterHeading));
        ImGui.SameLine();

        // Clearing everything is one control rather than unticking eight. It empties the filter
        // without touching the saved views, which is the difference between starting again and
        // losing work.
        using (ImRaii.Disabled(filter.IsEverything))
        {
            if (ImGui.SmallButton(loc.Get(LocKeys.ViewEverything)))
            {
                character.CurrentFilter = new FilterSpec { Sort = filter.Sort };
                character.ActiveViewName = string.Empty;
                state.Save();
                return;
            }
        }

        ImGui.Separator();

        var favourites = filter.FavouritesOnly;
        if (UiTheme.WrappedCheckbox("favonly", loc.Get(LocKeys.FilterFavouritesOnly), ref favourites))
        {
            filter.FavouritesOnly = favourites;
            changed = true;
        }

        ImGui.Separator();
        UiTheme.Muted(loc.Get(LocKeys.FilterRoles));

        foreach (var role in Enum.GetValues<JobRole>())
        {
            if (role == JobRole.Unknown)
            {
                continue;
            }

            var selected = filter.Roles.Contains(role);
            if (UiTheme.WrappedCheckbox($"role{role}", RoleName(role), ref selected))
            {
                if (selected)
                {
                    filter.Roles.Add(role);
                }
                else
                {
                    filter.Roles.Remove(role);
                }

                changed = true;
            }
        }

        ImGui.Separator();
        UiTheme.Muted(loc.Get(LocKeys.FilterCategories));

        foreach (var category in Enum.GetValues<JobCategory>())
        {
            if (category == JobCategory.Unknown)
            {
                continue;
            }

            var selected = filter.Categories.Contains(category);
            if (UiTheme.WrappedCheckbox($"cat{category}", CategoryName(category), ref selected))
            {
                if (selected)
                {
                    filter.Categories.Add(category);
                }
                else
                {
                    filter.Categories.Remove(category);
                }

                changed = true;
            }
        }

        if (character.FilterLevel == FilterLevel.Full)
        {
            changed |= DrawFullFilters(filter);
        }

        if (changed)
        {
            state.Save();
        }
    }

    private bool DrawFullFilters(FilterSpec filter)
    {
        var loc = state.Loc;
        var changed = false;

        ImGui.Separator();
        UiTheme.Muted(loc.Get(LocKeys.FilterTags));

        foreach (var tag in FilterEngine.CollectTags(state.Gearsets))
        {
            var selected = filter.Tags.Any(t =>
                string.Equals(t, tag, StringComparison.CurrentCultureIgnoreCase));

            if (UiTheme.WrappedCheckbox($"tag{tag}", tag, ref selected))
            {
                if (selected)
                {
                    filter.Tags.Add(tag);
                }
                else
                {
                    filter.Tags.RemoveAll(t =>
                        string.Equals(t, tag, StringComparison.CurrentCultureIgnoreCase));
                }

                changed = true;
            }
        }

        ImGui.Separator();

        var incomplete = filter.Completeness == CompletenessFilter.IncompleteOnly;
        if (UiTheme.WrappedCheckbox("incomplete", loc.Get(LocKeys.FilterIncompleteOnly), ref incomplete))
        {
            filter.Completeness = incomplete ? CompletenessFilter.IncompleteOnly : CompletenessFilter.Any;
            changed = true;
        }

        var onBar = filter.BarMembership == BarMembershipFilter.OnBarOnly;
        if (UiTheme.WrappedCheckbox("onbaronly", loc.Get(LocKeys.FilterOnBarOnly), ref onBar))
        {
            filter.BarMembership = onBar ? BarMembershipFilter.OnBarOnly : BarMembershipFilter.Any;
            changed = true;
        }

        var notOnBar = filter.BarMembership == BarMembershipFilter.NotOnBarOnly;
        if (UiTheme.WrappedCheckbox("notonbar", loc.Get(LocKeys.FilterNotOnBarOnly), ref notOnBar))
        {
            filter.BarMembership = notOnBar ? BarMembershipFilter.NotOnBarOnly : BarMembershipFilter.Any;
            changed = true;
        }

        var glamour = filter.GlamourLinkedOnly;
        if (UiTheme.WrappedCheckbox("glamlinked", loc.Get(LocKeys.FilterGlamourLinkedOnly), ref glamour))
        {
            filter.GlamourLinkedOnly = glamour;
            changed = true;
        }

        var unusedDays = filter.UnusedForDays ?? 0;
        UiTheme.Caption(loc.Get(LocKeys.FilterUnusedSince, unusedDays));
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.SliderInt("##unuseddays", ref unusedDays, 0, 365))
        {
            filter.UnusedForDays = unusedDays == 0 ? null : unusedDays;
            changed = true;
        }

        changed |= DrawViews(filter);

        return changed;
    }

    private bool DrawViews(FilterSpec filter)
    {
        var loc = state.Loc;
        var character = state.Character;
        if (character is null)
        {
            return false;
        }

        ImGui.Separator();
        UiTheme.Muted(loc.Get(LocKeys.ViewActive));

        var changed = false;

        foreach (var view in character.Views.ToList())
        {
            using var id = ImRaii.PushId(view.Name);

            var active = string.Equals(character.ActiveViewName, view.Name, StringComparison.Ordinal);
            if (ImGui.Selectable(view.Name, active))
            {
                character.ActiveViewName = view.Name;
                character.CurrentFilter = view.Filter.Clone();
                changed = true;
            }

            using var context = ImRaii.ContextPopupItem($"##view-{view.Name}");
            if (context)
            {
                if (ImGui.MenuItem(loc.Get(LocKeys.CommonDelete)))
                {
                    character.Views.Remove(view);
                    if (active)
                    {
                        character.ActiveViewName = string.Empty;
                    }

                    changed = true;
                }
            }
        }

        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##newview", loc.Get(LocKeys.ViewNew), ref newViewName, 60);

        var problem = SavedViews.CheckName(character.Views, newViewName);

        using (ImRaii.Disabled(problem != SavedViews.NameProblem.None))
        {
            if (ImGui.Button(loc.Get(LocKeys.CommonSave)))
            {
                character.Views.Add(new SavedView
                {
                    Name = newViewName.Trim(),
                    Filter = filter.Clone(),
                });

                character.ActiveViewName = newViewName.Trim();
                newViewName = string.Empty;
                changed = true;
            }
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(loc.Get(LocKeys.ViewSaveAs));
        }

        // Both reasons a name is refused are said out loud. A disabled button with no
        // explanation is a control somebody presses twice and then gives up on.
        switch (problem)
        {
            case SavedViews.NameProblem.AlreadyTaken:
                UiTheme.Muted(loc.Get(LocKeys.ViewNameTaken));
                break;

            case SavedViews.NameProblem.Empty when newViewName.Length > 0:
                UiTheme.Muted(loc.Get(LocKeys.ViewNameEmpty));
                break;

            default:
                break;
        }

        return changed;
    }

    private void DrawList(Core.Settings.CharacterSettings character, IReadOnlyList<ReconciledGearset> shown)
    {
        var loc = state.Loc;

        if (shown.Count == 0)
        {
            UiTheme.Muted(loc.Get(LocKeys.LibraryNoMatches));
            return;
        }

        var duplicates = character.Library.WarnAboutDuplicates
            ? FilterEngine.FindDuplicates(state.Gearsets).Select(g => g.Record.Id).ToHashSet()
            : [];

        // The best-in-slot column takes no width at all unless there is something to put in it.
        // The common failure with an integration like this is building the interface around the
        // extra information, after which the plugin looks broken to everybody without it.
        var showBis = state.Bis.HasAnything;

        var columns = 4 + (character.Library.ShowGameNumber ? 1 : 0) + (showBis ? 1 : 0);

        using var table = ImRaii.Table("##gearsets", columns,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp);

        if (!table)
        {
            return;
        }

        if (character.Library.ShowGameNumber)
        {
            ImGui.TableSetupColumn(loc.Get(LocKeys.LibraryColumnNumber), ImGuiTableColumnFlags.WidthFixed, 34f);
        }

        ImGui.TableSetupColumn(loc.Get(LocKeys.LibraryColumnName));
        ImGui.TableSetupColumn(loc.Get(LocKeys.LibraryColumnJob), ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn(loc.Get(LocKeys.LibraryColumnItemLevel), ImGuiTableColumnFlags.WidthFixed, 60f);

        if (showBis)
        {
            ImGui.TableSetupColumn(loc.Get(LocKeys.LibraryColumnBis), ImGuiTableColumnFlags.WidthFixed, 70f);
        }

        ImGui.TableSetupColumn(loc.Get(LocKeys.LibraryColumnTags));
        ImGui.TableHeadersRow();

        foreach (var gearset in shown)
        {
            ImGui.TableNextRow();
            using var id = ImRaii.PushId(gearset.Record.Id);

            if (character.Library.ShowGameNumber)
            {
                ImGui.TableNextColumn();
                UiTheme.Muted((gearset.Gearset.Slot + 1).ToString(CultureInfo.CurrentCulture));
            }

            ImGui.TableNextColumn();

            var selected = selectedRecordId == gearset.Record.Id;
            if (ImGui.Selectable(gearset.Gearset.Name, selected, ImGuiSelectableFlags.SpanAllColumns))
            {
                // The same gesture as on the bar. A modifier that means one thing in one window
                // and another thing in the next is worse than not having it at all.
                if (UiTheme.FavouriteModifierHeld)
                {
                    UiTheme.ToggleFavourite(state, gearset);
                }
                else
                {
                    Select(gearset);
                }
            }

            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                state.RequestEquip(gearset.Gearset.Slot, EquipTrigger.Library);
            }

            // The same menu the bar's tiles carry, bound to the row that was just drawn.
            UiTheme.GearsetContextMenu(state, gearset);

            if (duplicates.Contains(gearset.Record.Id))
            {
                ImGui.SameLine();
                UiTheme.Muted("!");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(loc.Get(LocKeys.LibraryDuplicateWarning));
                }
            }

            ImGui.TableNextColumn();
            UiTheme.Muted(state.Jobs.TryGetValue(gearset.Gearset.ClassJobId, out var job)
                ? job.Name
                : loc.Get(LocKeys.CommonUnknownJob));

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(gearset.Gearset.ItemLevel.ToString(CultureInfo.CurrentCulture));

            if (showBis)
            {
                ImGui.TableNextColumn();
                var badge = state.Bis.For(gearset.Gearset.Slot);
                if (badge is not null)
                {
                    ImGui.TextUnformatted($"{badge.Matched}/{badge.Total}");
                }
            }

            ImGui.TableNextColumn();
            UiTheme.Muted(string.Join(", ", gearset.Record.Tags));
        }
    }

    private void DrawDetail(Core.Settings.CharacterSettings character, IReadOnlyList<ReconciledGearset> shown)
    {
        var loc = state.Loc;

        var selected = state.Gearsets.FirstOrDefault(g => g.Record.Id == selectedRecordId);
        if (selected is null)
        {
            UiTheme.Muted(loc.Get(LocKeys.LibraryDetailNothingSelected));
            DrawOrphans(character);
            return;
        }

        if (editingRecordId != selected.Record.Id)
        {
            Select(selected);
        }

        ImGui.TextUnformatted(selected.Gearset.Name);
        UiTheme.Muted(state.Jobs.TryGetValue(selected.Gearset.ClassJobId, out var job)
            ? job.Name
            : loc.Get(LocKeys.CommonUnknownJob));

        var blocked = state.CheckCanEquip(selected);
        using (ImRaii.Disabled(blocked != EquipOutcome.Sent))
        {
            if (ImGui.Button(loc.Get(LocKeys.LibraryDetailEquip)))
            {
                state.RequestEquip(selected.Gearset.Slot, EquipTrigger.Library);
            }
        }

        var reason = UiTheme.BlockedReason(loc, blocked);
        if (reason is not null)
        {
            UiTheme.Muted(reason);
        }

        ImGui.Separator();

        var favourite = selected.Record.IsFavourite;
        if (UiTheme.WrappedCheckbox("detailfav", loc.Get(LocKeys.LibraryDetailFavourite), ref favourite))
        {
            state.UpdateRecords(records =>
                [.. records.Select(r => r.Id == selected.Record.Id ? r with { IsFavourite = favourite } : r)]);
        }

        var onBar = selected.Record.BarPosition is not null;
        if (UiTheme.WrappedCheckbox("detailonbar", loc.Get(LocKeys.LibraryDetailOnBar), ref onBar))
        {
            state.UpdateRecords(records => BarOrder.SetOnBar(records, selected.Record.Id, onBar));
        }

        // The bar's order is the player's, so it needs a control. Only shown for a set that is
        // actually on it, because moving something that is not there has no meaning.
        if (onBar)
        {
            ImGui.SameLine();
            if (ImGui.SmallButton("<##barleft"))
            {
                state.UpdateRecords(records => BarOrder.Move(records, selected.Record.Id, -1));
            }

            ImGui.SameLine();
            if (ImGui.SmallButton(">##barright"))
            {
                state.UpdateRecords(records => BarOrder.Move(records, selected.Record.Id, 1));
            }

            ImGui.SameLine();
            UiTheme.Muted((selected.Record.BarPosition!.Value + 1).ToString(CultureInfo.CurrentCulture));
        }

        ImGui.Separator();
        ImGui.TextUnformatted(loc.Get(LocKeys.LibraryDetailTags));
        UiTheme.HelpMarker(loc.Get(LocKeys.LibraryDetailTagsHint));

        if (ImGui.InputText("##tags", ref tagsBuffer, TagsMaxLength))
        {
            var tags = tagsBuffer
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            state.UpdateRecords(records =>
                [.. records.Select(r => r.Id == selected.Record.Id ? r with { Tags = tags } : r)]);
        }

        ImGui.TextUnformatted(loc.Get(LocKeys.LibraryDetailNote));
        if (ImGui.InputTextMultiline("##note", ref noteBuffer, NoteMaxLength, new Vector2(-1, 90)))
        {
            var note = noteBuffer;
            state.UpdateRecords(records =>
                [.. records.Select(r => r.Id == selected.Record.Id ? r with { Note = note } : r)]);
        }

        ImGui.Separator();

        // What the game knows about the set's equipment. Only the counts, because that is what
        // the gearset entry carries; reading the fourteen slots individually would mean reading
        // the pieces themselves, which this window has no reason to do.
        var pieces = selected.Gearset.MissingPieceCount == 0 && !selected.Gearset.MainHandMissing
            ? loc.Get(LocKeys.LibraryDetailPieces)
            : $"{loc.Get(LocKeys.LibraryDetailPieces)}: {selected.Gearset.MissingPieceCount}";

        UiTheme.Muted(pieces);

        if (selected.Gearset.GlamourPlateLink is { } plate)
        {
            UiTheme.Muted(loc.Get(LocKeys.LibraryDetailGlamourPlate, plate));
        }

        UiTheme.Muted($"{loc.Get(LocKeys.LibraryDetailLastUsed)}: " +
                      (selected.Record.LastUsedUtc is { } used
                          ? used.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
                          : loc.Get(LocKeys.CommonNever)));

        if (selected.Gearset.IsIncomplete)
        {
            UiTheme.Muted(loc.Get(LocKeys.GearsetIncomplete));
        }

        DrawBisState();
        DrawOrphans(character);
    }

    private void DrawBisState()
    {
        var loc = state.Loc;

        // A feature that switches itself off says so somewhere the player can find, otherwise
        // the whole plugin is assumed broken. Nothing is drawn when no provider is installed at
        // all, because that is not a state, it is the normal case.
        var message = state.Bis.State switch
        {
            BisProviderState.NoAccount => loc.Get(LocKeys.BisStateNoAccount),
            BisProviderState.Loading => loc.Get(LocKeys.BisStateLoading),
            BisProviderState.NoData => loc.Get(LocKeys.BisStateNoData),
            _ => null,
        };

        if (message is null)
        {
            return;
        }

        ImGui.Separator();
        UiTheme.Muted(message);
    }

    private void DrawOrphans(Core.Settings.CharacterSettings character)
    {
        if (!character.Library.ShowOrphans || state.Orphans.Count == 0)
        {
            return;
        }

        var loc = state.Loc;

        ImGui.Separator();
        ImGui.TextUnformatted(loc.Get(LocKeys.LibraryOrphanHeading));
        UiTheme.HelpMarker(loc.Get(LocKeys.LibraryOrphanExplain));

        foreach (var orphan in state.Orphans)
        {
            using var id = ImRaii.PushId($"orphan-{orphan.Id}");

            UiTheme.Muted(orphan.LastKnownName);

            ImGui.SameLine();
            if (ImGui.SmallButton(loc.Get(LocKeys.LibraryOrphanForget)))
            {
                state.UpdateRecords(records => [.. records.Where(r => r.Id != orphan.Id)]);
            }
        }
    }

    private void Select(ReconciledGearset gearset)
    {
        selectedRecordId = gearset.Record.Id;
        editingRecordId = gearset.Record.Id;
        noteBuffer = gearset.Record.Note;
        tagsBuffer = string.Join(", ", gearset.Record.Tags);
    }

    private string SortName(GearsetSortOrder order) => state.Loc.Get(order switch
    {
        GearsetSortOrder.Name => LocKeys.SortByName,
        GearsetSortOrder.Job => LocKeys.SortByJob,
        GearsetSortOrder.ItemLevel => LocKeys.SortByItemLevel,
        GearsetSortOrder.LastUsed => LocKeys.SortByLastUsed,
        _ => LocKeys.SortBySlot,
    });

    private string CategoryName(JobCategory category) => state.Loc.Get(category switch
    {
        JobCategory.Combat => LocKeys.CategoryCombat,
        JobCategory.Crafting => LocKeys.CategoryCrafting,
        JobCategory.Gathering => LocKeys.CategoryGathering,
        _ => LocKeys.CategoryUnknown,
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
}
