using Gearbook.Core.Identity;

namespace Gearbook.Core.Model;

/// <summary>
/// Which shortcuts to other plugins belong at the end of the bar, decided by the gearsets the bar
/// is actually showing rather than by the view that selected them.
/// </summary>
/// <remarks>
/// <para>
/// Deciding by the view meant a favourites or tag bar never offered anything, however many
/// crafters it held. Deciding by the shown sets covers every view with one rule, including views
/// added later.
/// </para>
/// <para>
/// A gearset whose job the table does not describe counts for nothing here. That is deliberate:
/// a set that is not a combat, crafting or gathering set has no best-in-slot target and no
/// crafting plugin to open, so it should not pull a shortcut onto the bar.
/// </para>
/// </remarks>
public static class ExternalToolPlacement
{
    /// <summary>The categories present among the given gearsets.</summary>
    public static IReadOnlySet<JobCategory> CategoriesShown(
        IEnumerable<ReconciledGearset> gearsets,
        IReadOnlyDictionary<uint, JobInfo> jobs)
    {
        ArgumentNullException.ThrowIfNull(gearsets);
        ArgumentNullException.ThrowIfNull(jobs);

        return gearsets
            .Select(g => jobs.TryGetValue(g.Gearset.ClassJobId, out var job) ? job.Category : JobCategory.Unknown)
            .ToHashSet();
    }

    /// <summary>
    /// The shortcuts to offer, in the order they are drawn.
    /// </summary>
    /// <param name="shown">The categories among the gearsets on the bar.</param>
    /// <param name="inCosmicExploration">Whether the player is standing in cosmic exploration
    /// content, which is the only place Ice's plugin has anything to offer.</param>
    public static IReadOnlyList<ExternalTool> For(IReadOnlySet<JobCategory> shown, bool inCosmicExploration)
    {
        ArgumentNullException.ThrowIfNull(shown);

        var crafting = shown.Contains(JobCategory.Crafting);
        var gathering = shown.Contains(JobCategory.Gathering);
        var combat = shown.Contains(JobCategory.Combat);

        var tools = new List<ExternalTool>(3);

        if (crafting)
        {
            tools.Add(ExternalTool.Artisan);
        }

        // Eorzea Arsenal compares crafting and gathering gear against a target as well as combat
        // gear, so any of the three brings it.
        if (combat || crafting || gathering)
        {
            tools.Add(ExternalTool.Arsenal);
        }

        if ((crafting || gathering) && inCosmicExploration)
        {
            tools.Add(ExternalTool.Cosmic);
        }

        return tools;
    }
}
