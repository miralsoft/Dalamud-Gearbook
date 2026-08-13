namespace Gearbook.Core.Localization;

/// <summary>
/// The active language, and the one thing the interface asks for a string.
/// </summary>
/// <remarks>
/// The resolution chain is: active language, then the fallback language, then the key itself.
/// Returning the key shows an ugly string in the window, which makes the gap obvious during
/// use. Throwing would take the window down; returning empty would hide the gap entirely, and a
/// gap nobody can see is one nobody reports.
/// </remarks>
public sealed class Localizer
{
    private readonly IReadOnlyDictionary<string, LanguageCatalog> catalogues;

    /// <summary>
    /// Creates a localizer over the catalogues that loaded.
    /// </summary>
    public Localizer(IReadOnlyList<LanguageCatalog> catalogues)
    {
        ArgumentNullException.ThrowIfNull(catalogues);

        this.catalogues = catalogues.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);
        AvailableCodes = [.. this.catalogues.Keys.OrderBy(c => c, StringComparer.Ordinal)];
        ActiveCode = LanguageResolver.Resolve(null, null, AvailableCodes);
    }

    /// <summary>Which catalogues are actually present.</summary>
    public IReadOnlyList<string> AvailableCodes { get; }

    /// <summary>The language currently being spoken. Always has a catalogue behind it.</summary>
    public string ActiveCode { get; private set; }

    /// <summary>
    /// Re-resolves the active language from the player's choice and the host's setting.
    /// </summary>
    public void SetLanguage(string? explicitChoice, string? hostLanguage) =>
        ActiveCode = LanguageResolver.Resolve(explicitChoice, hostLanguage, AvailableCodes);

    /// <summary>
    /// The text for a key. Never throws and never returns null.
    /// </summary>
    public string Get(string key)
    {
        if (catalogues.TryGetValue(ActiveCode, out var active))
        {
            var text = active.Find(key);
            if (text is not null)
            {
                return text;
            }
        }

        if (catalogues.TryGetValue(LanguageResolver.Fallback, out var fallback))
        {
            var text = fallback.Find(key);
            if (text is not null)
            {
                return text;
            }
        }

        return key;
    }

    /// <summary>
    /// The text for a key with arguments substituted. A placeholder a translator got wrong
    /// yields the unformatted text rather than an exception.
    /// </summary>
    public string Get(string key, params object[] arguments) =>
        LanguageCatalog.SafeFormat(Get(key), arguments);
}
