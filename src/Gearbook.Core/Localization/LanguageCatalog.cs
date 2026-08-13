using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Gearbook.Core.Localization;

/// <summary>
/// One language, loaded from an embedded flat key-to-string map.
/// </summary>
/// <remarks>
/// <para>
/// Flat because the completeness test compares key sets, and nesting turns that comparison into
/// a tree walk for no gain. One file per language, included by glob, so adding a language is a
/// data change rather than a code change.
/// </para>
/// <para>
/// Nothing here throws. A catalogue that fails to parse is skipped, a missing key resolves to
/// the key itself, and a broken placeholder returns the unformatted text. All three of those
/// run inside a draw callback, where an exception is not a caught error.
/// </para>
/// </remarks>
public sealed class LanguageCatalog
{
    /// <summary>
    /// The resource prefix the loader builds names from. It is the project's root namespace plus
    /// the folder path, and it is a constant here rather than a string built at three call sites
    /// because moving the folder breaks the load silently: the failure shows as an empty window,
    /// not as an error.
    /// </summary>
    private const string ResourcePrefix = "Gearbook.Core.Localization.Resources.";

    private readonly IReadOnlyDictionary<string, string> entries;

    private LanguageCatalog(string code, IReadOnlyDictionary<string, string> entries)
    {
        Code = code;
        this.entries = entries;
    }

    /// <summary>The two-letter language code this catalogue holds.</summary>
    public string Code { get; }

    /// <summary>Every key this catalogue defines.</summary>
    public IReadOnlyCollection<string> Keys => (IReadOnlyCollection<string>)entries.Keys;

    /// <summary>
    /// Loads every catalogue embedded in the assembly. A file that cannot be parsed is left out
    /// rather than taking the load down with it.
    /// </summary>
    /// <param name="onError">Called with a human-readable reason for each file that was skipped,
    /// so the failure reaches the log instead of vanishing.</param>
    public static IReadOnlyList<LanguageCatalog> LoadAll(Action<string>? onError = null)
    {
        var assembly = typeof(LanguageCatalog).Assembly;
        var catalogues = new List<LanguageCatalog>();

        foreach (var resourceName in assembly.GetManifestResourceNames()
                     .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                     .Where(n => n.EndsWith(".json", StringComparison.Ordinal))
                     .OrderBy(n => n, StringComparer.Ordinal))
        {
            var code = resourceName[ResourcePrefix.Length..^".json".Length];
            var catalogue = Load(assembly, resourceName, code, onError);
            if (catalogue is not null)
            {
                catalogues.Add(catalogue);
            }
        }

        return catalogues;
    }

    /// <summary>
    /// Builds a catalogue from a dictionary. For tests, which own the values their verdict
    /// depends on rather than reading the shipped files.
    /// </summary>
    public static LanguageCatalog FromEntries(string code, IReadOnlyDictionary<string, string> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return new LanguageCatalog(code, new Dictionary<string, string>(entries, StringComparer.Ordinal));
    }

    private static LanguageCatalog? Load(
        Assembly assembly,
        string resourceName,
        string code,
        Action<string>? onError)
    {
        try
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                onError?.Invoke($"Language catalogue {resourceName} could not be opened.");
                return null;
            }

            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
            if (parsed is null)
            {
                onError?.Invoke($"Language catalogue {resourceName} parsed to nothing.");
                return null;
            }

            return new LanguageCatalog(code, parsed);
        }
        catch (JsonException ex)
        {
            onError?.Invoke($"Language catalogue {resourceName} is not valid JSON: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// The text for a key, or null when this catalogue does not define it. Resolving further is
    /// the resolver's job, not the catalogue's.
    /// </summary>
    public string? Find(string key) => entries.TryGetValue(key, out var value) ? value : null;

    /// <summary>
    /// Substitutes arguments into a translated string, returning the unformatted text when the
    /// placeholders do not match what was passed.
    /// </summary>
    /// <remarks>
    /// A translator writing {1} where the code passes one argument must not be able to take a
    /// window down. Showing the raw text with its braces makes the mistake visible in use, which
    /// is where somebody will actually see it.
    /// </remarks>
    public static string SafeFormat(string text, params object[] arguments)
    {
        if (arguments is null || arguments.Length == 0)
        {
            return text;
        }

        try
        {
            return string.Format(CultureInfo.CurrentCulture, text, arguments);
        }
        catch (FormatException)
        {
            return text;
        }
    }
}
