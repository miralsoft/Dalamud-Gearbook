using Gearbook.Core.Model;
using Xunit;

namespace Gearbook.Core.Tests.Model;

/// <summary>
/// The game's role symbols.
/// </summary>
/// <remarks>
/// These tests cannot tell whether a number draws the right picture; only the client can, and
/// that is how the numbers were established in the first place. What they can do is keep the
/// table from drifting, which is the way a mapping like this actually breaks: somebody adds a
/// role, copies the line above and leaves the number on it.
/// </remarks>
public sealed class RoleIconsTests
{
    [Theory]
    [InlineData(JobRole.Tank)]
    [InlineData(JobRole.Healer)]
    [InlineData(JobRole.MeleeDps)]
    [InlineData(JobRole.PhysicalRangedDps)]
    [InlineData(JobRole.MagicalRangedDps)]
    [InlineData(JobRole.Crafter)]
    [InlineData(JobRole.Gatherer)]
    public void EveryRealRoleHasASymbol(JobRole role) =>
        Assert.NotEqual(RoleIcons.None, RoleIcons.For(role));

    [Fact]
    public void AnUndescribedRoleHasNone() =>
        Assert.Equal(RoleIcons.None, RoleIcons.For(JobRole.Unknown));

    [Fact]
    public void AnUndescribedCategoryHasNone() =>
        Assert.Equal(RoleIcons.None, RoleIcons.For(JobCategory.Unknown));

    /// <summary>
    /// Two roles sharing a symbol would show the same picture for two different views, which is
    /// the one failure a copied line produces and the one this file exists to catch.
    /// </summary>
    [Fact]
    public void NoTwoRolesShareASymbol()
    {
        var symbols = Enum.GetValues<JobRole>()
            .Where(r => r != JobRole.Unknown)
            .Select(RoleIcons.For)
            .ToList();

        Assert.Equal(symbols.Count, symbols.Distinct().Count());
    }

    /// <summary>
    /// Crafting and gathering are the same gearsets whether they are reached as a role or as a
    /// category, so they share a picture on purpose. Written down because it looks like the
    /// duplicate the test above forbids, and the next reader deserves to know which it is.
    /// </summary>
    [Theory]
    [InlineData(JobRole.Crafter, JobCategory.Crafting)]
    [InlineData(JobRole.Gatherer, JobCategory.Gathering)]
    public void ACategoryAgreesWithTheRoleItRepeats(JobRole role, JobCategory category) =>
        Assert.Equal(RoleIcons.For(role), RoleIcons.For(category));

    /// <summary>
    /// The two the game never made are built from a ground and a tool, and the ground has to be
    /// one no role uses. On blue, green or red they would read as a fourth and fifth role rather
    /// than as the level above one.
    /// </summary>
    [Theory]
    [InlineData(JobCategory.Crafting)]
    [InlineData(JobCategory.Gathering)]
    public void ABuiltCategorySymbolStandsOnAGroundNoRoleUses(JobCategory category)
    {
        var built = RoleIcons.For(category);

        Assert.True(built.IsLayered);
        Assert.DoesNotContain(
            built.Base,
            Enum.GetValues<JobRole>().Select(r => RoleIcons.For(r).Base).Where(b => b != built.Base));
    }

    [Fact]
    public void TheTwoBuiltSymbolsAreToldApartByBothHalves()
    {
        var crafting = RoleIcons.For(JobCategory.Crafting);
        var gathering = RoleIcons.For(JobCategory.Gathering);

        Assert.NotEqual(crafting.Base, gathering.Base);
        Assert.NotEqual(crafting.Overlay, gathering.Overlay);
    }

    /// <summary>
    /// Combat is not one of the roles. It covers tanks, healers and damage alike, so it must not
    /// borrow any single role's picture.
    /// </summary>
    [Fact]
    public void CombatBorrowsNoRoleSymbol()
    {
        var combat = RoleIcons.For(JobCategory.Combat);

        Assert.NotEqual(RoleIcons.None, combat);
        Assert.DoesNotContain(
            combat,
            Enum.GetValues<JobRole>().Select(RoleIcons.For));
    }
}
