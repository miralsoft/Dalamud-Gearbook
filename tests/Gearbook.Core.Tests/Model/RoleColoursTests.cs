using System.Numerics;
using Gearbook.Core.Model;
using Xunit;

namespace Gearbook.Core.Tests.Model;

/// <summary>
/// The colours that say which role a gearset belongs to.
/// </summary>
/// <remarks>
/// These tests cannot say whether a colour looks right on a bar; only a person looking at one
/// can, and that is how the first two attempts were found wanting. What they hold is the property
/// the second attempt turns on: that turning a colour up makes it brighter <em>and</em> more
/// saturated, where the first attempt drained it while brightening and so said less the harder it
/// tried.
/// </remarks>
public sealed class RoleColoursTests
{
    [Theory]
    [InlineData(JobRole.Tank)]
    [InlineData(JobRole.Healer)]
    [InlineData(JobRole.MeleeDps)]
    [InlineData(JobRole.PhysicalRangedDps)]
    [InlineData(JobRole.MagicalRangedDps)]
    [InlineData(JobRole.Crafter)]
    [InlineData(JobRole.Gatherer)]
    public void EveryRealRoleHasAColour(JobRole role) =>
        Assert.NotNull(RoleColours.For(role));

    [Fact]
    public void AnUndescribedJobHasNoneRatherThanAGrey()
    {
        // So the caller can leave the host's own colour alone instead of painting something that
        // looks chosen over a job the job table does not describe.
        Assert.Null(RoleColours.For(JobRole.Unknown));
    }

    [Fact]
    public void TheThreeDamageRolesShareOneColour()
    {
        // The game's own division: one colour for the group, three symbols to tell them apart.
        // Inventing two more colours would disagree with every window the player already knows.
        var melee = RoleColours.For(JobRole.MeleeDps);

        Assert.Equal(melee, RoleColours.For(JobRole.PhysicalRangedDps));
        Assert.Equal(melee, RoleColours.For(JobRole.MagicalRangedDps));
    }

    [Fact]
    public void TankHealerAndDamageAreToldApart()
    {
        var tank = RoleColours.For(JobRole.Tank);
        var healer = RoleColours.For(JobRole.Healer);
        var damage = RoleColours.For(JobRole.MeleeDps);
        var hand = RoleColours.For(JobRole.Crafter);
        var land = RoleColours.For(JobRole.Gatherer);

        Assert.Equal(5, new[] { tank, healer, damage, hand, land }.Distinct().Count());
    }

    [Theory]
    [InlineData(JobRole.Tank)]
    [InlineData(JobRole.Healer)]
    [InlineData(JobRole.MeleeDps)]
    [InlineData(JobRole.Crafter)]
    [InlineData(JobRole.Gatherer)]
    public void TurningAColourUpReachesFullBrightnessOnItsStrongestChannel(JobRole role)
    {
        var vivid = RoleColours.Vivid(RoleColours.For(role)!.Value);
        var peak = Math.Max(vivid.X, Math.Max(vivid.Y, vivid.Z));

        Assert.Equal(1f, peak, 3);
    }

    [Theory]
    [InlineData(JobRole.Tank)]
    [InlineData(JobRole.Healer)]
    [InlineData(JobRole.MeleeDps)]
    [InlineData(JobRole.Crafter)]
    public void TurningAColourUpAlsoWidensTheGapBetweenItsChannels(JobRole role)
    {
        // The failure this exists for. Mixing towards white brightens and drains at once, so the
        // brighter it got the less it meant, and on a bar it read as a smudge.
        var plain = RoleColours.For(role)!.Value;
        var vivid = RoleColours.Vivid(plain);

        Assert.True(Spread(vivid) > Spread(plain));
    }

    [Theory]
    [InlineData(JobRole.Tank)]
    [InlineData(JobRole.Healer)]
    [InlineData(JobRole.MeleeDps)]
    [InlineData(JobRole.Crafter)]
    [InlineData(JobRole.Gatherer)]
    public void TurningAColourUpLeavesItTheSameHue(JobRole role)
    {
        // Which channel leads decides what colour a person sees. Brightness may change; the
        // order must not, or blue arrives as something else.
        var plain = RoleColours.For(role)!.Value;
        var vivid = RoleColours.Vivid(plain);

        Assert.Equal(Ranking(plain), Ranking(vivid));
    }

    [Fact]
    public void TurningUpBlackIsLeftAloneRatherThanDividedByZero()
    {
        var black = new Vector4(0f, 0f, 0f, 1f);

        Assert.Equal(black, RoleColours.Vivid(black));
    }

    [Fact]
    public void ChangingTheOpacityLeavesTheColourItself()
    {
        var colour = new Vector4(0.2f, 0.4f, 0.6f, 1f);
        var faded = RoleColours.WithAlpha(colour, 0.25f);

        Assert.Equal(colour.X, faded.X);
        Assert.Equal(colour.Y, faded.Y);
        Assert.Equal(colour.Z, faded.Z);
        Assert.Equal(0.25f, faded.W);
    }

    private static float Spread(Vector4 colour) =>
        Math.Max(colour.X, Math.Max(colour.Y, colour.Z))
        - Math.Min(colour.X, Math.Min(colour.Y, colour.Z));

    /// <summary>Which channel leads and which trails, which is what a person reads as the hue.</summary>
    private static string Ranking(Vector4 colour)
    {
        var channels = new[] { ('r', colour.X), ('g', colour.Y), ('b', colour.Z) };
        return new string([.. channels.OrderByDescending(c => c.Item2).Select(c => c.Item1)]);
    }
}
