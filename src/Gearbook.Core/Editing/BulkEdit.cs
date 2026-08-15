using Gearbook.Core.Model;
using Gearbook.Core.Sorting;

namespace Gearbook.Core.Editing;

/// <summary>
/// Changes applied to several gearsets at once.
/// </summary>
/// <remarks>
/// <para>
/// Pure functions over the records, so the rules that decide what a bulk change does are tested
/// rather than living inside the window that offers them. That matters more here than elsewhere:
/// a mistake in a single edit costs one gearset, and the same mistake here costs forty.
/// </para>
/// <para>
/// Deliberately absent: anything that replaces free text. Setting a note across a selection would
/// overwrite forty things somebody wrote by hand, and there is no undo. Tags can be added and
/// removed because each is one word and putting it back is the same gesture that took it away.
/// </para>
/// </remarks>
public static class BulkEdit
{
    /// <summary>
    /// Adds a tag to every selected gearset, leaving the ones that already carry it alone.
    /// </summary>
    public static IReadOnlyList<GearsetRecord> AddTag(
        IReadOnlyList<GearsetRecord> records,
        IReadOnlySet<int> selection,
        string tag)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(selection);

        var trimmed = tag?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return records;
        }

        return [.. records.Select(record =>
        {
            if (!selection.Contains(record.Id) || HasTag(record, trimmed))
            {
                return record;
            }

            return record with { Tags = [.. record.Tags, trimmed] };
        })];
    }

    /// <summary>
    /// Removes a tag from every selected gearset, comparing case-insensitively so that what the
    /// filter treats as one tag is what this removes.
    /// </summary>
    public static IReadOnlyList<GearsetRecord> RemoveTag(
        IReadOnlyList<GearsetRecord> records,
        IReadOnlySet<int> selection,
        string tag)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(selection);

        var trimmed = tag?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return records;
        }

        return [.. records.Select(record =>
        {
            if (!selection.Contains(record.Id))
            {
                return record;
            }

            var kept = record.Tags
                .Where(t => !string.Equals(t.Trim(), trimmed, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            return kept.Count == record.Tags.Count ? record : record with { Tags = kept };
        })];
    }

    /// <summary>
    /// Marks or unmarks every selected gearset as a favourite, which is the same as putting it
    /// on the bar or taking it off.
    /// </summary>
    /// <remarks>
    /// Applied one at a time through the same function a single change uses, so the bar's
    /// positions come out contiguous and in a predictable order rather than needing a second
    /// rule for the bulk case.
    /// </remarks>
    public static IReadOnlyList<GearsetRecord> SetFavourite(
        IReadOnlyList<GearsetRecord> records,
        IReadOnlySet<int> selection,
        bool favourite)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(selection);

        var current = records;

        foreach (var id in selection.OrderBy(id => id))
        {
            current = BarOrder.SetFavourite(current, id, favourite);
        }

        return current;
    }

    /// <summary>Every tag carried by at least one of the selected gearsets, sorted.</summary>
    public static IReadOnlyList<string> TagsInSelection(
        IReadOnlyList<GearsetRecord> records,
        IReadOnlySet<int> selection)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(selection);

        return [.. records
            .Where(r => selection.Contains(r.Id))
            .SelectMany(r => r.Tags)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(t => t, StringComparer.CurrentCultureIgnoreCase)];
    }

    private static bool HasTag(GearsetRecord record, string tag) =>
        record.Tags.Any(t => string.Equals(t.Trim(), tag, StringComparison.CurrentCultureIgnoreCase));
}
