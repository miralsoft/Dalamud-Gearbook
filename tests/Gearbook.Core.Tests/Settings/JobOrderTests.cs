using Gearbook.Core.Settings;
using Xunit;

namespace Gearbook.Core.Tests.Settings;

/// <summary>
/// The order the jobs take inside their role.
/// </summary>
public sealed class JobOrderTests
{
    [Fact]
    public void AnUntouchedOrderTakesTheOrderItIsGiven()
    {
        // Which is alphabetical, supplied that way by the caller, so a player who never opens
        // the setting sees exactly what sorting by role did before it existed.
        var character = new CharacterSettings();

        character.NormaliseJobOrder([32u, 21u, 19u]);

        Assert.Equal([32u, 21u, 19u], character.JobOrder);
    }

    [Fact]
    public void AnArrangementSurvivesAndTheFallbackOnlyFillsTheRest()
    {
        var character = new CharacterSettings { JobOrder = [32u, 21u] };

        character.NormaliseJobOrder([19u, 21u, 32u, 37u]);

        // The two that were placed keep their order and their lead; the two that were not follow
        // in the order they arrived, which is where a job nobody has arranged belongs.
        Assert.Equal([32u, 21u, 19u, 37u], character.JobOrder);
    }

    [Fact]
    public void AJobWithNoGearsetsLeavesTheList()
    {
        // Not a loss: it comes back at the end the moment it has a gearset again, and a row for
        // a job the character does not have is a row whose effect nobody can see.
        var character = new CharacterSettings { JobOrder = [32u, 21u, 19u] };

        character.NormaliseJobOrder([32u, 19u]);

        Assert.Equal([32u, 19u], character.JobOrder);
    }

    [Fact]
    public void AListThatGrewOnEveryLoadIsRepairedToOneOfEach()
    {
        // The same defect the role order had: a serialiser reading into a property that already
        // holds items appends rather than replaces.
        var character = new CharacterSettings { JobOrder = [32u, 21u, 32u, 21u, 32u] };

        character.NormaliseJobOrder([21u, 32u]);

        Assert.Equal([32u, 21u], character.JobOrder);
    }

    [Fact]
    public void NoGearsetsAtAllLeavesAnEmptyList()
    {
        var character = new CharacterSettings { JobOrder = [32u, 21u] };

        character.NormaliseJobOrder([]);

        Assert.Empty(character.JobOrder);
    }
}
