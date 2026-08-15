using Gearbook.Core.Identity;
using Gearbook.Core.Model;

namespace Gearbook.Core.Sorting;

/// <summary>What the quick-switch bar is currently showing.</summary>
public enum BarViewKind
{
    /// <summary>
    /// The favourites, which is the default. A bar is worth having because it is short enough to
    /// take in at a glance.
    /// </summary>
    Favourites = 0,

    /// <summary>Every gearset, for somebody who would rather not mark anything.</summary>
    All,

    /// <summary>One role: every tank, every crafter, and so on.</summary>
    Role,

    /// <summary>One category: everything for combat, for crafting, or for gathering.</summary>
    Category,

    /// <summary>
    /// Everything carrying one tag. The tags are the player's own words, so this is the view
    /// that grows without anybody adding a feature: tag a few sets as glamour and the bar can
    /// show exactly those.
    /// </summary>
    Tag,
}

/// <summary>
/// Picks what the bar holds.
/// </summary>
/// <remarks>
/// Separate from the library's filter on purpose. The library filter is a dozen axes somebody
/// composes deliberately; this is one choice made from a menu on the bar itself, in the middle
/// of doing something else. They would fight if they were the same thing: switching the bar to
/// the tanks would silently rewrite the filter somebody spent a minute building next door.
/// </remarks>
public static class BarView
{
    /// <summary>
    /// The roles the switcher offers, which is not every role there is.
    /// </summary>
    /// <remarks>
    /// Crafter and gatherer are missing on purpose. Every crafter is a disciple of the hand and
    /// every disciple of the hand is a crafter, so offering both the role and the category put
    /// the same entry in the menu twice under two names, which asks the player to work out a
    /// difference that does not exist. The category keeps it, because that is the level the other
    /// two entries beside it are on.
    ///
    /// The pairing is only exact for those two. A combat role is a genuine subdivision of the
    /// combat category, so a tank and "combat" are not the same list and both belong.
    /// </remarks>
    public static IReadOnlyList<JobRole> SelectableRoles { get; } =
    [
        JobRole.Tank,
        JobRole.Healer,
        JobRole.MeleeDps,
        JobRole.PhysicalRangedDps,
        JobRole.MagicalRangedDps,
    ];

    /// <summary>The categories the switcher offers.</summary>
    public static IReadOnlyList<JobCategory> SelectableCategories { get; } =
    [
        JobCategory.Combat,
        JobCategory.Crafting,
        JobCategory.Gathering,
    ];

    /// <summary>
    /// The gearsets a view holds, before ordering.
    /// </summary>
    /// <param name="gearsets">Everything the character has.</param>
    /// <param name="kind">Which view.</param>
    /// <param name="role">The role, when the view is a role.</param>
    /// <param name="category">The category, when the view is a category.</param>
    /// <param name="tag">The tag, when the view is a tag.</param>
    /// <param name="jobs">The job table, for resolving a gearset's role and category.</param>
    public static IReadOnlyList<ReconciledGearset> Select(
        IEnumerable<ReconciledGearset> gearsets,
        BarViewKind kind,
        JobRole role,
        JobCategory category,
        string? tag,
        IReadOnlyDictionary<uint, JobInfo> jobs)
    {
        ArgumentNullException.ThrowIfNull(gearsets);
        ArgumentNullException.ThrowIfNull(jobs);

        return kind switch
        {
            BarViewKind.All => Ordered(gearsets, includeEverything: true),

            BarViewKind.Role => Ordered(
                gearsets.Where(g => JobFor(g, jobs).Role == role),
                includeEverything: true),

            BarViewKind.Category => Ordered(
                gearsets.Where(g => JobFor(g, jobs).Category == category),
                includeEverything: true),

            // Compared the same way the library's tag filter compares, so a tag that matches
            // there matches here. Two ideas of what counts as the same tag would be a bug
            // waiting for the first player who capitalises inconsistently.
            BarViewKind.Tag => Ordered(
                gearsets.Where(g => HasTag(g, tag)),
                includeEverything: true),

            BarViewKind.Favourites or _ => Ordered(gearsets, includeEverything: false),
        };
    }

    private static bool HasTag(ReconciledGearset gearset, string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var wanted = tag.Trim();

        return gearset.Record.Tags.Any(t =>
            string.Equals(t.Trim(), wanted, StringComparison.CurrentCultureIgnoreCase));
    }

    /// <summary>
    /// True when the arrangement the player made still decides the order in this view.
    /// </summary>
    /// <remarks>
    /// Only in the favourites view. Everywhere else the bar holds gearsets that were never
    /// arranged, so the arrangement would order a handful of them and leave the rest in an
    /// arbitrary tail, which looks like a fault rather than a rule.
    /// </remarks>
    public static bool UsesArrangement(BarViewKind kind) => kind == BarViewKind.Favourites;

    private static IReadOnlyList<ReconciledGearset> Ordered(
        IEnumerable<ReconciledGearset> gearsets,
        bool includeEverything) =>
        BarOrder.OnBar(gearsets, includeEverything);

    private static JobInfo JobFor(ReconciledGearset gearset, IReadOnlyDictionary<uint, JobInfo> jobs) =>
        jobs.TryGetValue(gearset.Gearset.ClassJobId, out var job)
            ? job
            : JobInfo.Unknown(gearset.Gearset.ClassJobId);
}
