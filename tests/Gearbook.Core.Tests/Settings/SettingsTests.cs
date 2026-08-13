using Gearbook.Core.Settings;
using Xunit;

namespace Gearbook.Core.Tests.Settings;

public class SettingsMigratorTests
{
    [Fact]
    public void A_current_configuration_needs_no_migration_and_says_nothing()
    {
        var log = new List<string>();
        var settings = new GearbookSettings { LayoutVersion = SettingsMigrator.CurrentVersion };

        Assert.True(SettingsMigrator.Migrate(settings, log.Add));
        Assert.Empty(log);
    }

    [Fact]
    public void A_file_from_before_versioning_is_brought_forward_and_the_step_is_logged()
    {
        var log = new List<string>();
        var settings = new GearbookSettings { LayoutVersion = 0 };

        Assert.True(SettingsMigrator.Migrate(settings, log.Add));
        Assert.Equal(SettingsMigrator.CurrentVersion, settings.LayoutVersion);

        // A migration that ran silently cannot be told apart from one that did not, and the
        // difference matters when somebody is looking at a value they cannot explain.
        Assert.NotEmpty(log);
    }

    [Fact]
    public void A_file_from_a_newer_build_is_left_exactly_as_it_is()
    {
        var log = new List<string>();
        var settings = new GearbookSettings { LayoutVersion = SettingsMigrator.CurrentVersion + 5 };

        // Downgrading would mean guessing what a value this build has never heard of used to
        // mean. Failing closed keeps the file intact for the build that understands it.
        Assert.False(SettingsMigrator.Migrate(settings, log.Add));
        Assert.Equal(SettingsMigrator.CurrentVersion + 5, settings.LayoutVersion);
        Assert.NotEmpty(log);
    }

    [Fact]
    public void A_default_nobody_chose_is_corrected_on_upgrade()
    {
        Assert.Equal(6, SettingsMigrator.CorrectUnchosenDefault(stored: 4, oldDefault: 4, newDefault: 6));
    }

    [Fact]
    public void A_value_somebody_chose_is_left_alone()
    {
        Assert.Equal(9, SettingsMigrator.CorrectUnchosenDefault(stored: 9, oldDefault: 4, newDefault: 6));
    }
}

public class GearbookSettingsTests
{
    private const ulong SomeCharacter = 1234567890123456;
    private const ulong AnotherCharacter = 9876543210987654;

    [Fact]
    public void A_character_is_not_known_until_something_is_stored_for_it()
    {
        var settings = new GearbookSettings();

        // This is what tells a first installation apart from an update. The last-seen version
        // cannot do it: somebody updating from a build that predates that setting has also seen
        // nothing, so the two look identical from there.
        Assert.False(settings.Knows(SomeCharacter));

        settings.For(SomeCharacter);

        Assert.True(settings.Knows(SomeCharacter));
    }

    [Fact]
    public void Two_characters_keep_separate_settings()
    {
        var settings = new GearbookSettings();

        settings.For(SomeCharacter).Language = "de";
        settings.For(AnotherCharacter).Language = "en";

        Assert.Equal("de", settings.For(SomeCharacter).Language);
        Assert.Equal("en", settings.For(AnotherCharacter).Language);
    }

    [Fact]
    public void Asking_twice_for_the_same_character_returns_the_same_settings()
    {
        var settings = new GearbookSettings();

        Assert.Same(settings.For(SomeCharacter), settings.For(SomeCharacter));
    }
}

public class GearsetRecordDataTests
{
    [Fact]
    public void A_record_survives_a_round_trip_through_its_stored_form()
    {
        var original = TestData.Record(
            id: 7,
            job: TestData.DarkKnightId,
            name: "Ultimate",
            slot: 4,
            favourite: true,
            note: "for UWU",
            barPosition: 2,
            lastUsed: TestData.Now.AddDays(-3),
            tags: ["raid", "ultimate"]);

        var restored = GearsetRecordData.FromRecord(original).ToRecord();

        Assert.Equal(original, restored);
    }

    [Fact]
    public void A_stored_form_with_missing_pieces_still_produces_a_usable_record()
    {
        // What a hand-edited or partially written file looks like. A configuration that fails to
        // load is a failure that lands on the player's machine rather than on ours.
        var data = new GearsetRecordData { Id = 3 };

        var record = data.ToRecord();

        Assert.Equal(3, record.Id);
        Assert.Equal(string.Empty, record.Note);
        Assert.Empty(record.Tags);
        Assert.Null(record.BarPosition);
    }

    [Fact]
    public void Two_records_with_the_same_tags_in_different_lists_are_equal()
    {
        // A record's generated equality would compare the tag lists by reference, and a record
        // read back from the configuration never shares a list with the one that wrote it. So
        // anything asking "did this change" would always have answered yes.
        var one = TestData.Record(1, TestData.WhiteMageId, "A", slot: 1, tags: ["raid", "alt"]);
        var other = TestData.Record(1, TestData.WhiteMageId, "A", slot: 1, tags: ["raid", "alt"]);

        Assert.Equal(one, other);
        Assert.Equal(one.GetHashCode(), other.GetHashCode());
    }

    [Fact]
    public void Records_differing_only_in_their_tags_are_not_equal()
    {
        var one = TestData.Record(1, TestData.WhiteMageId, "A", slot: 1, tags: ["raid"]);
        var other = TestData.Record(1, TestData.WhiteMageId, "A", slot: 1, tags: ["glam"]);

        Assert.NotEqual(one, other);
    }

    [Fact]
    public void The_stored_form_shares_no_list_with_the_record_it_came_from()
    {
        var original = TestData.Record(1, TestData.WhiteMageId, "A", slot: 1, tags: ["one"]);

        var data = GearsetRecordData.FromRecord(original);
        data.Tags.Add("two");

        Assert.Equal(["one"], original.Tags);
    }
}
