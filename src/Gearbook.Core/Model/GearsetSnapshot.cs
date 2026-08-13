namespace Gearbook.Core.Model;

/// <summary>
/// One gearset as it exists in the game right now. Transient: it describes this moment and is
/// replaced whole on the next read. Its counterpart is <see cref="GearsetRecord"/>, which is
/// what this plugin saved and what outlives the session.
/// </summary>
/// <remarks>
/// These are two types on purpose. Merging them is how a note ends up attached to the wrong
/// gearset: the saved half would then inherit whatever the game happened to say this frame,
/// including a slot number that moved. Only <see cref="Identity.GearsetReconciler"/> joins them.
/// </remarks>
/// <param name="Slot">The number the game shows in its own gearset list. Not an identity: the
/// game's own "change number" reassigns it, so nothing persisted may key on it alone (GB-04).</param>
/// <param name="ClassJobId">The game's job id.</param>
/// <param name="Name">The gearset name, free text, editable by the player, and not unique. The
/// same job and the same name can appear more than once.</param>
/// <param name="ItemLevel">The item level the game itself shows beside the set. Read from the
/// gearset entry rather than computed from its fourteen pieces.</param>
/// <param name="MainHandMissing">The set has no main hand, which is the game's own flag for a
/// set that cannot be equipped as it stands.</param>
/// <param name="MissingPieceCount">How many of the equipment slots the set names an item for
/// that is no longer in the player's possession.</param>
/// <param name="GlamourPlateLink">The glamour plate this set is linked to, or null when there
/// is none.</param>
/// <param name="EquipmentFingerprint">A stable digest of the set's equipment, used as the last
/// resort when matching a set that was both renamed and moved. It changes when the player
/// re-equips the set, which is why it is the last stage and not the first.</param>
public sealed record GearsetSnapshot(
    int Slot,
    uint ClassJobId,
    string Name,
    int ItemLevel,
    bool MainHandMissing,
    int MissingPieceCount,
    byte? GlamourPlateLink,
    string EquipmentFingerprint)
{
    /// <summary>
    /// True when the set names at least one piece the player no longer has, or has no main
    /// hand at all. This is what the "incomplete" filter asks.
    /// </summary>
    public bool IsIncomplete => MainHandMissing || MissingPieceCount > 0;
}
