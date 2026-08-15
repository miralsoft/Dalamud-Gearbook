using System.Reflection;
using Gearbook.Core.Localization;
using Xunit;

namespace Gearbook.Core.Tests.Localization;

/// <summary>
/// The completeness half of this is what makes a missing translation a failing build instead of
/// a gap somebody eventually notices in a screenshot.
/// </summary>
public class LanguageCatalogueCompletenessTests
{
    private static IReadOnlyList<LanguageCatalog> Load()
    {
        var errors = new List<string>();
        var catalogues = LanguageCatalog.LoadAll(errors.Add);

        Assert.Empty(errors);
        return catalogues;
    }

    [Fact]
    public void Both_shipped_languages_are_present_and_parse()
    {
        var codes = Load().Select(c => c.Code).ToList();

        Assert.Contains("en", codes);
        Assert.Contains("de", codes);
    }

    [Fact]
    public void Every_declared_key_exists_in_every_shipped_language()
    {
        foreach (var catalogue in Load())
        {
            var missing = LocKeys.All.Except(catalogue.Keys, StringComparer.Ordinal).ToList();

            Assert.True(
                missing.Count == 0,
                $"Language '{catalogue.Code}' is missing: {string.Join(", ", missing)}");
        }
    }

    [Fact]
    public void No_catalogue_carries_a_key_that_is_no_longer_declared()
    {
        foreach (var catalogue in Load())
        {
            var extra = catalogue.Keys.Except(LocKeys.All, StringComparer.Ordinal).ToList();

            Assert.True(
                extra.Count == 0,
                $"Language '{catalogue.Code}' declares keys nothing uses: {string.Join(", ", extra)}");
        }
    }

    [Fact]
    public void Every_language_describes_the_same_set_of_keys()
    {
        var catalogues = Load();
        var reference = catalogues[0];

        foreach (var other in catalogues.Skip(1))
        {
            Assert.Equal(
                reference.Keys.OrderBy(k => k, StringComparer.Ordinal),
                other.Keys.OrderBy(k => k, StringComparer.Ordinal));
        }
    }

    [Fact]
    public void No_translated_string_is_empty()
    {
        foreach (var catalogue in Load())
        {
            foreach (var key in catalogue.Keys)
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(catalogue.Find(key)),
                    $"Language '{catalogue.Code}' has an empty string for '{key}'.");
            }
        }
    }
}

/// <summary>
/// Keeps the declared keys and the interface honest with each other.
/// </summary>
/// <remarks>
/// The completeness tests above compare the catalogues against the declared keys, which catches
/// a translation that was never written. They cannot catch the opposite: a key declared and
/// translated into both languages for a control that was never built. That gap is not
/// hypothetical. It reached twenty-five keys here before anybody counted, which is twenty-five
/// pieces of interface that were described, translated, and then not drawn.
/// </remarks>
public class LocalizationKeyUsageTests
{
    private const string LocKeysFileName = "LocKeys.cs";

    [Fact]
    public void Every_declared_key_is_used_somewhere_in_the_product()
    {
        var source = ReadAllProductSource();

        var unused = typeof(LocKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false })
            .Select(f => f.Name)
            .Where(name => !source.Contains($"LocKeys.{name}", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unused.Count == 0,
            "These keys are declared and translated but nothing uses them, which means the "
            + "control they were written for does not exist. Build it or remove the key: "
            + string.Join(", ", unused));
    }

    [Fact]
    public void The_usage_detector_can_tell_a_used_key_from_an_unused_one()
    {
        // A guard whose only observable outcome is silence cannot be told apart from one that
        // does nothing, so it is tried against something it must find and something it must not
        // before its verdict above is worth anything (R-20).
        var source = ReadAllProductSource();

        Assert.Contains("LocKeys.WindowLibraryTitle", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LocKeys.ZzzNoSuchKeyZzz", source, StringComparison.Ordinal);
    }

    [Fact]
    public void The_german_catalogue_still_has_its_umlauts()
    {
        // Written after a tooling mistake rewrote this file through the wrong code page and
        // turned every umlaut into two characters. That damage is valid UTF-8, so an encoding
        // check would have passed it. This looks for the result instead of the cause.
        var german = ReadCatalogue("de.json");

        Assert.Contains("ö", german, StringComparison.Ordinal);
        Assert.DoesNotContain("Ã", german, StringComparison.Ordinal);
        Assert.DoesNotContain("Â", german, StringComparison.Ordinal);
        Assert.DoesNotContain("�", german, StringComparison.Ordinal);
    }

    private static string ReadCatalogue(string fileName)
    {
        var path = Path.Combine(
            RepositoryRoot().FullName,
            "src",
            "Gearbook.Core",
            "Localization",
            "Resources",
            fileName);

        Assert.True(File.Exists(path), $"{path} was not found.");
        return File.ReadAllText(path);
    }

    private static string ReadAllProductSource()
    {
        var root = RepositoryRoot();
        var sources = new List<string>();

        foreach (var project in new[] { "Gearbook", "Gearbook.Core" })
        {
            var directory = Path.Combine(root.FullName, "src", project);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                // Skip the build output, which holds generated copies, and the declaration file
                // itself, where every key trivially appears.
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || Path.GetFileName(file).Equals(LocKeysFileName, StringComparison.Ordinal))
                {
                    continue;
                }

                sources.Add(File.ReadAllText(file));
            }
        }

