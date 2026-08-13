using System.Globalization;
using System.Reflection;
using Gearbook.Core.News;
using Xunit;

namespace Gearbook.Core.Tests.News;

public class VersionComparerTests
{
    [Theory]
    [InlineData("1.0.0", "0.9.9")]
    [InlineData("0.2.0", "0.1.9")]
    [InlineData("0.1.1", "0.1.0")]
    [InlineData("1.0.0.1", "1.0.0")]
    public void A_later_version_compares_greater(string later, string earlier)
    {
        Assert.True(VersionComparer.Instance.Compare(later, earlier) > 0);
        Assert.True(VersionComparer.IsNewer(later, earlier));
    }

    [Theory]
    [InlineData("0.2.0-beta.1", "0.2.0")]
    [InlineData("0.2.0+build.7", "0.2.0")]
    [InlineData("  1.2.3  ", "1.2.3")]
    public void A_suffix_or_surrounding_space_makes_no_difference(string one, string other)
    {
        Assert.Equal(0, VersionComparer.Instance.Compare(one, other));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a version")]
    [InlineData("....")]
    public void Anything_unreadable_sorts_as_the_oldest_possible_version(string? nonsense)
    {
        // The harmless direction: it shows the notes once too often rather than never. These
        // strings arrive from a hand-written file and from a saved setting, so all of them are
        // reachable.
        Assert.True(VersionComparer.IsNewer("0.0.1", nonsense));
        Assert.False(VersionComparer.IsNewer(nonsense, "0.0.1"));
    }

    [Fact]
    public void Comparing_never_throws_whatever_it_is_given()
    {
        string?[] awkward = [null, "", "1", "1.", ".1", "9999999999999999999", "1.a.3", "-1.0.0"];

        foreach (var left in awkward)
        {
            foreach (var right in awkward)
            {
                _ = VersionComparer.Instance.Compare(left, right);
            }
        }
    }
}

public class ReleaseNotesTests
{
    /// <summary>
    /// The version actually being built, read from the assembly rather than written down here.
    /// A hand-kept copy is wrong at the next release, and wrong invisibly.
    /// </summary>
    private static string BuiltVersion
    {
        get
        {
            var version = typeof(ReleaseNotesLoader).Assembly.GetName().Version;
            Assert.NotNull(version);
            return version.ToString();
        }
    }

    [Fact]
    public void Every_shipped_language_has_notes_that_parse_and_have_content()
    {
        var codes = ReleaseNotesLoader.AvailableCodes();

        Assert.Contains("en", codes);
        Assert.Contains("de", codes);

        foreach (var code in codes)
        {
            var errors = new List<string>();
            var document = ReleaseNotesLoader.Load(code, errors.Add);

            Assert.Empty(errors);
            Assert.NotEmpty(document.Versions);
            Assert.All(document.Versions, v => Assert.True(v.HasContent, $"{code} {v.Version} is empty."));
        }
    }

    [Fact]
    public void All_languages_describe_exactly_the_same_set_of_versions()
    {
        // A version written into one file and forgotten in another cannot ship.
        var codes = ReleaseNotesLoader.AvailableCodes();
        var reference = ReleaseNotesLoader.Load(codes[0])
            .Versions.Select(v => v.Version)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        foreach (var code in codes.Skip(1))
        {
            var other = ReleaseNotesLoader.Load(code)
                .Versions.Select(v => v.Version)
                .OrderBy(v => v, StringComparer.Ordinal);

            Assert.Equal(reference, other);
        }
    }

    [Fact]
    public void The_newest_notes_name_the_version_being_built()
    {
        // A version bump without notes fails the build rather than shipping silently.
        foreach (var code in ReleaseNotesLoader.AvailableCodes())
        {
            var ordered = ReleaseNotesLoader.Load(code).Newest();

            Assert.NotEmpty(ordered);
            var newest = ordered[0];
            Assert.Equal(0, VersionComparer.Instance.Compare(newest.Version, BuiltVersion));
        }
    }

