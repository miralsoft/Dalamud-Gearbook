using Gearbook.Core.Model;

namespace Gearbook.Core.Identity;

/// <summary>
/// The outcome of matching what was saved against what the game currently holds.
/// </summary>
/// <param name="Present">Every gearset that exists in the game, with its record attached.</param>
/// <param name="Orphans">Saved records that matched nothing this time. Kept, not deleted: a
/// failed match and a deleted gearset look identical from here, and only one of them should
/// cost the player their notes.</param>
/// <param name="Ambiguities">Human-readable notes about matches that could not be made with
/// certainty. These are logged. An ambiguity is never resolved silently.</param>
/// <param name="NextId">The identifier to issue next. Passed back rather than held in the
/// reconciler so that the whole operation stays a pure function of its inputs.</param>
public sealed record ReconciliationResult(
    IReadOnlyList<ReconciledGearset> Present,
    IReadOnlyList<GearsetRecord> Orphans,
    IReadOnlyList<string> Ambiguities,
    int NextId)
{
    /// <summary>
    /// Everything that should be written back to the configuration: the records of the sets
    /// that exist plus the orphans that were kept.
    /// </summary>
    public IReadOnlyList<GearsetRecord> AllRecords =>
        [.. Present.Select(p => p.Record), .. Orphans];
}
