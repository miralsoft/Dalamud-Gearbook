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
/// <param name="DisplayPriority">Where the game itself puts this job in a list, from its own
/// `UIPriority` column. Zero when it is not known.</param>
/// <remarks>
/// The display priority is read rather than written down, and that is the point of it. It
/// reproduces the order the game's own character window uses exactly, tanks then healers then
/// melee and so on, each role in the sequence a player already knows. A job added in a future
/// patch arrives with its own number and files itself in the right place without a line changing
/// here, which a hand-kept list could not do.
/// </remarks>
public sealed record JobInfo(
    uint Id,
    string Abbreviation,
    string Name,
    JobRole Role,
    JobCategory Category,
    int DisplayPriority = 0)
{
    /// <summary>
    /// The stand-in used when a gearset names a job the job table does not describe. Returning
    /// this rather than throwing keeps one unreadable row from emptying the whole list, which
    /// matters because the row is read from game data that a patch can change under us.
    /// </summary>
    public static JobInfo Unknown(uint id) =>
        new(id, "???", "???", JobRole.Unknown, JobCategory.Unknown);

    /// <summary>
    /// The display priority as something to sort by, with "not known" sorting last.
    /// </summary>
    /// <remarks>
    /// Zero means the number was never read, and zero sorts first, so used raw it would put every
    /// undescribed job at the top of the list. An absence is not a first place.
    /// </remarks>
    public int SortablePriority => DisplayPriority <= 0 ? int.MaxValue : DisplayPriority;
}
