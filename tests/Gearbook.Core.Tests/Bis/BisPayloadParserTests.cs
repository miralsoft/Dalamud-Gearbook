using Gearbook.Core.Bis;
using Xunit;

namespace Gearbook.Core.Tests.Bis;

/// <summary>
/// This parser reads a string produced by a different plugin, whose version moves independently
/// of this one, and the result is used inside a draw callback where an exception is not a caught
/// error. So the interesting cases here are all the malformed ones.
/// </summary>
public class BisPayloadParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void No_answer_means_nothing_is_shown(string? payload)
    {
        var snapshot = BisPayloadParser.Parse(payload);

        Assert.Equal(BisProviderState.Unavailable, snapshot.State);
        Assert.False(snapshot.HasAnything);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("[1, 2, 3]")]
    [InlineData("\"a string\"")]
    [InlineData("42")]
    public void An_unreadable_answer_is_reported_and_shows_nothing(string payload)
    {
        var errors = new List<string>();

        var snapshot = BisPayloadParser.Parse(payload, errors.Add);

        Assert.Equal(BisProviderState.Unavailable, snapshot.State);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void A_well_formed_answer_is_read()
    {
        const string payload = """
            {
              "state": "ok",
              "entries": [
                { "id": 4, "matched": 14, "total": 16, "target": "Ultimate BiS" },
                { "id": 7, "matched": 16, "total": 16, "target": "Savage BiS" }
              ]
            }
            """;

        var snapshot = BisPayloadParser.Parse(payload);

        Assert.Equal(BisProviderState.Ok, snapshot.State);
        Assert.True(snapshot.HasAnything);

        var entry = snapshot.For(4);
        Assert.NotNull(entry);
        Assert.Equal(14, entry.Matched);
        Assert.Equal(16, entry.Total);
        Assert.Equal("Ultimate BiS", entry.Target);

        Assert.Null(snapshot.For(99));
    }

    [Theory]
    [InlineData("noaccount", BisProviderState.NoAccount)]
    [InlineData("NoAccount", BisProviderState.NoAccount)]
    [InlineData("loading", BisProviderState.Loading)]
    [InlineData("nodata", BisProviderState.NoData)]
    public void The_state_field_is_read_without_regard_to_case(string state, BisProviderState expected)
    {
        var snapshot = BisPayloadParser.Parse($$"""{ "state": "{{state}}", "entries": [] }""");

        Assert.Equal(expected, snapshot.State);
    }

    [Fact]
    public void A_state_this_build_has_never_heard_of_is_treated_as_working()
    {
        // Not an error and not a reason to discard entries. The other side is allowed to grow
        // its vocabulary without breaking an older build of this one.
        var snapshot = BisPayloadParser.Parse(
            """{ "state": "somethingNew", "entries": [ { "id": 1, "matched": 1, "total": 2 } ] }""");

        Assert.Equal(BisProviderState.Ok, snapshot.State);
        Assert.NotNull(snapshot.For(1));
    }

    [Fact]
    public void An_answer_with_no_state_field_at_all_is_treated_as_working()
    {
        var snapshot = BisPayloadParser.Parse(
            """{ "entries": [ { "id": 1, "matched": 1, "total": 2 } ] }""");

        Assert.Equal(BisProviderState.Ok, snapshot.State);
    }

    [Fact]
    public void Unknown_fields_are_ignored_rather_than_rejected()
    {
        // This is what lets the other side add fields later without breaking older builds, and
        // it is the reason the contract is a JSON string rather than a shared type.
        var snapshot = BisPayloadParser.Parse("""
            {
              "state": "ok",
              "somethingNew": { "nested": true },
              "entries": [ { "id": 2, "matched": 3, "total": 4, "colour": "red" } ]
            }
            """);

        Assert.Equal(BisProviderState.Ok, snapshot.State);
        Assert.Equal(3, snapshot.For(2)!.Matched);
    }

    [Theory]
    [InlineData("""{ "id": 1, "matched": 3, "total": 0 }""")]
    [InlineData("""{ "id": 1, "matched": -1, "total": 4 }""")]
    [InlineData("""{ "id": 1, "matched": 9, "total": 4 }""")]
    [InlineData("""{ "id": 1, "total": 4 }""")]
    [InlineData("""{ "matched": 1, "total": 4 }""")]
    [InlineData("""{ "id": "one", "matched": 1, "total": 4 }""")]
    [InlineData("\"not an object\"")]
    public void An_entry_that_cannot_be_true_is_dropped_rather_than_displayed(string entry)
    {
        // A total of zero would render as "3 of 0", and a count higher than the total means the
        // other side is confused about something. Either is worse on screen than no badge.
        var snapshot = BisPayloadParser.Parse($$"""{ "state": "ok", "entries": [ {{entry}} ] }""");

        Assert.False(snapshot.HasAnything);
    }

    [Fact]
    public void One_bad_entry_does_not_discard_the_good_ones()
    {
        var snapshot = BisPayloadParser.Parse("""
            {
              "state": "ok",
              "entries": [
                { "id": 1, "matched": 3, "total": 0 },
                { "id": 2, "matched": 3, "total": 4 }
              ]
            }
            """);

        Assert.Null(snapshot.For(1));
        Assert.NotNull(snapshot.For(2));
    }

    [Fact]
    public void A_repeated_gearset_keeps_the_first_answer_rather_than_the_last()
    {
        var snapshot = BisPayloadParser.Parse("""
            {
              "state": "ok",
              "entries": [
                { "id": 1, "matched": 3, "total": 4, "target": "First" },
                { "id": 1, "matched": 1, "total": 4, "target": "Second" }
              ]
            }
            """);

        Assert.Equal("First", snapshot.For(1)!.Target);
    }

    [Fact]
    public void A_missing_target_name_is_an_empty_string_rather_than_a_failure()
    {
        var snapshot = BisPayloadParser.Parse(
            """{ "state": "ok", "entries": [ { "id": 1, "matched": 1, "total": 2 } ] }""");

        Assert.Equal(string.Empty, snapshot.For(1)!.Target);
    }

    [Fact]
    public void Parsing_never_throws_whatever_it_is_given()
    {
        string?[] awkward =
        [
            null, "", "{}", "[]", "null", "{\"entries\": null}", "{\"entries\": {}}",
            "{\"state\": 7}", "{\"entries\": [null]}", "{\"entries\": [[]]}",
        ];

        foreach (var payload in awkward)
        {
            _ = BisPayloadParser.Parse(payload);
        }
    }
}
