namespace Gearbook.Core.Model;

/// <summary>
/// What this plugin saved about one gearset, and what has to survive the player renaming it,
/// reordering the list, or deleting something above it. Its counterpart is
/// <see cref="GearsetSnapshot"/>, which describes the game's current state.
/// </summary>
/// <remarks>
/// The game offers no stable identity for a gearset: the slot is the displayed number and the
/// game reassigns it, and the name is free text that can repeat for the same job. So this
/// plugin issues <see cref="Id"/> itself and re-derives the mapping on every load. The three
/// "last known" values below exist only as inputs to that matching, never as the identity
/// (GB-04).
/// </remarks>
/// <param name="Id">This plugin's own identifier, issued once and never reused. Everything the
/// player owns hangs off it.</param>
/// <param name="ClassJobId">The job this set was for when it was last seen.</param>
/// <param name="LastKnownName">The name it had when it was last seen.</param>
/// <param name="LastKnownSlot">The slot it sat in when it was last seen.</param>
/// <param name="LastKnownFingerprint">The equipment digest it had when it was last seen.</param>
/// <param name="IsFavourite">Whether the player marked it.</param>
/// <param name="Tags">The player's own tags. Ordered as entered, compared case-insensitively
/// by the filter engine.</param>
/// <param name="Note">The player's own note, or an empty string.</param>
/// <param name="BarPosition">Where it sits on the quick-switch bar, or null when it is not on
/// the bar.</param>
/// <param name="LastUsedUtc">When this plugin last equipped it, or null. The game does not
/// record this, so it is only ever as complete as the plugin's own history.</param>
/// <param name="LastSeenUtc">When it was last matched to a set that actually exists. What the
/// library window uses to say how long an unmatched record has been unmatched.</param>
public sealed record GearsetRecord(
    int Id,
    uint ClassJobId,
    string LastKnownName,
    int LastKnownSlot,
    string LastKnownFingerprint,
    bool IsFavourite,
    IReadOnlyList<string> Tags,
    string Note,
    int? BarPosition,
    DateTimeOffset? LastUsedUtc,
    DateTimeOffset? LastSeenUtc)
{
    /// <summary>
    /// A fresh record for a gearset seen for the first time. Everything the player owns starts
    /// empty; nothing is guessed from the set itself.
    /// </summary>
    public static GearsetRecord ForNewGearset(int id, GearsetSnapshot snapshot, DateTimeOffset now) =>
        new(
            Id: id,
            ClassJobId: snapshot.ClassJobId,
            LastKnownName: snapshot.Name,
            LastKnownSlot: snapshot.Slot,
            LastKnownFingerprint: snapshot.EquipmentFingerprint,
            IsFavourite: false,
            Tags: [],
            Note: string.Empty,
            BarPosition: null,
            LastUsedUtc: null,
            LastSeenUtc: now);

    /// <summary>
    /// The same record, with the "last known" values brought up to date after a successful
    /// match. What the player owns is carried across untouched, which is the entire point of
    /// the exercise.
    /// </summary>
    public GearsetRecord WithSightingOf(GearsetSnapshot snapshot, DateTimeOffset now) =>
        this with
        {
            ClassJobId = snapshot.ClassJobId,
            LastKnownName = snapshot.Name,
            LastKnownSlot = snapshot.Slot,
            LastKnownFingerprint = snapshot.EquipmentFingerprint,
            LastSeenUtc = now,
        };
}
