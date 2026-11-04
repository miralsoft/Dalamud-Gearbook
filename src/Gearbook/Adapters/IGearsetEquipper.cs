namespace Gearbook.Adapters;

/// <summary>Why a gearset change did or did not happen.</summary>
internal enum EquipOutcome
{
    /// <summary>The game accepted the change.</summary>
    Sent = 0,

    /// <summary>The player is not logged in.</summary>
    NotLoggedIn,

    /// <summary>In combat.</summary>
    InCombat,

    /// <summary>In a cutscene.</summary>
    InCutscene,

    /// <summary>Busy with something the game will not interrupt.</summary>
    Occupied,

    /// <summary>The game refused it. Nothing was equipped.</summary>
    Refused,
}

/// <summary>Where a change was asked for. Recorded with every attempt.</summary>
internal enum EquipTrigger
{
    Bar = 0,
    Library,
    Command,
}

/// <summary>
/// The one component allowed to change the player's gearset.
/// </summary>
/// <remarks>
/// <para>
/// This exists so that the line Dalamud draws around automation can be reviewed by reading one
/// file. Its restrictions forbid interacting with the game servers automatically, and somebody
/// checking that has to be able to prove a negative. Spread across the plugin, that proof would
/// mean auditing everything.
/// </para>
/// <para>
/// Nothing else in this codebase holds an instance of this. There is no queue, no retry and no
/// deferred change: a switch that cannot happen now is reported and dropped. A change that
/// fires later by itself is exactly the automatic interaction that is forbidden, and it would
/// arrive looking like a convenience.
/// </para>
/// </remarks>
internal interface IGearsetEquipper
{
    /// <summary>
    /// Equips a gearset, if it can be equipped right now.
    /// </summary>
    /// <param name="slot">The game's own gearset number.</param>
    /// <param name="trigger">What the player did to ask for this. Logged, because in the failure
    /// case it is what separates "it tried and the game refused" from "it never tried", which
    /// have completely different causes and look identical from the outside.</param>
    EquipOutcome Equip(int slot, EquipTrigger trigger);

    /// <summary>
    /// Why no gearset can be equipped right now, or <see cref="EquipOutcome.Sent"/> when one can.
    /// What the interface asks before greying a tile out, so that the reason in the tooltip and
    /// the reason in the log are the same answer from the same code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing about the particular set is a reason, on purpose. A set missing a piece is handled
    /// by the game itself, which offers a substitute and lets the player decide. The set already
    /// worn is not refused either: the game re-equips it, which is how a player restores a set
    /// after changing a piece by hand, and the plugin's idea of which set is worn can be stale.
    /// Refusing either would take away something the player has in the game's own window.
    /// </para>
    /// <para>
    /// The component that reorders the gearset list asks the same question, because it must not
    /// run while the game is refusing to touch gear either. Two lists of conditions would drift.
    /// </para>
    /// </remarks>
    EquipOutcome CheckCanChangeGear();
}
