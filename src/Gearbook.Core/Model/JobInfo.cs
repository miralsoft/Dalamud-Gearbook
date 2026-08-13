namespace Gearbook.Core.Model;

/// <summary>
/// What the core needs to know about a job. Read out of the game's job table by the plugin and
/// passed in, because the core must not reference the game data library. That indirection is
/// what makes the filters testable at all.
/// </summary>
/// <param name="Id">The game's own job id.</param>
/// <param name="Abbreviation">The three-letter form, for example WHM. Already localised by the
/// caller, because the game's job table is localised and the core does not translate it.</param>
/// <param name="Name">The full job name, already localised by the caller.</param>
/// <param name="Role">What the job does.</param>
/// <param name="Category">The coarse grouping the filter sidebar uses.</param>
public sealed record JobInfo(
    uint Id,
    string Abbreviation,
    string Name,
    JobRole Role,
    JobCategory Category)
{
    /// <summary>
    /// The stand-in used when a gearset names a job the job table does not describe. Returning
    /// this rather than throwing keeps one unreadable row from emptying the whole list, which
    /// matters because the row is read from game data that a patch can change under us.
    /// </summary>
    public static JobInfo Unknown(uint id) =>
        new(id, "???", "???", JobRole.Unknown, JobCategory.Unknown);
}
