using System.Globalization;
using Gearbook.Core.Model;

namespace Gearbook.Core.Identity;

/// <summary>
/// Connects saved records to the gearsets the game currently holds.
/// </summary>
/// <remarks>
/// <para>
/// This is the piece whose failure destroys a player's data quietly. The game gives no stable
/// identity: the slot is the displayed number and the game's own "change number" reassigns it,
/// and the name is free text that can repeat for the same job. If the wrong record is attached
/// to the wrong set, favourites and notes swap, it reads as data loss rather than as a bug, and
/// by the time anybody notices the correct mapping is gone.
/// </para>
/// <para>
/// So the matching runs in stages, most certain first, and a stage only accepts a pairing when
/// it is unique. What is left over is never guessed: unmatched records are kept as orphans, and
/// a genuine ambiguity is resolved by slot order and said out loud.
/// </para>
/// <para>
/// A pure function of its inputs, including the clock and the next identifier, so that every
/// case below can be tested without a game and without a fixed date.
/// </para>
/// </remarks>
public static class GearsetReconciler
{
    /// <summary>
    /// Matches <paramref name="savedRecords"/> against <paramref name="currentGearsets"/>.
    /// </summary>
    /// <param name="savedRecords">What was loaded from the configuration.</param>
    /// <param name="currentGearsets">What was read out of the game.</param>
    /// <param name="nextId">The next identifier to issue. Returned advanced.</param>
    /// <param name="now">The clock, passed in so the result is reproducible.</param>
    public static ReconciliationResult Reconcile(
        IReadOnlyList<GearsetRecord> savedRecords,
        IReadOnlyList<GearsetSnapshot> currentGearsets,
        int nextId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(savedRecords);
        ArgumentNullException.ThrowIfNull(currentGearsets);

        var remainingRecords = new List<GearsetRecord>(savedRecords);
        var remainingGearsets = new List<GearsetSnapshot>(currentGearsets);
        var matched = new List<ReconciledGearset>();
        var ambiguities = new List<string>();

        // Stage 1: slot, job and name all agree. Nothing moved and nothing was renamed.
        MatchUniquely<(int Slot, uint Job, string Name)>(
            remainingRecords,
            remainingGearsets,
            matched,
            now,
            MatchStage.Exact,
            r => (r.LastKnownSlot, r.ClassJobId, r.LastKnownName),
            g => (g.Slot, g.ClassJobId, g.Name));

        // Stage 2: job and name agree. The set was moved, which is what reordering the game's
        // own list does to every set below the one that moved.
        MatchUniquely<(uint Job, string Name)>(
            remainingRecords,
            remainingGearsets,
            matched,
            now,
            MatchStage.Moved,
            r => (r.ClassJobId, r.LastKnownName),
            g => (g.ClassJobId, g.Name));

        // Stage 3: slot and job agree. The set was renamed in place.
        MatchUniquely<(int Slot, uint Job)>(
            remainingRecords,
            remainingGearsets,
            matched,
            now,
            MatchStage.Renamed,
            r => (r.LastKnownSlot, r.ClassJobId),
            g => (g.Slot, g.ClassJobId));

        // Stage 4: job and the equipment digest agree. Renamed and moved in one session. Last
        // because the digest changes whenever the player re-equips the set, so it is the least
        // durable of the four signals, not the strongest.
        MatchUniquely<(uint Job, string Fingerprint)>(
            remainingRecords,
            remainingGearsets,
            matched,
            now,
            MatchStage.Fingerprint,
            r => string.IsNullOrEmpty(r.LastKnownFingerprint)
                ? null
                : (r.ClassJobId, r.LastKnownFingerprint),
            g => string.IsNullOrEmpty(g.EquipmentFingerprint)
                ? null
                : (g.ClassJobId, g.EquipmentFingerprint));

        // Stage 5: several sets share a job and a name and nothing above could tell them apart.
        // This is a real configuration, not a corner case: one job with several sets named the
        // same is exactly how people keep a raid set and a glamour set side by side. Pair them
        // in slot order, which is stable and explicable, and say that this is what happened.
        MatchBySlotOrder(remainingRecords, remainingGearsets, matched, ambiguities, now);

        // Whatever gearset is still unclaimed is one this plugin has not seen before.
        var created = new List<ReconciledGearset>();
        foreach (var gearset in remainingGearsets.OrderBy(g => g.Slot))
        {
            var record = GearsetRecord.ForNewGearset(nextId, gearset, now);
            nextId++;
            created.Add(new ReconciledGearset(record, gearset, MatchStage.Created));
        }

        matched.AddRange(created);

        // Whatever record is still unclaimed describes a gearset that is not there. It might
        // have been deleted, or this might be a load where the game had not finished handing
        // over its list. Those two are indistinguishable from here, so the record is kept.
        var orphans = remainingRecords
            .OrderBy(r => r.Id)
            .ToList();

        var present = matched
            .OrderBy(m => m.Gearset.Slot)
            .ToList();

        return new ReconciliationResult(present, orphans, ambiguities, nextId);
    }

