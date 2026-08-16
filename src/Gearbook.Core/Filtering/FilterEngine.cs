using Gearbook.Core.Identity;
using Gearbook.Core.Model;

namespace Gearbook.Core.Filtering;

/// <summary>
/// Applies a <see cref="FilterSpec"/> and an order to a list of gearsets.
/// </summary>
/// <remarks>
/// Pure, and given the job table rather than reading it, so every rule below is testable
/// without a running game. Job facts arrive as <see cref="JobInfo"/> from the plugin side.
/// </remarks>
public static class FilterEngine
{
    /// <summary>
    /// Filters and sorts in one pass.
    /// </summary>
    /// <param name="gearsets">Everything currently in the game, with its record attached.</param>
    /// <param name="filter">What to keep.</param>
    /// <param name="jobs">The job table, keyed by job id.</param>
    /// <param name="now">The clock, for the "not used in N days" axis.</param>
    /// <param name="roleOrder">The player's order for the roles, used only when sorting by
    /// role. Null falls back to the order the roles are declared in.</param>
    /// <param name="jobOrder">The player's order for the jobs within a role, used only when
    /// sorting by role. Null leaves the jobs alphabetical.</param>
    public static IReadOnlyList<ReconciledGearset> Apply(
        IEnumerable<ReconciledGearset> gearsets,
        FilterSpec filter,
        IReadOnlyDictionary<uint, JobInfo> jobs,
        DateTimeOffset now,
        IReadOnlyList<JobRole>? roleOrder = null,
        IReadOnlyList<uint>? jobOrder = null)
    {
        ArgumentNullException.ThrowIfNull(gearsets);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(jobs);

        var terms = SplitTerms(filter.Text);

        var kept = gearsets.Where(g => Matches(g, filter, jobs, terms, now));

        return Sort(kept, filter.Sort, jobs, roleOrder, jobOrder);
    }

