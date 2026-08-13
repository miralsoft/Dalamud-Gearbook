namespace Gearbook.Core.Localization;

/// <summary>
/// Decides which language to speak.
/// </summary>
/// <remarks>
/// A pure function of three inputs, because the fallback rules are the part that goes wrong and
/// they cannot be tested at all once they are spread across the window that uses them.
/// </remarks>
public static class LanguageResolver
{
    /// <summary>
    /// The reserved code meaning "follow the host application". A value of the same type as a
    /// language code rather than a separate flag, so the setting stays one field and the
    /// resolver has one input.
    /// </summary>
    public const string Automatic = "auto";

    /// <summary>The language a missing key falls back to.</summary>
    public const string Fallback = "en";

    /// <summary>
    /// Resolves the active language code.
    /// </summary>
    /// <param name="explicitChoice">What the player chose in the plugin, or
    /// <see cref="Automatic"/> when they have not chosen.</param>
    /// <param name="hostLanguage">What the host reports as its own interface language. May
    /// arrive as a bare tag or as a culture name, and the shape is not guaranteed.</param>
    /// <param name="availableCodes">The catalogues that actually loaded.</param>
    /// <returns>A code that always has a catalogue behind it.</returns>
    public static string Resolve(
        string? explicitChoice,
        string? hostLanguage,
        IReadOnlyCollection<string> availableCodes)
    {
        ArgumentNullException.ThrowIfNull(availableCodes);

        if (availableCodes.Count == 0)
        {
            return Fallback;
        }

        // An explicit choice wins and keeps winning. Without this check, a later change to the
        // host's language would silently overwrite it, which reads as the plugin forgetting a
        // setting rather than as a rule.
        var chosen = Normalise(explicitChoice);
        if (chosen is not null && !string.Equals(chosen, Automatic, StringComparison.Ordinal))
        {
            if (Contains(availableCodes, chosen))
            {
                return Find(availableCodes, chosen);
            }
        }

        var host = Normalise(hostLanguage);
        if (host is not null && Contains(availableCodes, host))
        {
            return Find(availableCodes, host);
        }

        if (Contains(availableCodes, Fallback))
        {
            return Find(availableCodes, Fallback);
        }

        // Never return a code with no catalogue behind it. That would leave the window empty
        // rather than merely untranslated, which is a worse failure and a harder one to report.
        return availableCodes.OrderBy(c => c, StringComparer.Ordinal).First();
    }

    /// <summary>
    /// Reduces a language value to a bare lowercase tag. The host may report either a bare tag
    /// or a culture name, so a plugin that compares the raw value falls back to English on every
    /// client that reports the longer form.
    /// </summary>
    public static string? Normalise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var separator = trimmed.IndexOfAny(['-', '_']);
        if (separator > 0)
        {
            trimmed = trimmed[..separator];
        }

        return trimmed.ToLowerInvariant();
    }

    private static bool Contains(IReadOnlyCollection<string> codes, string code) =>
        codes.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));

    private static string Find(IReadOnlyCollection<string> codes, string code) =>
        codes.First(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
}
