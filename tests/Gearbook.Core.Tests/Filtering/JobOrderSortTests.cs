using Gearbook.Core.Filtering;
using Gearbook.Core.Identity;
using Gearbook.Core.Model;
using Xunit;

namespace Gearbook.Core.Tests.Filtering;

/// <summary>
/// Sorting by role, with the player's own order for the jobs inside each role.
/// </summary>
/// <remarks>
/// Its own job table rather than the shared one, because the verdict depends on having two jobs
/// in a single role and the shared table has one of each. A test owns the values its verdict
/// rests on.
/// </remarks>
public sealed class JobOrderSortTests
{
    private const uint Paladin = 19;
    private const uint DarkKnight = 32;
    private const uint Gunbreaker = 37;
    private const uint WhiteMage = 24;

    private static readonly IReadOnlyDictionary<uint, JobInfo> Jobs = new Dictionary<uint, JobInfo>
    {
        [Paladin] = new(Paladin, "PLD", "Paladin", JobRole.Tank, JobCategory.Combat, 1),
        [DarkKnight] = new(DarkKnight, "DRK", "Dark Knight", JobRole.Tank, JobCategory.Combat, 5),
        [Gunbreaker] = new(Gunbreaker, "GNB", "Gunbreaker", JobRole.Tank, JobCategory.Combat, 6),
        [WhiteMage] = new(WhiteMage, "WHM", "White Mage", JobRole.Healer, JobCategory.Combat, 11),
    };

    private static readonly IReadOnlyList<JobRole> RoleOrder =
        [JobRole.Tank, JobRole.Healer, JobRole.MeleeDps, JobRole.PhysicalRangedDps, JobRole.MagicalRangedDps];

    [Fact]
    public void WithoutAJobOrderTheJobsTakeTheGamesOwnListPosition()
    {
        var sorted = Sort(null);

        // The order the game's own character window uses, which is what these priorities are.
        Assert.Equal([Paladin, DarkKnight, Gunbreaker, WhiteMage], Jobs2(sorted));
    }

    [Fact]
    public void TheJobOrderDecidesInsideTheRole()
    {
        var sorted = Sort([DarkKnight, Gunbreaker, Paladin]);

        Assert.Equal([DarkKnight, Gunbreaker, Paladin, WhiteMage], Jobs2(sorted));

        var reversed = Sort([Paladin, Gunbreaker, DarkKnight]);

        Assert.Equal([Paladin, Gunbreaker, DarkKnight, WhiteMage], Jobs2(reversed));
    }

    [Fact]
    public void AJobOrderCannotMoveAJobPastItsRole()
    {
        // The point of the whole setting, and the reason it is presented per role: the role
        // separates first, so no arrangement of the jobs can put the healer among the tanks.
        var sorted = Sort([WhiteMage, DarkKnight, Gunbreaker, Paladin]);

        Assert.Equal([DarkKnight, Gunbreaker, Paladin, WhiteMage], Jobs2(sorted));
    }

    [Fact]
    public void AJobMissingFromTheOrderFallsInBehindTheOnesThatArePlaced()
    {
        // And in the game's own list position among themselves, which is the tiebreak sitting
        // behind the order.
        var sorted = Sort([Gunbreaker]);

        Assert.Equal([Gunbreaker, Paladin, DarkKnight, WhiteMage], Jobs2(sorted));
    }

    [Fact]
    public void TwoSetsOfOneJobStayTogetherAndKeepTheirSlotOrder()
    {
        var gearsets = new List<ReconciledGearset>
        {
            Set(5, Paladin),
            Set(1, DarkKnight),
            Set(3, Paladin),
        };

        var sorted = FilterEngine.Sort(
            gearsets,
            GearsetSortOrder.Role,
            Jobs,
            RoleOrder,
            [Paladin, DarkKnight]);

        Assert.Equal([3, 5, 1], sorted.Select(g => g.Gearset.Slot));
    }

    /// <summary>
    /// Crafters and gatherers take the game's order too, from the same column.
    /// </summary>
    /// <remarks>
    /// Worth its own test because the numbers look like a separate scheme: the crafters run from
    /// 101 and the gatherers from 201, where the combat jobs run from 1. They are the real values
    /// out of the job table. Nothing has to reconcile the ranges, because the role sorts first and
    /// two jobs from different ranges are therefore never compared with each other.
    /// </remarks>
    [Fact]
    public void CraftersAndGatherersTakeTheGamesOrderFromTheSameColumn()
    {
        const uint Carpenter = 8;
        const uint Culinarian = 15;
        const uint Miner = 16;
        const uint Fisher = 18;

        var jobs = new Dictionary<uint, JobInfo>
        {
            [Carpenter] = new(Carpenter, "CRP", "Carpenter", JobRole.Crafter, JobCategory.Crafting, 101),
            [Culinarian] = new(Culinarian, "CUL", "Culinarian", JobRole.Crafter, JobCategory.Crafting, 108),
            [Miner] = new(Miner, "MIN", "Miner", JobRole.Gatherer, JobCategory.Gathering, 201),
            [Fisher] = new(Fisher, "FSH", "Fisher", JobRole.Gatherer, JobCategory.Gathering, 203),
        };

        // Deliberately arriving in the wrong order, and alphabetically the other way round from
        // the answer, so neither a sort that did nothing nor the old alphabetical one would pass.
        var gearsets = new List<ReconciledGearset>
        {
            Set(1, Fisher),
            Set(2, Culinarian),
            Set(3, Miner),
            Set(4, Carpenter),
        };

        var sorted = FilterEngine.Sort(
            gearsets,
            GearsetSortOrder.Role,
            jobs,
            [JobRole.Crafter, JobRole.Gatherer],
            jobOrder: null);

        Assert.Equal([Carpenter, Culinarian, Miner, Fisher], Jobs2(sorted));
    }

    private static IReadOnlyList<ReconciledGearset> Sort(IReadOnlyList<uint>? jobOrder)
    {
        // Deliberately not already in the wanted order, so a sort that did nothing would fail.
        var gearsets = new List<ReconciledGearset>
        {
            Set(1, WhiteMage),
            Set(2, Paladin),
            Set(3, Gunbreaker),
            Set(4, DarkKnight),
        };

        return FilterEngine.Sort(gearsets, GearsetSortOrder.Role, Jobs, RoleOrder, jobOrder);
    }

    private static IEnumerable<uint> Jobs2(IEnumerable<ReconciledGearset> sorted) =>
        sorted.Select(g => g.Gearset.ClassJobId);

    private static ReconciledGearset Set(int slot, uint job) =>
        TestData.Entry(slot, slot, job, $"set {slot}");
}