    [Fact]
    public void The_newest_changelog_section_names_the_version_being_built()
    {
        var changelog = FindRepositoryFile("CHANGELOG.md");
        var lines = File.ReadAllLines(changelog);

        var firstRelease = lines
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("## [", StringComparison.Ordinal))
            .Where(l => !l.StartsWith("## [Unreleased]", StringComparison.OrdinalIgnoreCase))
            .Select(l => l["## [".Length..])
            .Select(l => l[..l.IndexOf(']', StringComparison.Ordinal)])
            .FirstOrDefault();

        Assert.NotNull(firstRelease);
        Assert.Equal(0, VersionComparer.Instance.Compare(firstRelease, BuiltVersion));
    }

    [Fact]
    public void The_changelog_and_the_release_notes_agree_on_which_versions_exist()
    {
        // Neither document is generated from the other, because they are written for different
        // readers. What is automated is only the check that they do not drift apart.
        var changelog = FindRepositoryFile("CHANGELOG.md");

        var changelogVersions = File.ReadAllLines(changelog)
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("## [", StringComparison.Ordinal))
            .Where(l => !l.StartsWith("## [Unreleased]", StringComparison.OrdinalIgnoreCase))
            .Select(l => l["## [".Length..])
            .Select(l => l[..l.IndexOf(']', StringComparison.Ordinal)])
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        var noteVersions = ReleaseNotesLoader.Load("en")
            .Versions.Select(v => v.Version)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(changelogVersions, noteVersions);
    }

    [Fact]
    public void Versions_are_ordered_newest_first_by_the_code_not_by_the_file()
    {
        var document = new ReleaseNotesDocument
        {
            Versions =
            [
                new() { Version = "0.1.0", Summary = "first" },
                new() { Version = "0.3.0", Summary = "third" },
                new() { Version = "0.2.0", Summary = "second" },
            ],
        };

        // Notes are hand-written, and appending a new version to the bottom is the obvious way
        // to get the order wrong.
        Assert.Equal(["0.3.0", "0.2.0", "0.1.0"], document.Newest().Select(v => v.Version));
    }

    [Fact]
    public void A_language_with_no_notes_falls_back_to_english_rather_than_to_nothing()
    {
        var document = ReleaseNotesLoader.Load("fr");

        Assert.NotEmpty(document.Versions);
    }

    [Fact]
    public void A_first_installation_never_opens_the_notes_by_itself()
    {
        // Leaving it unmarked would make the very next start look like an update and the notes
        // would appear then, which is worse than either alternative because it looks random.
        Assert.False(ReleaseNotesLoader.ShouldOpenAutomatically(
            isFirstInstallation: true,
            lastSeenVersion: null,
            currentVersion: "0.1.0",
            enabled: true));
    }

    [Fact]
    public void An_update_opens_the_notes_once()
    {
        Assert.True(ReleaseNotesLoader.ShouldOpenAutomatically(false, "0.1.0", "0.2.0", enabled: true));
        Assert.False(ReleaseNotesLoader.ShouldOpenAutomatically(false, "0.2.0", "0.2.0", enabled: true));
    }

    [Fact]
    public void Turning_the_automatic_opening_off_still_leaves_the_unread_marker()
    {
        // After the one automatic appearance the marker is the only route back into the notes,
        // and a control that never signals it has anything is one nobody presses.
        Assert.False(ReleaseNotesLoader.ShouldOpenAutomatically(false, "0.1.0", "0.2.0", enabled: false));
        Assert.True(ReleaseNotesLoader.HasUnread("0.1.0", "0.2.0"));
    }

    [Fact]
    public void The_unread_marker_and_the_automatic_opening_read_the_same_comparison()
    {
        // Two sources for one fact drift apart, and the one nobody is looking at goes wrong.
        Assert.Equal(
            ReleaseNotesLoader.HasUnread("0.1.0", "0.2.0"),
            ReleaseNotesLoader.ShouldOpenAutomatically(false, "0.1.0", "0.2.0", enabled: true));
    }

    private static string FindRepositoryFile(string fileName)
    {
        var directory = new DirectoryInfo(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            string.Format(
                CultureInfo.InvariantCulture,
                "{0} was not found above the test assembly. This test reads it from the repository.",
                fileName));
    }
}
