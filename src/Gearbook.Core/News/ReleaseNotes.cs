using System.Text.Json.Serialization;

namespace Gearbook.Core.News;

/// <summary>
/// One version's notes, written for whoever plays with the plugin rather than for whoever works
/// on it. Changes are grouped by kind rather than tagged line by line, because a list of
/// strings under a heading is what a person can edit without getting it wrong.
/// </summary>
public sealed class ReleaseNoteVersion
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    /// <summary>Optional single paragraph. Omitted for a small release.</summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("added")]
    public List<string>? Added { get; set; }

    [JsonPropertyName("changed")]
    public List<string>? Changed { get; set; }

    [JsonPropertyName("fixed")]
    public List<string>? Fixed { get; set; }

    [JsonPropertyName("removed")]
    public List<string>? Removed { get; set; }

    /// <summary>True when this entry would render as an empty heading and nothing else.</summary>
    public bool HasContent =>
        !string.IsNullOrWhiteSpace(Summary)
        || Added?.Count > 0
        || Changed?.Count > 0
        || Fixed?.Count > 0
        || Removed?.Count > 0;
}

/// <summary>
/// The release notes for one language.
/// </summary>
public sealed class ReleaseNotesDocument
{
    [JsonPropertyName("versions")]
    public List<ReleaseNoteVersion> Versions { get; set; } = [];

    /// <summary>An empty document, which is what a missing or unreadable file resolves to.</summary>
    public static ReleaseNotesDocument Empty { get; } = new();

    /// <summary>
    /// The versions, newest first.
    /// </summary>
    /// <remarks>
    /// Sorted here rather than trusted from the file. Notes are hand-written, and appending a
    /// new version to the bottom is the obvious way to get the order wrong.
    /// </remarks>
    public IReadOnlyList<ReleaseNoteVersion> Newest() =>
        [.. Versions
            .Where(v => v is not null)
            .OrderByDescending(v => v.Version, VersionComparer.Instance)];

    /// <summary>The notes for one version, or null.</summary>
    public ReleaseNoteVersion? Find(string version) =>
        Versions.FirstOrDefault(v =>
            VersionComparer.Instance.Compare(v.Version, version) == 0);
}
