using Gearbook.Core.Model;
using Gearbook.Services;
using Lumina.Excel.Sheets;

namespace Gearbook.Adapters;

/// <summary>
/// The job table, as the core needs it.
/// </summary>
internal interface IJobDataSource
{
    /// <summary>Every job, keyed by the game's own job id.</summary>
    IReadOnlyDictionary<uint, JobInfo> Jobs { get; }

    /// <summary>The icon the game itself draws for a given gearset.</summary>
    uint IconIdFor(int gearsetSlot);
}

/// <summary>
/// Reads the job table through Lumina, which reads the local game files.
/// </summary>
/// <remarks>
/// Local rather than remote on purpose: the data is accurate, current, faster to reach, and
/// introduces no network dependency. Read once and held, because the table does not change while
/// the game is running and the filter asks for it every frame.
/// </remarks>
internal sealed unsafe class JobDataSource : IJobDataSource
{
    private readonly Dictionary<uint, JobInfo> jobs = [];

    public JobDataSource()
    {
        try
        {
            var sheet = GearbookServices.DataManager.GetExcelSheet<ClassJob>();

            foreach (var row in sheet)
            {
                if (row.RowId == 0)
                {
                    continue;
                }

                var (role, category) = JobClassifier.Classify(
                    row.Role,
                    row.ClassJobCategory.RowId,
                    row.PrimaryStat);

                var abbreviation = row.Abbreviation.ExtractText();
                var name = row.Name.ExtractText();

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                // The game's own list position, so a job added in a later patch files itself in
                // the right place without anything here changing.
                jobs[row.RowId] = new JobInfo(
                    row.RowId,
                    string.IsNullOrWhiteSpace(abbreviation) ? name : abbreviation,
                    name,
                    role,
                    category,
                    row.UIPriority);
            }

            GearbookServices.Log.Debug("Read {Count} jobs from the game data.", jobs.Count);
        }
        catch (Exception ex)
        {
            // A gearset with no job entry still lists, still switches, and only loses its role
            // filter and its readable job name. Degrading to that is better than a plugin that
            // does not load because one table could not be read.
            GearbookServices.Log.Error(
                ex,
                "The job table could not be read. Gearsets will be listed without their job names.");
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<uint, JobInfo> Jobs => jobs;

    /// <inheritdoc />
    public uint IconIdFor(int gearsetSlot)
    {
        var module = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.Instance();
        if (module is null)
        {
            return 0;
        }

        // The game's own icon for that gearset, so the bar looks like the hotbar it replaces
        // rather than like a reconstruction of it.
        var icon = module->GetClassJobIconForGearset(gearsetSlot);
        return icon > 0 ? (uint)icon : 0;
    }
}