        Assert.NotEmpty(sources);
        return string.Join('\n', sources);
    }

    private static DirectoryInfo RepositoryRoot()
    {
        var directory = new DirectoryInfo(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Gearbook.slnx")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "The repository root was not found above the test assembly. These tests read the source.");
    }
}

public class LanguageResolverTests
{
    private static readonly string[] Available = ["de", "en"];

    [Fact]
    public void An_explicit_choice_wins_over_what_the_host_reports()
    {
        Assert.Equal("de", LanguageResolver.Resolve("de", "en", Available));
        Assert.Equal("en", LanguageResolver.Resolve("en", "de", Available));
    }

    [Fact]
    public void Automatic_follows_the_host()
    {
        Assert.Equal("de", LanguageResolver.Resolve(LanguageResolver.Automatic, "de", Available));
    }

    [Fact]
    public void A_culture_name_from_the_host_is_reduced_to_its_bare_tag()
    {
        // The host may report either shape and does not promise which. Comparing the raw value
        // would silently fall back to English on every client reporting the longer form.
        Assert.Equal("de", LanguageResolver.Resolve(LanguageResolver.Automatic, "de-DE", Available));
        Assert.Equal("de", LanguageResolver.Resolve(LanguageResolver.Automatic, "DE_de", Available));
        Assert.Equal("de", LanguageResolver.Resolve(LanguageResolver.Automatic, "  De  ", Available));
    }

    [Fact]
    public void A_host_language_with_no_catalogue_falls_back_rather_than_showing_nothing()
    {
        Assert.Equal("en", LanguageResolver.Resolve(LanguageResolver.Automatic, "fr", Available));
    }

    [Fact]
    public void An_explicit_choice_with_no_catalogue_falls_back_rather_than_showing_nothing()
    {
        Assert.Equal("en", LanguageResolver.Resolve("ja", "fr", Available));
    }

    [Fact]
    public void The_result_always_has_a_catalogue_behind_it()
    {
        string[] onlyJapanese = ["ja"];

        // Neither the choice, nor the host, nor the fallback language exists here. Returning a
        // code with no catalogue would leave the window empty rather than untranslated.
        Assert.Equal("ja", LanguageResolver.Resolve("de", "fr", onlyJapanese));
    }

    [Fact]
    public void Nothing_loaded_at_all_still_yields_the_fallback_code_rather_than_throwing()
    {
        Assert.Equal("en", LanguageResolver.Resolve("de", "de", []));
    }
}

public class LocalizerTests
{
    private static Localizer Build()
    {
        var english = LanguageCatalog.FromEntries("en", new Dictionary<string, string>
        {
            ["a.key"] = "English A",
            ["b.key"] = "English B",
            ["fmt.key"] = "Value is {0}",
            ["bad.fmt"] = "Value is {1}",
        });

        var german = LanguageCatalog.FromEntries("de", new Dictionary<string, string>
        {
            ["a.key"] = "Deutsch A",
        });

        return new Localizer([english, german]);
    }

    [Fact]
    public void The_active_language_answers_first()
    {
        var localizer = Build();
        localizer.SetLanguage("de", null);

        Assert.Equal("Deutsch A", localizer.Get("a.key"));
    }

    [Fact]
    public void A_key_the_active_language_lacks_comes_from_the_fallback()
    {
        var localizer = Build();
        localizer.SetLanguage("de", null);

        Assert.Equal("English B", localizer.Get("b.key"));
    }

    [Fact]
    public void An_undefined_key_returns_the_key_itself()
    {
        var localizer = Build();

        // Ugly on screen on purpose. Throwing would take the window down, and returning empty
        // would hide the gap, which means nobody reports it.
        Assert.Equal("nothing.defines.this", localizer.Get("nothing.defines.this"));
    }

    [Fact]
    public void A_placeholder_a_translator_got_wrong_yields_the_unformatted_text()
    {
        var localizer = Build();

        Assert.Equal("Value is 7", localizer.Get("fmt.key", 7));
        Assert.Equal("Value is {1}", localizer.Get("bad.fmt", 7));
    }
}
