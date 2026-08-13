using Gearbook.Core.Model;
using Xunit;

namespace Gearbook.Core.Tests.Model;

public class JobClassifierTests
{
    private const sbyte NotAHandOrLandJob = -1;

    [Theory]
    [InlineData(1, JobRole.Tank)]
    [InlineData(2, JobRole.MeleeDps)]
    [InlineData(4, JobRole.Healer)]
    public void The_table_role_decides_the_combat_role(byte role, JobRole expected)
    {
        var (actual, category) = JobClassifier.Classify(role, NotAHandOrLandJob, primaryStat: 1);

        Assert.Equal(expected, actual);
        Assert.Equal(JobCategory.Combat, category);
    }

    [Fact]
    public void A_ranged_job_scaling_from_dexterity_is_physical()
    {
        var (role, _) = JobClassifier.Classify(3, NotAHandOrLandJob, primaryStat: 2);

        Assert.Equal(JobRole.PhysicalRangedDps, role);
    }

    [Fact]
    public void A_ranged_job_scaling_from_intelligence_is_magical()
    {
        var (role, _) = JobClassifier.Classify(3, NotAHandOrLandJob, primaryStat: 4);

        Assert.Equal(JobRole.MagicalRangedDps, role);
    }

    [Fact]
    public void A_ranged_job_whose_split_cannot_be_established_stays_combat_and_loses_only_the_role()
    {
        // Visible and reportable rather than quietly wrong. Guessing the other way would put a
        // black mage under physical ranged and nothing would ever say so.
        var (role, category) = JobClassifier.Classify(3, NotAHandOrLandJob, primaryStat: 99);

        Assert.Equal(JobRole.Unknown, role);
        Assert.Equal(JobCategory.Combat, category);
    }

    [Fact]
    public void A_hand_job_is_a_crafter_whatever_its_table_role_says()
    {
        // The table gives crafters role 0, the same value it gives the starting classes, so the
        // hand and land index is what separates them rather than the role.
        var (role, category) = JobClassifier.Classify(0, dohDolJobIndex: 3, primaryStat: 0);

        Assert.Equal(JobRole.Crafter, role);
        Assert.Equal(JobCategory.Crafting, category);
    }

    [Fact]
    public void A_land_job_is_a_gatherer()
    {
        var (role, category) = JobClassifier.Classify(0, dohDolJobIndex: 9, primaryStat: 0);

        Assert.Equal(JobRole.Gatherer, role);
        Assert.Equal(JobCategory.Gathering, category);
    }

    [Fact]
    public void A_starting_class_is_still_a_combat_gearset_with_no_role_of_its_own()
    {
        var (role, category) = JobClassifier.Classify(0, NotAHandOrLandJob, primaryStat: 1);

        Assert.Equal(JobRole.Unknown, role);
        Assert.Equal(JobCategory.Combat, category);
    }

    [Fact]
    public void A_role_from_a_future_patch_is_not_an_error_and_does_not_hide_the_gearset()
    {
        var (role, category) = JobClassifier.Classify(99, NotAHandOrLandJob, primaryStat: 1);

        Assert.Equal(JobRole.Unknown, role);
        Assert.Equal(JobCategory.Unknown, category);
    }
}
