using Gearbook.Core.Model;
using Xunit;

namespace Gearbook.Core.Tests.Model;

/// <summary>
/// The values in these tests are the real rows of the job table, read off an installed client
/// rather than invented, which is the difference between this suite and the one it replaced.
/// The one it replaced passed while filing every gatherer as a crafter.
/// </summary>
public class JobClassifierTests
{
    private const uint DiscipleOfWar = 30;
    private const uint DiscipleOfMagic = 31;
    private const uint DiscipleOfTheLand = 32;
    private const uint DiscipleOfTheHand = 33;

    [Theory]
    [InlineData(1, DiscipleOfWar, 1, JobRole.Tank)]           // paladin, dark knight
    [InlineData(2, DiscipleOfWar, 1, JobRole.MeleeDps)]       // dragoon
    [InlineData(2, DiscipleOfWar, 2, JobRole.MeleeDps)]       // ninja, dexterity but melee
    [InlineData(4, DiscipleOfMagic, 5, JobRole.Healer)]       // white mage
    [InlineData(4, DiscipleOfWar, 5, JobRole.Healer)]         // sage sits under war
    public void A_fighting_job_takes_its_role_from_the_table(
        byte role,
        uint category,
        byte primaryStat,
        JobRole expected)
    {
        var (actual, actualCategory) = JobClassifier.Classify(role, category, primaryStat);

        Assert.Equal(expected, actual);
        Assert.Equal(JobCategory.Combat, actualCategory);
    }

    [Theory]
    [InlineData(DiscipleOfWar)]     // archer, bard, machinist, dancer
    [InlineData(DiscipleOfMagic)]
    public void A_ranged_job_scaling_from_dexterity_is_physical(uint category)
    {
        var (role, _) = JobClassifier.Classify(3, category, primaryStat: 2);

        Assert.Equal(JobRole.PhysicalRangedDps, role);
    }

    [Theory]
    [InlineData(DiscipleOfMagic)]   // thaumaturge, black mage
    [InlineData(DiscipleOfWar)]     // summoner, red mage, pictomancer sit under war
    public void A_ranged_job_scaling_from_intelligence_is_magical(uint category)
    {
        var (role, _) = JobClassifier.Classify(3, category, primaryStat: 4);

        Assert.Equal(JobRole.MagicalRangedDps, role);
    }

    [Fact]
    public void A_ranged_job_whose_split_cannot_be_established_stays_combat_and_loses_only_the_role()
    {
        // Visible and reportable rather than quietly wrong. Guessing the other way would put a
        // black mage under physical ranged and nothing would ever say so.
        var (role, category) = JobClassifier.Classify(3, DiscipleOfWar, primaryStat: 99);

        Assert.Equal(JobRole.Unknown, role);
        Assert.Equal(JobCategory.Combat, category);
    }

    [Theory]
    [InlineData(0)]     // carpenter, the first hand job
    [InlineData(7)]     // culinarian, the last one
    public void Every_hand_job_is_a_crafter_whatever_its_position_in_the_sequence(byte unusedStat)
    {
        var (role, category) = JobClassifier.Classify(0, DiscipleOfTheHand, unusedStat);

        Assert.Equal(JobRole.Crafter, role);
        Assert.Equal(JobCategory.Crafting, category);
    }

    [Fact]
    public void Every_land_job_is_a_gatherer()
    {
        // The defect this test exists for: miner, botanist and fisher restart the hand-and-land
        // sequence at zero, so a blacksmith and a botanist both sit at index one. Splitting on
        // that index filed all three gatherers as crafters, and only a player noticed.
        var (role, category) = JobClassifier.Classify(0, DiscipleOfTheLand, primaryStat: 0);

        Assert.Equal(JobRole.Gatherer, role);
        Assert.Equal(JobCategory.Gathering, category);
    }

    [Fact]
    public void A_gatherer_and_a_crafter_at_the_same_sequence_position_are_still_told_apart()
    {
        var blacksmith = JobClassifier.Classify(0, DiscipleOfTheHand, primaryStat: 0);
        var botanist = JobClassifier.Classify(0, DiscipleOfTheLand, primaryStat: 0);

        Assert.NotEqual(blacksmith, botanist);
    }

    [Fact]
    public void A_starting_class_is_still_a_combat_gearset_with_no_role_of_its_own()
    {
        var (role, category) = JobClassifier.Classify(0, DiscipleOfWar, primaryStat: 1);

        Assert.Equal(JobRole.Unknown, role);
        Assert.Equal(JobCategory.Combat, category);
    }

    [Fact]
    public void A_category_from_a_future_patch_keeps_the_role_and_loses_only_the_grouping()
    {
        var (role, category) = JobClassifier.Classify(1, classJobCategoryRowId: 999, primaryStat: 1);

        Assert.Equal(JobRole.Tank, role);
        Assert.Equal(JobCategory.Unknown, category);
    }

    [Fact]
    public void A_role_from_a_future_patch_is_not_an_error_and_does_not_hide_the_gearset()
    {
        var (role, category) = JobClassifier.Classify(99, DiscipleOfWar, primaryStat: 1);

        Assert.Equal(JobRole.Unknown, role);
        Assert.Equal(JobCategory.Combat, category);
    }
}