    /// <summary>
    /// Pairs records and gearsets that agree on a key, but only where exactly one of each
    /// carries that key. A key shared by two records and two gearsets says nothing about which
    /// belongs to which, so this stage declines rather than picking one.
    /// </summary>
    /// <remarks>
    /// A null key means the item cannot take part in this stage at all, which is how the
    /// fingerprint stage excludes sets that have no equipment digest. Without that, every empty
    /// digest would look like a match with every other empty digest.
    /// </remarks>
    private static void MatchUniquely<TKey>(
        List<GearsetRecord> remainingRecords,
        List<GearsetSnapshot> remainingGearsets,
        List<ReconciledGearset> matched,
        DateTimeOffset now,
        MatchStage stage,
        Func<GearsetRecord, TKey?> recordKey,
        Func<GearsetSnapshot, TKey?> gearsetKey)
        where TKey : struct
    {
        var recordsByKey = remainingRecords
            .Select(r => (Record: r, Key: recordKey(r)))
            .Where(x => x.Key.HasValue)
            .GroupBy(x => x.Key!.Value)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single().Record);

        var gearsetsByKey = remainingGearsets
            .Select(g => (Gearset: g, Key: gearsetKey(g)))
            .Where(x => x.Key.HasValue)
            .GroupBy(x => x.Key!.Value)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single().Gearset);

        foreach (var (key, record) in recordsByKey)
        {
            if (!gearsetsByKey.TryGetValue(key, out var gearset))
            {
                continue;
            }

            matched.Add(new ReconciledGearset(record.WithSightingOf(gearset, now), gearset, stage));
            remainingRecords.Remove(record);
            remainingGearsets.Remove(gearset);
        }
    }

    /// <summary>
    /// The last resort: sets that share a job and a name, paired by slot order. Every pairing
    /// made here produces an entry in the ambiguity list, because this is the one place where
    /// the answer might be wrong and nothing else would say so.
    /// </summary>
    private static void MatchBySlotOrder(
        List<GearsetRecord> remainingRecords,
        List<GearsetSnapshot> remainingGearsets,
        List<ReconciledGearset> matched,
        List<string> ambiguities,
        DateTimeOffset now)
    {
        var recordGroups = remainingRecords
            .GroupBy(r => (r.ClassJobId, r.LastKnownName))
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.LastKnownSlot).ToList());

        var gearsetGroups = remainingGearsets
            .GroupBy(g => (g.ClassJobId, g.Name))
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Slot).ToList());

        foreach (var (key, records) in recordGroups)
        {
            if (!gearsetGroups.TryGetValue(key, out var gearsets))
            {
                continue;
            }

            var pairs = Math.Min(records.Count, gearsets.Count);
            if (pairs == 0)
            {
                continue;
            }

            ambiguities.Add(string.Format(
                CultureInfo.InvariantCulture,
                "{0} saved records and {1} gearsets share job {2} and the name \"{3}\". "
                    + "They cannot be told apart, so they were paired in slot order. "
                    + "If a note or a favourite looks wrong on one of these, this is why.",
                records.Count,
                gearsets.Count,
                key.ClassJobId,
                key.Item2));

            for (var i = 0; i < pairs; i++)
            {
                var record = records[i];
                var gearset = gearsets[i];

                matched.Add(new ReconciledGearset(
                    record.WithSightingOf(gearset, now),
                    gearset,
                    MatchStage.SlotOrderFallback));

                remainingRecords.Remove(record);
                remainingGearsets.Remove(gearset);
            }
        }
    }
}
