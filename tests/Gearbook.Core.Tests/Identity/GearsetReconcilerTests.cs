using Gearbook.Core.Identity;
using Gearbook.Core.Model;
using Xunit;

namespace Gearbook.Core.Tests.Identity;

/// <summary>
/// The reconciler decides which saved favourite, tag and note belongs to which gearset. Getting
/// it wrong looks like data loss rather than like a bug, so every case the game can actually
/// produce is covered here.
/// </summary>
public class GearsetReconcilerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    private const uint DarkKnight = 32;
    private const uint WhiteMage = 24;
    private const uint Botanist = 17;

    private static GearsetSnapshot Gearset(
        int slot,
        uint job,
        string name,
        string fingerprint = "fp",
        int itemLevel = 790,
        bool mainHandMissing = false,
        int missingPieces = 0) =>
        new(
            Slot: slot,
            ClassJobId: job,
            Name: name,
            ItemLevel: itemLevel,
            MainHandMissing: mainHandMissing,
            MissingPieceCount: missingPieces,
            GlamourPlateLink: null,
            EquipmentFingerprint: fingerprint);

    private static GearsetRecord Record(
        int id,
        uint job,
        string name,
        int slot,
        string fingerprint = "fp",
        bool favourite = false,
        string note = "",
        params string[] tags) =>
        new(
            Id: id,
            ClassJobId: job,
            LastKnownName: name,
            LastKnownSlot: slot,
            LastKnownFingerprint: fingerprint,
            IsFavourite: favourite,
            Tags: tags,
            Note: note,
            BarPosition: null,
            LastUsedUtc: null,
            LastSeenUtc: null);

    [Fact]
    public void A_first_run_issues_one_record_per_gearset_and_orphans_nothing()
    {
        var gearsets = new[]
        {
            Gearset(1, WhiteMage, "Heal", "a"),
            Gearset(2, DarkKnight, "Tank", "b"),
        };

        var result = GearsetReconciler.Reconcile([], gearsets, nextId: 1, Now);

        Assert.Equal(2, result.Present.Count);
        Assert.Empty(result.Orphans);
        Assert.Empty(result.Ambiguities);
        Assert.All(result.Present, p => Assert.Equal(MatchStage.Created, p.Stage));
        Assert.Equal(3, result.NextId);
    }

    [Fact]
    public void An_issued_identifier_is_never_reused()
    {
        var first = GearsetReconciler.Reconcile(
            [],
            [Gearset(1, WhiteMage, "Heal", "a")],
            nextId: 7,
            Now);

        var second = GearsetReconciler.Reconcile(
            first.AllRecords,
            [Gearset(1, WhiteMage, "Heal", "a"), Gearset(2, DarkKnight, "Tank", "b")],
            first.NextId,
            Now);

        var ids = second.Present.Select(p => p.Record.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Contains(7, ids);
        Assert.True(second.NextId > ids.Max());
    }

    [Fact]
    public void An_unchanged_list_matches_exactly_and_carries_the_owned_values_across()
    {
        var records = new[]
        {
            Record(1, DarkKnight, "Ultimate", slot: 4, favourite: true, note: "for UWU", tags: "raid"),
        };

        var result = GearsetReconciler.Reconcile(
            records,
            [Gearset(4, DarkKnight, "Ultimate")],
            nextId: 2,
            Now);

        var matched = Assert.Single(result.Present);

        Assert.Equal(MatchStage.Exact, matched.Stage);
        Assert.Equal(1, matched.Record.Id);
        Assert.True(matched.Record.IsFavourite);
        Assert.Equal("for UWU", matched.Record.Note);
        Assert.Equal(["raid"], matched.Record.Tags);
    }

    [Fact]
    public void A_moved_gearset_keeps_its_record_and_the_record_learns_the_new_slot()
    {
        var records = new[] { Record(1, DarkKnight, "Ultimate", slot: 4, favourite: true) };

        // The player used the game's own "change number" and the set is now at 9.
        var result = GearsetReconciler.Reconcile(
            records,
            [Gearset(9, DarkKnight, "Ultimate")],
            nextId: 2,
            Now);

        var matched = Assert.Single(result.Present);

        Assert.Equal(MatchStage.Moved, matched.Stage);
        Assert.Equal(1, matched.Record.Id);
        Assert.True(matched.Record.IsFavourite);
        Assert.Equal(9, matched.Record.LastKnownSlot);
        Assert.Empty(result.Orphans);
    }

    [Fact]
    public void A_renamed_gearset_keeps_its_record_and_the_record_learns_the_new_name()
    {
        var records = new[] { Record(1, DarkKnight, "Old name", slot: 4, note: "keep me") };

        var result = GearsetReconciler.Reconcile(
            records,
            [Gearset(4, DarkKnight, "New name")],
            nextId: 2,
            Now);

        var matched = Assert.Single(result.Present);

        Assert.Equal(MatchStage.Renamed, matched.Stage);
        Assert.Equal("keep me", matched.Record.Note);
        Assert.Equal("New name", matched.Record.LastKnownName);
    }

    [Fact]
    public void A_gearset_that_was_renamed_and_moved_is_matched_on_its_equipment()
    {
        var records = new[] { Record(1, DarkKnight, "Old name", slot: 4, fingerprint: "abc", note: "keep me") };

        var result = GearsetReconciler.Reconcile(
            records,
            [Gearset(11, DarkKnight, "New name", "abc")],
            nextId: 2,
            Now);

        var matched = Assert.Single(result.Present);

        Assert.Equal(MatchStage.Fingerprint, matched.Stage);
        Assert.Equal("keep me", matched.Record.Note);
    }

    [Fact]
    public void An_empty_equipment_digest_never_matches_another_empty_one()
    {
        // Two sets with nothing equipped would otherwise look identical to the fingerprint
        // stage, and it would pair them at random.
        var records = new[] { Record(1, DarkKnight, "Old name", slot: 4, fingerprint: "") };

        var result = GearsetReconciler.Reconcile(
            records,
            [Gearset(11, DarkKnight, "New name", fingerprint: "")],
            nextId: 2,
            Now);

        Assert.Single(result.Orphans);
        Assert.Equal(MatchStage.Created, Assert.Single(result.Present).Stage);
    }

    [Fact]
    public void A_record_whose_gearset_is_gone_is_kept_rather_than_deleted()
    {
        var records = new[]
        {
            Record(1, WhiteMage, "Heal", slot: 1, favourite: true, note: "important"),
            Record(2, Botanist, "Gather", slot: 2),
        };

        var result = GearsetReconciler.Reconcile(
            records,
            [Gearset(1, WhiteMage, "Heal")],
            nextId: 3,
            Now);

        var orphan = Assert.Single(result.Orphans);

        Assert.Equal(2, orphan.Id);
        Assert.True(orphan.IsFavourite is false);
        Assert.Single(result.Present);

        // Nothing was dropped on the way through: what goes back to the configuration is the
        // matched record plus the orphan.
        Assert.Equal(records.Length, result.AllRecords.Count);
        Assert.Equal(
            records.Select(r => r.Id).OrderBy(id => id),
            result.AllRecords.Select(r => r.Id).OrderBy(id => id));
    }

    [Fact]
    public void Two_sets_sharing_a_job_and_a_name_are_matched_without_complaint_while_one_sits_still()
    {
        // This is the owner's real configuration: several dark knight sets, some named alike.
        var records = new[]
        {
            Record(1, DarkKnight, "Dark Knight", slot: 4, fingerprint: "raid", note: "raid set"),
            Record(2, DarkKnight, "Dark Knight", slot: 5, fingerprint: "glam", note: "glamour"),
        };

        var result = GearsetReconciler.Reconcile(
            records,
            [
                Gearset(4, DarkKnight, "Dark Knight", "raid"),
                Gearset(5, DarkKnight, "Dark Knight", "glam"),
            ],
            nextId: 3,
            Now);

        Assert.Empty(result.Ambiguities);
        Assert.All(result.Present, p => Assert.Equal(MatchStage.Exact, p.Stage));
        Assert.Equal("raid set", result.Present.Single(p => p.Gearset.Slot == 4).Record.Note);
        Assert.Equal("glamour", result.Present.Single(p => p.Gearset.Slot == 5).Record.Note);
    }

    [Fact]
    public void Two_indistinguishable_sets_that_both_moved_are_paired_by_slot_order_and_reported()
    {
        // Same job, same name, same equipment, and both slots changed. Nothing can tell these
        // apart, so the answer might be wrong and has to say so.
        var records = new[]
        {
            Record(1, DarkKnight, "Dark Knight", slot: 4, fingerprint: "same"),
            Record(2, DarkKnight, "Dark Knight", slot: 5, fingerprint: "same"),
        };

        var result = GearsetReconciler.Reconcile(
            records,
            [
                Gearset(11, DarkKnight, "Dark Knight", "same"),
                Gearset(12, DarkKnight, "Dark Knight", "same"),
            ],
            nextId: 3,
            Now);

        Assert.All(result.Present, p => Assert.Equal(MatchStage.SlotOrderFallback, p.Stage));
        Assert.Single(result.Ambiguities);
        Assert.Empty(result.Orphans);

        // Slot order on both sides: the record that sat lower gets the gearset that sits lower.
        Assert.Equal(1, result.Present.Single(p => p.Gearset.Slot == 11).Record.Id);
        Assert.Equal(2, result.Present.Single(p => p.Gearset.Slot == 12).Record.Id);
    }

    [Fact]
    public void An_ambiguity_is_never_resolved_silently()
    {
        var records = new[]
        {
            Record(1, DarkKnight, "Dark Knight", slot: 4, fingerprint: "same"),
            Record(2, DarkKnight, "Dark Knight", slot: 5, fingerprint: "same"),
        };

        var result = GearsetReconciler.Reconcile(
            records,
            [
                Gearset(11, DarkKnight, "Dark Knight", "same"),
                Gearset(12, DarkKnight, "Dark Knight", "same"),
            ],
            nextId: 3,
            Now);

        // The property, not the count: a fallback pairing and a reported ambiguity always come
        // together. Neither may appear without the other.
        Assert.Equal(
            result.Present.Any(p => p.Stage == MatchStage.SlotOrderFallback),
            result.Ambiguities.Any());
    }

    [Fact]
    public void A_more_certain_stage_wins_over_a_less_certain_one()
    {
        // The record could be matched to slot 4 by slot and job (renamed), or to slot 9 by job
        // and name (moved). Moved is the more certain claim and runs first.
        var records = new[] { Record(1, DarkKnight, "Ultimate", slot: 4, fingerprint: "a") };

        var result = GearsetReconciler.Reconcile(
            records,
            [
                Gearset(4, DarkKnight, "Something else", "b"),
                Gearset(9, DarkKnight, "Ultimate", "c"),
            ],
            nextId: 2,
            Now);

        var matched = result.Present.Single(p => p.Record.Id == 1);

        Assert.Equal(MatchStage.Moved, matched.Stage);
        Assert.Equal(9, matched.Gearset.Slot);
    }

    [Fact]
    public void Every_gearset_in_the_game_ends_up_present_exactly_once()
    {
        var records = new[]
        {
            Record(1, WhiteMage, "Heal", slot: 1),
            Record(2, DarkKnight, "Tank", slot: 2, fingerprint: "t"),
            Record(3, Botanist, "Gone", slot: 3),
        };

        var gearsets = new[]
        {
            Gearset(1, WhiteMage, "Heal"),
            Gearset(7, DarkKnight, "Renamed tank", "t"),
            Gearset(8, Botanist, "Brand new", "n"),
        };

        var result = GearsetReconciler.Reconcile(records, gearsets, nextId: 4, Now);

        Assert.Equal(gearsets.Length, result.Present.Count);
        Assert.Equal(
            gearsets.Select(g => g.Slot).OrderBy(s => s),
            result.Present.Select(p => p.Gearset.Slot).OrderBy(s => s));
        Assert.Equal(
            result.Present.Count,
            result.Present.Select(p => p.Record.Id).Distinct().Count());
    }

    [Fact]
    public void A_matched_record_records_when_it_was_last_seen()
    {
        var records = new[] { Record(1, WhiteMage, "Heal", slot: 1) };

        var result = GearsetReconciler.Reconcile(
            records,
            [Gearset(1, WhiteMage, "Heal")],
            nextId: 2,
            Now);

        Assert.Equal(Now, Assert.Single(result.Present).Record.LastSeenUtc);
    }

    [Fact]
    public void An_orphaned_record_keeps_the_moment_it_was_last_seen_rather_than_being_touched()
    {
        var seenBefore = Now.AddDays(-30);
        var records = new[]
        {
            Record(1, WhiteMage, "Heal", slot: 1) with { LastSeenUtc = seenBefore },
        };

        var result = GearsetReconciler.Reconcile(records, [], nextId: 2, Now);

        Assert.Equal(seenBefore, Assert.Single(result.Orphans).LastSeenUtc);
    }
}
