using System.Text.Json;

namespace Gearbook.Core.News;

/// <summary>
/// Loads the embedded release notes.
/// </summary>
/// <remarks>
/// The notes live in the core rather than in the plugin project, and that is the only reason
/// the completeness tests can run without a game installation. Everything else about how they
/// are arranged follows from that choice.
/// </remarks>
public static class ReleaseNotesLoader
{
    /// <summary>
    /// The resource prefix, in one place next to the loader. The resource name is the root
    /// namespace plus the folder path plus the file name, so moving this folder breaks the load
    /// and the failure shows as an empty window rather than as an error.
    /// </summary>
    private const string ResourcePrefix = "Gearbook.Core.News.Resources.";

    /// <summary>The language codes that have a notes file.</summary>
    public static IReadOnlyList<string> AvailableCodes()
    {
        var assembly = typeof(ReleaseNotesLoader).Assembly;

        return [.. assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .Where(n => n.EndsWith(".json", StringComparison.Ordinal))
            .Select(n => n[ResourcePrefix.Length..^".json".Length])
            .OrderBy(c => c, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Loads the notes for a language.
    /// </summary>
    /// <remarks>
    /// The chain is: the requested language, then English, then nothing at all. A missing or
    /// unparseable notes file costs the player a window, never the plugin.
    /// </remarks>
    /// <param name="languageCode">The language to prefer.</param>
    /// <param name="onError">Called with a reason whenever a file was skipped.</param>
    public static ReleaseNotesDocument Load(string? languageCode, Action<string>? onError = null)
    {
        var document = LoadExact(languageCode, onError);
        if (document is not null)
        {
            return document;
        }

        if (!string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase))
        {
            document = LoadExact("en", onError);
            if (document is not null)
            {
                return document;
            }
        }

        return ReleaseNotesDocument.Empty;
    }

    private static ReleaseNotesDocument? LoadExact(string? languageCode, Action<string>? onError)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        var resourceName = ResourcePrefix + languageCode.Trim().ToLowerInvariant() + ".json";
        var assembly = typeof(ReleaseNotesLoader).Assembly;

        try
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                return null;
            }

            var parsed = JsonSerializer.Deserialize<ReleaseNotesDocument>(stream);
            if (parsed is null)
            {
                onError?.Invoke($"Release notes {resourceName} parsed to nothing.");
                return null;
            }

            return parsed;
        }
        catch (JsonException ex)
        {
            onError?.Invoke($"Release notes {resourceName} are not valid JSON: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Whether the notes should open by themselves.
    /// </summary>
    /// <param name="isFirstInstallation">Whether this run is the first the plugin has ever had
    /// on this character. Told apart by whether a configuration existed at all, not by the
    /// last-seen setting: somebody updating from a build that predates that setting has also
    /// seen nothing, so the two look identical from there.</param>
    /// <param name="lastSeenVersion">The newest version whose notes were opened.</param>
    /// <param name="currentVersion">The version now running.</param>
    /// <param name="enabled">Whether the player wants this at all.</param>
    public static bool ShouldOpenAutomatically(
        bool isFirstInstallation,
        string? lastSeenVersion,
        string? currentVersion,
        bool enabled)
    {
        // A first installation is marked as seen rather than left alone, and never opens the
        // window. Leaving it would make the very next start look like an update and the notes
        // would appear then, which is worse than either alternative because it looks random.
        if (isFirstInstallation)
        {
            return false;
        }

        return enabled && VersionComparer.IsNewer(currentVersion, lastSeenVersion);
    }

    /// <summary>
    /// Whether there is anything unread, which is what keeps the way back into the notes
    /// visible after their one automatic appearance.
    /// </summary>
    /// <remarks>
    /// Deliberately the same comparison that decides whether to open the window, rather than a
    /// second flag. Two sources for one fact drift apart, and the one nobody is looking at is
    /// the one that goes wrong.
    /// </remarks>
    public static bool HasUnread(string? lastSeenVersion, string? currentVersion) =>
        VersionComparer.IsNewer(currentVersion, lastSeenVersion);
}
