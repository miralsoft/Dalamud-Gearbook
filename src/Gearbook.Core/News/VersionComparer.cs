using System.Globalization;

namespace Gearbook.Core.News;

/// <summary>
/// Compares version strings tolerantly and never throws.
/// </summary>
/// <remarks>
/// <para>
/// The strings here arrive from a hand-written data file and from a saved setting, so both can
/// be absent, empty, or nonsense. This runs inside a draw callback, where an exception is not a
/// caught error but a closed game client.
/// </para>
/// <para>
/// Anything unreadable sorts as the oldest possible version, which is the harmless direction: it
/// shows the notes once too often rather than never. A pre-release or build suffix is ignored,
/// so 0.2.0-beta.1 and 0.2.0 compare equal.
/// </para>
/// </remarks>
public sealed class VersionComparer : IComparer<string?>
{
    /// <summary>The shared instance. This type holds no state.</summary>
    public static VersionComparer Instance { get; } = new();

    /// <inheritdoc />
    public int Compare(string? x, string? y)
    {
        var left = Parse(x);
        var right = Parse(y);

        for (var i = 0; i < 4; i++)
        {
            var comparison = left[i].CompareTo(right[i]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    /// <summary>
    /// True when <paramref name="candidate"/> describes a later version than
    /// <paramref name="baseline"/>. What decides whether the release notes open by themselves.
    /// </summary>
    public static bool IsNewer(string? candidate, string? baseline) =>
        Instance.Compare(candidate, baseline) > 0;

    /// <summary>
    /// Reduces a version string to four numbers. Everything from the first character that is
    /// neither a digit nor a dot is discarded, which is what makes the suffix rule work.
    /// </summary>
    private static int[] Parse(string? version)
    {
        var parts = new int[4];

        if (string.IsNullOrWhiteSpace(version))
        {
            return parts;
        }

        var trimmed = version.Trim();

        var cut = trimmed.IndexOfAny(['-', '+', ' ']);
        if (cut >= 0)
        {
            trimmed = trimmed[..cut];
        }

        var segments = trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < parts.Length && i < segments.Length; i++)
        {
            if (int.TryParse(segments[i], NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                && value >= 0)
            {
                parts[i] = value;
            }
            else
            {
                // A segment that is not a plain number makes everything from here on
                // meaningless, so it stops rather than skipping and pretending the next
                // segment belonged in this position.
                break;
            }
        }

        return parts;
    }
}
