using Gearbook.Core.Identity;
using Gearbook.Core.Model;

namespace Gearbook.Core.Sorting;

/// <summary>
/// The quick-switch bar's own order, which the player arranges by hand.
/// </summary>
/// <remarks>
/// Positions are kept as a contiguous run from zero with no gaps, because every operation here
/// renumbers afterwards. Gaps would work, but they make "move up" ambiguous the moment two
/// entries share a position, and sharing a position is exactly what happens when a record is
/// restored from an older configuration.
/// </remarks>
public static class BarOrder
{
    /// <summary>The gearsets on the bar, in the player's order.</summary>
    public static IReadOnlyList<ReconciledGearset> OnBar(IEnumerable<ReconciledGearset> gearsets)
    {
        ArgumentNullException.ThrowIfNull(gearsets);

        return [.. gearsets
            .Where(g => g.Record.BarPosition is not null)
            .OrderBy(g => g.Record.BarPosition!.Value)
            .ThenBy(g => g.Gearset.Slot)];
    }

    /// <summary>
    /// Renumbers the bar so its positions run from zero with no gaps and no duplicates, keeping
    /// the order the records already describe.
    /// </summary>
    public static IReadOnlyList<GearsetRecord> Normalise(IReadOnlyList<GearsetRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var ordered = records
            .Where(r => r.BarPosition is not null)
            .OrderBy(r => r.BarPosition!.Value)
            .ThenBy(r => r.Id)
            .ToList();

        var positions = new Dictionary<int, int>();
        for (var i = 0; i < ordered.Count; i++)
        {
            positions[ordered[i].Id] = i;
        }

        return [.. records.Select(r =>
            positions.TryGetValue(r.Id, out var position)
                ? r with { BarPosition = position }
                : r with { BarPosition = null })];
    }

    /// <summary>
    /// Puts a gearset on the bar at the end, or takes it off. Taking one off closes the gap it
    /// leaves behind.
    /// </summary>
    public static IReadOnlyList<GearsetRecord> SetOnBar(
        IReadOnlyList<GearsetRecord> records,
        int recordId,
        bool onBar)
    {
        ArgumentNullException.ThrowIfNull(records);

        var next = records.Count(r => r.BarPosition is not null);

        var updated = records.Select(r =>
        {
            if (r.Id != recordId)
            {
                return r;
            }

            if (!onBar)
            {
                return r with { BarPosition = null };
            }

            return r.BarPosition is not null ? r : r with { BarPosition = next };
        });

        return Normalise([.. updated]);
    }

    /// <summary>
    /// Moves an entry along the bar by <paramref name="delta"/> places, clamped at both ends.
    /// </summary>
    /// <remarks>
    /// Clamped rather than wrapped. Dragging the first entry up and having it appear at the far
    /// end is a surprise, and a control that surprises somebody once gets used carefully
    /// forever after.
    /// </remarks>
    public static IReadOnlyList<GearsetRecord> Move(
        IReadOnlyList<GearsetRecord> records,
        int recordId,
        int delta)
    {
        ArgumentNullException.ThrowIfNull(records);

        var normalised = Normalise(records);

        var ordered = normalised
            .Where(r => r.BarPosition is not null)
            .OrderBy(r => r.BarPosition!.Value)
            .ToList();

        var index = ordered.FindIndex(r => r.Id == recordId);
        if (index < 0 || delta == 0 || ordered.Count < 2)
        {
            return normalised;
        }

        var target = Math.Clamp(index + delta, 0, ordered.Count - 1);
        if (target == index)
        {
            return normalised;
        }

        var moving = ordered[index];
        ordered.RemoveAt(index);
        ordered.Insert(target, moving);

        var positions = new Dictionary<int, int>();
        for (var i = 0; i < ordered.Count; i++)
        {
            positions[ordered[i].Id] = i;
        }

        return [.. normalised.Select(r =>
            positions.TryGetValue(r.Id, out var position)
                ? r with { BarPosition = position }
                : r)];
    }
}
