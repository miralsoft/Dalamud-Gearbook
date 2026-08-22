using Gearbook.Core.Identity;
using Gearbook.Core.Model;

namespace Gearbook.Core.Tests;

/// <summary>
/// Builders for the test suite. A test owns the values its verdict depends on, so nothing here
/// reads a shipped default: improving a default must not break a test that has nothing to do
/// with it.
/// </summary>
/// <remarks>
/// The job ids and the role each one carries below are the game's own (T-07): dark knight 32,
/// white mage 24, machinist 31, black mage 25, dragoon 22, culinarian 15, botanist 17, the same
/// table <see cref="Gearbook.Core.Tests.Model.JobClassifierTests"/> reads its inputs from. They
/// stand in here for "some real job of this role" rather than testing classification itself, but a
/// wrong id would still let a sorting or filtering test agree with a bug instead of catching one,
/// which is the failure T-07 exists to rule out.
/// </remarks>
internal static class TestData
{
    public const uint DarkKnightId = 32;
    public const uint WhiteMageId = 24;
    public const uint MachinistId = 31;
    public const uint BlackMageId = 25;
    public const uint DragoonId = 22;
    public const uint CulinarianId = 15;
    public const uint BotanistId = 17;

    public static readonly DateTimeOffset Now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    public static IReadOnlyDictionary<uint, JobInfo> Jobs { get; } = new Dictionary<uint, JobInfo>
    {
        [DarkKnightId] = new(DarkKnightId, "DRK", "Dark Knight", JobRole.Tank, JobCategory.Combat),
        [WhiteMageId] = new(WhiteMageId, "WHM", "White Mage", JobRole.Healer, JobCategory.Combat),
        [MachinistId] = new(MachinistId, "MCH", "Machinist", JobRole.PhysicalRangedDps, JobCategory.Combat),
        [BlackMageId] = new(BlackMageId, "BLM", "Black Mage", JobRole.MagicalRangedDps, JobCategory.Combat),
        [DragoonId] = new(DragoonId, "DRG", "Dragoon", JobRole.MeleeDps, JobCategory.Combat),
        [CulinarianId] = new(CulinarianId, "CUL", "Culinarian", JobRole.Crafter, JobCategory.Crafting),
        [BotanistId] = new(BotanistId, "BTN", "Botanist", JobRole.Gatherer, JobCategory.Gathering),
    };

    public static GearsetSnapshot Gearset(
        int slot,
        uint job,
        string name,
        int itemLevel = 700,
        bool mainHandMissing = false,
        int missingPieces = 0,
        byte? glamourPlate = null,
        string fingerprint = "fp") =>
        new(slot, job, name, itemLevel, mainHandMissing, missingPieces, glamourPlate, fingerprint);

    public static GearsetRecord Record(
        int id,
        uint job,
        string name,
        int slot,
        bool favourite = false,
        string note = "",
        int? barPosition = null,
        DateTimeOffset? lastUsed = null,
        params string[] tags) =>
        new(
            Id: id,
            ClassJobId: job,
            LastKnownName: name,
            LastKnownSlot: slot,
            LastKnownFingerprint: "fp",
            IsFavourite: favourite,
            Tags: tags,
            Note: note,
            BarPosition: barPosition,
            LastUsedUtc: lastUsed,
            LastSeenUtc: Now);

    public static ReconciledGearset Pair(GearsetRecord record, GearsetSnapshot gearset) =>
        new(record, gearset, MatchStage.Exact);

    /// <summary>
    /// A gearset and its record built together, with the record's job, name and slot taken from
    /// the gearset so a test only states what it actually cares about.
    /// </summary>
    public static ReconciledGearset Entry(
        int id,
        int slot,
        uint job,
        string name,
        bool favourite = false,
        string note = "",
        int? barPosition = null,
        DateTimeOffset? lastUsed = null,
        int itemLevel = 700,
        bool incomplete = false,
        byte? glamourPlate = null,
        params string[] tags)
    {
        var gearset = Gearset(
            slot,
            job,
            name,
            itemLevel,
            mainHandMissing: incomplete,
            glamourPlate: glamourPlate);

        var record = Record(id, job, name, slot, favourite, note, barPosition, lastUsed, tags);

        return Pair(record, gearset);
    }
}
