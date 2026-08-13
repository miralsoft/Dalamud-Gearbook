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