    /// <summary>
    /// Orders a list without filtering it. Every order falls back to the game's own number for
    /// ties, so two sets that compare equal keep a stable position.
    /// </summary>
    public static IReadOnlyList<ReconciledGearset> Sort(
        IEnumerable<ReconciledGearset> gearsets,
        GearsetSortOrder order,
        IReadOnlyDictionary<uint, JobInfo> jobs,
        IReadOnlyList<JobRole>? roleOrder = null,
        IReadOnlyList<uint>? jobOrder = null)
    {
        ArgumentNullException.ThrowIfNull(gearsets);
        ArgumentNullException.ThrowIfNull(jobs);

        return order switch
        {
            // Grouped by role in the player's order, then by job so several sets of the same job
            // stay together, then by the game's number. Without the middle step a tank with two
            // jobs gets its sets interleaved, which is the opposite of what grouping is for.
            GearsetSortOrder.Role =>
                [.. gearsets
                    .OrderBy(g => RolePosition(JobFor(g, jobs).Role, roleOrder))
                    .ThenBy(g => JobPosition(g.Gearset.ClassJobId, jobOrder))
                    .ThenBy(g => JobFor(g, jobs).SortablePriority)
                    .ThenBy(g => g.Gearset.Slot)],

            GearsetSortOrder.Name =>
                [.. gearsets
                    .OrderBy(g => g.Gearset.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(g => g.Gearset.Slot)],

            GearsetSortOrder.Job =>
                [.. gearsets
                    .OrderBy(g => JobFor(g, jobs).Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(g => g.Gearset.Slot)],

            GearsetSortOrder.ItemLevel =>
                [.. gearsets
                    .OrderByDescending(g => g.Gearset.ItemLevel)
                    .ThenBy(g => g.Gearset.Slot)],

            // Never equipped goes last rather than first. A set with no history is not the
            // oldest, it is unknown, and sorting it to the top of "least recently used" would
            // be an answer drawn from an absence.
            GearsetSortOrder.LastUsed =>
                [.. gearsets
                    .OrderBy(g => g.Record.LastUsedUtc is null ? 1 : 0)
                    .ThenByDescending(g => g.Record.LastUsedUtc ?? DateTimeOffset.MinValue)
                    .ThenBy(g => g.Gearset.Slot)],

            GearsetSortOrder.Slot or _ =>
                [.. gearsets.OrderBy(g => g.Gearset.Slot)],
        };
    }

    /// <summary>
    /// The gearsets that share a job and a name with at least one other, which is the case the
    /// game itself cannot help the player with.
    /// </summary>
    public static IReadOnlyList<ReconciledGearset> FindDuplicates(
        IEnumerable<ReconciledGearset> gearsets)
    {
        ArgumentNullException.ThrowIfNull(gearsets);

        return [.. gearsets
            .GroupBy(g => (g.Gearset.ClassJobId, g.Gearset.Name), TupleComparer.Instance)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group)
            .OrderBy(g => g.Gearset.Slot)];
    }

    /// <summary>
    /// Every tag in use, sorted, deduplicated case-insensitively. What the filter panel offers.
    /// </summary>
    public static IReadOnlyList<string> CollectTags(IEnumerable<ReconciledGearset> gearsets)
    {
        ArgumentNullException.ThrowIfNull(gearsets);

        return [.. gearsets
            .SelectMany(g => g.Record.Tags)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(t => t, StringComparer.CurrentCultureIgnoreCase)];
    }

    private static bool Matches(
        ReconciledGearset gearset,
        FilterSpec filter,
        IReadOnlyDictionary<uint, JobInfo> jobs,
        string[] terms,
        DateTimeOffset now)
    {
        if (filter.FavouritesOnly && !gearset.Record.IsFavourite)
        {
            return false;
        }

        var job = JobFor(gearset, jobs);

        if (filter.Roles.Count > 0 && !filter.Roles.Contains(job.Role))
        {
            return false;
        }

        if (filter.Categories.Count > 0 && !filter.Categories.Contains(job.Category))
        {
            return false;
        }

        if (filter.Completeness == CompletenessFilter.IncompleteOnly && !gearset.Gearset.IsIncomplete)
        {
            return false;
        }

        if (filter.Completeness == CompletenessFilter.CompleteOnly && gearset.Gearset.IsIncomplete)
        {
            return false;
        }

        if (filter.GlamourLinkedOnly && gearset.Gearset.GlamourPlateLink is null)
        {
            return false;
        }

        if (filter.UnusedForDays is { } days)
        {
            // A set this plugin has never equipped counts as unused. That is honest: the
            // plugin genuinely has no record of it being worn, and saying so is more useful
            // than hiding it behind an absence.
            var lastUsed = gearset.Record.LastUsedUtc;
            if (lastUsed is not null && lastUsed > now.AddDays(-days))
            {
                return false;
            }
        }

        if (filter.Tags.Count > 0)
        {
            foreach (var wanted in filter.Tags)
            {
                if (!gearset.Record.Tags.Any(t =>
                        string.Equals(t.Trim(), wanted.Trim(), StringComparison.CurrentCultureIgnoreCase)))
                {
                    return false;
                }
            }
        }

        // Every term has to match somewhere. Two words are a narrowing, not a widening: typing
        // "dark ultimate" should find the ultimate set of the dark knight rather than every
        // dark knight set plus every ultimate set.
        foreach (var term in terms)
        {
            if (!MatchesTerm(gearset, job, term))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesTerm(ReconciledGearset gearset, JobInfo job, string term) =>
        Contains(gearset.Gearset.Name, term)
        || Contains(job.Name, term)
        || Contains(job.Abbreviation, term)
        || Contains(gearset.Record.Note, term)
        || gearset.Record.Tags.Any(t => Contains(t, term));

    private static bool Contains(string? haystack, string needle) =>
        haystack is not null
        && haystack.Contains(needle, StringComparison.CurrentCultureIgnoreCase);

    private static string[] SplitTerms(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Where a role sits in the player's order.
    /// </summary>
    /// <remarks>
    /// A role the stored order does not mention goes last rather than first. That is what
    /// happens to a role added in a later version of this plugin: the player's saved order
    /// predates it, and appending it is the answer that changes least about what they arranged.
    /// </remarks>
    /// <summary>
    /// Where a job sits in the player's own job order, or the end when they have not placed it.
    /// </summary>
    /// <remarks>
    /// Only ever consulted after the role has already decided, which is the whole scope of this
    /// setting: arranging jobs across roles would have no visible effect, because the role
    /// separates them first whatever the job order says. Sorting by job on its own stays
    /// alphabetical for the same reason in reverse: there, the roles are not separating anything,
    /// and a stored order grouped by role would silently turn "by job" into "by role, then job".
    ///
    /// A job with no position falls through to the game's own list position behind this, which is
    /// the order its character window uses: tanks, then healers, then melee, each role in the
    /// sequence a player already knows. That number is read from the job table rather than kept
    /// here, so a job added in a later patch takes its place without anything changing.
    /// </remarks>
    private static int JobPosition(uint classJobId, IReadOnlyList<uint>? order)
    {
        if (order is null)
        {
            return int.MaxValue;
        }

        for (var i = 0; i < order.Count; i++)
        {
            if (order[i] == classJobId)
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    private static int RolePosition(JobRole role, IReadOnlyList<JobRole>? order)
    {
        if (order is null)
        {
            return (int)role;
        }

        for (var i = 0; i < order.Count; i++)
        {
            if (order[i] == role)
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    private static JobInfo JobFor(ReconciledGearset gearset, IReadOnlyDictionary<uint, JobInfo> jobs) =>
        jobs.TryGetValue(gearset.Gearset.ClassJobId, out var job)
            ? job
            : JobInfo.Unknown(gearset.Gearset.ClassJobId);

    /// <summary>
    /// Groups a job id and a name with the same case-insensitive comparison the rest of the
    /// product uses for names, so "Dark Knight" and "dark knight" count as the same duplicate.
    /// </summary>
    private sealed class TupleComparer : IEqualityComparer<(uint Job, string Name)>
    {
        public static TupleComparer Instance { get; } = new();

        public bool Equals((uint Job, string Name) x, (uint Job, string Name) y) =>
            x.Job == y.Job
            && string.Equals(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);

        public int GetHashCode((uint Job, string Name) obj) =>
            HashCode.Combine(obj.Job, obj.Name.ToUpperInvariant());
    }
}
