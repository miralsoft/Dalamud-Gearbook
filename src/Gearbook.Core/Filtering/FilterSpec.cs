using Gearbook.Core.Model;

namespace Gearbook.Core.Filtering;

/// <summary>How much of the filter panel is on screen. Display only.</summary>
public enum FilterLevel
{
    /// <summary>Favourites and a search box. Nothing else.</summary>
    FavouritesOnly = 0,

    /// <summary>Search, roles and categories, favourites. The default.</summary>
    Simple,

    /// <summary>Every axis, plus saved views and tags.</summary>
    Full,
}

/// <summary>Whether a gearset that is missing a piece should be shown.</summary>
public enum CompletenessFilter
{
    Any = 0,
    IncompleteOnly,
    CompleteOnly,
}

/// <summary>
/// What the player is asking to see. Mutable because the interface edits it directly and this
/// is also the shape that gets persisted inside a saved view.
/// </summary>
public sealed class FilterSpec
{
    /// <summary>Free text. Split on whitespace, and every term has to match somewhere.</summary>
    public string Text { get; set; } = string.Empty;

    public List<JobRole> Roles { get; set; } = [];

    public List<JobCategory> Categories { get; set; } = [];

    /// <summary>Tags, compared case-insensitively. A gearset must carry all of them.</summary>
    public List<string> Tags { get; set; } = [];

    public bool FavouritesOnly { get; set; }

    public CompletenessFilter Completeness { get; set; }

    public bool GlamourLinkedOnly { get; set; }

    /// <summary>Show only sets this plugin has not equipped in this many days, or null.</summary>
    public int? UnusedForDays { get; set; }

    /// <summary>The sort order this filter is normally viewed in.</summary>
    public GearsetSortOrder Sort { get; set; } = GearsetSortOrder.Slot;

    /// <summary>True when nothing is being asked for, so every gearset passes.</summary>
    public bool IsEverything =>
        string.IsNullOrWhiteSpace(Text)
        && Roles.Count == 0
        && Categories.Count == 0
        && Tags.Count == 0
        && !FavouritesOnly
        && Completeness == CompletenessFilter.Any
        && !GlamourLinkedOnly
        && UnusedForDays is null;

    /// <summary>A deep copy, so that editing one view does not edit another.</summary>
    public FilterSpec Clone() => new()
    {
        Text = Text,
        Roles = [.. Roles],
        Categories = [.. Categories],
        Tags = [.. Tags],
        FavouritesOnly = FavouritesOnly,
        Completeness = Completeness,
        GlamourLinkedOnly = GlamourLinkedOnly,
        UnusedForDays = UnusedForDays,
        Sort = Sort,
    };

    /// <summary>
    /// The filter as it applies at a given level of the filter panel.
    /// </summary>
    /// <remarks>
    /// The stored filter is never changed by this, which is what makes the promise in the
    /// settings help text true: turning the panel down hides controls, it does not throw away
    /// the views and tags somebody already made. Turning it back up brings them back exactly as
    /// they were.
    /// </remarks>
    public FilterSpec AtLevel(FilterLevel level)
    {
        switch (level)
        {
            case FilterLevel.FavouritesOnly:
                return new FilterSpec
                {
                    Text = Text,
                    FavouritesOnly = true,
                    Sort = Sort,
                };

            case FilterLevel.Simple:
                return new FilterSpec
                {
                    Text = Text,
                    Roles = [.. Roles],
                    Categories = [.. Categories],
                    FavouritesOnly = FavouritesOnly,
                    Sort = Sort,
                };

            case FilterLevel.Full:
            default:
                return Clone();
        }
    }
}
