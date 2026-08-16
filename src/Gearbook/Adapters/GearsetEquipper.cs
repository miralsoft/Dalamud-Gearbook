using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Gearbook.Services;

namespace Gearbook.Adapters;

/// <summary>
/// The single gate to the server. The only type in this codebase that calls
/// <see cref="RaptureGearsetModule.EquipGearset"/>.
/// </summary>
internal sealed unsafe class GearsetEquipper : IGearsetEquipper
{
    /// <summary>
    /// The value passed as the glamour plate argument to mean "whatever the set is linked to".
    /// </summary>
    /// <remarks>
    /// Reflection gives the signature and not the semantics, so this is recorded in the
    /// project's open points as something to verify against a running game. Zero is the
    /// conservative choice: it is the value the game's own gearset window passes when the player
    /// has not asked for a specific plate.
    /// </remarks>
    private const byte UseLinkedGlamourPlate = 0;

    private readonly IGameStateProbe gameState;

    public GearsetEquipper(IGameStateProbe gameState)
    {
        this.gameState = gameState;
    }

    /// <inheritdoc />
    public EquipOutcome CheckCanChangeGear()
    {
        if (!gameState.IsLoggedIn)
        {
            return EquipOutcome.NotLoggedIn;
        }

        if (gameState.IsInCutscene)
        {
            return EquipOutcome.InCutscene;
        }

        if (gameState.IsInCombat)
        {
            return EquipOutcome.InCombat;
        }

        if (gameState.IsOccupied)
        {
            return EquipOutcome.Occupied;
        }

        return EquipOutcome.Sent;
    }

    /// <inheritdoc />
    public EquipOutcome CheckCanEquip(int slot)
    {
        var allowed = CheckCanChangeGear();
        if (allowed != EquipOutcome.Sent)
        {
            return allowed;
        }

        // A set missing a piece is not refused here. The game answers that case itself, by
        // asking whether to use a suitable substitute, and the player decides. Nothing in this
        // plugin touches that dialog; it appears because the game put it there.
        if (gameState.CurrentGearsetSlot == slot)
        {
            return EquipOutcome.AlreadyWorn;
        }

        return EquipOutcome.Sent;
    }

    /// <inheritdoc />
    public EquipOutcome Equip(int slot, EquipTrigger trigger)
    {
        // The check runs again here rather than trusting the interface to have run it. The
        // interface asked a frame ago, and combat can start in between.
        var allowed = CheckCanEquip(slot);
        if (allowed != EquipOutcome.Sent)
        {
            GearbookServices.Log.Debug(
                "Gearset change to {Slot} skipped, triggered from {Trigger}: {Reason}.",
                slot,
                trigger,
                allowed);

            return allowed;
        }

        var module = RaptureGearsetModule.Instance();
        if (module is null)
        {
            GearbookServices.Log.Debug(
                "Gearset change to {Slot} skipped, triggered from {Trigger}: the gearset module was not available.",
                slot,
                trigger);

            return EquipOutcome.Refused;
        }

        if (!module->IsValidGearset(slot))
        {
            GearbookServices.Log.Debug(
                "Gearset change to {Slot} skipped, triggered from {Trigger}: no such gearset.",
                slot,
                trigger);

            return EquipOutcome.Refused;
        }

        var result = module->EquipGearset(slot, UseLinkedGlamourPlate);

        // Logged either way, with the trigger. In the failure case this is what separates "it
        // tried and the game refused" from "it never tried".
        GearbookServices.Log.Information(
            "Gearset change to {Slot}, triggered from {Trigger}, returned {Result}.",
            slot,
            trigger,
            result);

        return result < 0 ? EquipOutcome.Refused : EquipOutcome.Sent;
    }
}
