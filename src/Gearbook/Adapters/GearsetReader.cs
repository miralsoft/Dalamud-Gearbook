using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Gearbook.Core.Model;
using Gearbook.Services;

namespace Gearbook.Adapters;

/// <summary>
/// Reads <see cref="RaptureGearsetModule"/>.
/// </summary>
/// <remarks>
/// <para>
/// Called from the framework thread only. Every pointer is checked on every path, including the
/// ones that worked a moment earlier: an instance accessor can return null at any time, and an
/// addon under construction can report a count for an array that does not exist yet.
/// </para>
/// <para>
/// A try block around this would not make it safe. It catches a managed exception, and an access
/// violation is not one; it ends the process regardless of any handler. The checks below are what
/// prevent the crash, and the handler only buys early detection and a clean unload.
/// </para>
/// </remarks>
internal sealed unsafe class GearsetReader : IGearsetReader
{
    /// <summary>
    /// The module holds a fixed array of this many entries. Read from the structure rather than
    /// assumed, and it is also the limit the game itself shows in its gearset window.
    /// </summary>
    private const int MaxGearsets = 100;

    /// <summary>
    /// The value <see cref="RaptureGearsetModule.GearsetEntry.GlamourSetLink"/> carries when no
    /// glamour plate is linked.
    /// </summary>
    /// <remarks>
    /// Not established by reflection, which gives the field and not its meaning, and recorded in
    /// the project's open points as something to check against a running game. Reading it wrong
    /// costs a wrong plate number in a tooltip and nothing else, which is why it is not worth
    /// blocking on: the value is displayed, never passed back to the game.
    /// </remarks>
    private const byte NoGlamourPlate = 0;

    /// <inheritdoc />
    public IReadOnlyList<GearsetSnapshot> Read()
    {
        var module = RaptureGearsetModule.Instance();
        if (module is null)
        {
            return [];
        }

        var gearsets = new List<GearsetSnapshot>();

        for (var slot = 0; slot < MaxGearsets; slot++)
        {
            if (!module->IsValidGearset(slot))
            {
                continue;
            }

            var entry = module->GetGearset(slot);
            if (entry is null)
            {
                continue;
            }

            var snapshot = ReadEntry(slot, entry);
            if (snapshot is not null)
            {
                gearsets.Add(snapshot);
            }
        }

        return gearsets;
    }

    /// <inheritdoc />
    public int? CurrentSlot()
    {
        var module = RaptureGearsetModule.Instance();
        if (module is null)
        {
            return null;
        }

        var current = module->CurrentGearsetIndex;
        return current >= 0 && current < MaxGearsets ? current : null;
    }

    /// <inheritdoc />
    public ulong? CharacterContentId()
    {
        var module = RaptureGearsetModule.Instance();
        if (module is null)
        {
            return null;
        }

        var contentId = module->CharacterContentId;
        return contentId == 0 ? null : contentId;
    }

    private static GearsetSnapshot? ReadEntry(int slot, RaptureGearsetModule.GearsetEntry* entry)
    {
        if (!entry->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists))
        {
            return null;
        }

        var name = entry->NameString ?? string.Empty;

        var itemIds = new List<uint>(14);
        var missingPieces = 0;

        var items = entry->Items;
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            itemIds.Add(item.ItemId);

            // A slot the set names nothing for is not a missing piece, it is an empty slot. A
            // set with no off hand is complete; a set whose off hand has been sold is not.
            if (item.ItemId != 0
                && item.Flags.HasFlag(RaptureGearsetModule.GearsetItemFlag.ItemMissing))
            {
                missingPieces++;
            }
        }

        var glamourPlate = entry->GlamourSetLink == NoGlamourPlate
            ? (byte?)null
            : entry->GlamourSetLink;

        return new GearsetSnapshot(
            Slot: slot,
            ClassJobId: entry->ClassJob,
            Name: name,
            ItemLevel: entry->ItemLevel,
            MainHandMissing: entry->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.MainHandMissing),
            MissingPieceCount: missingPieces,
            GlamourPlateLink: glamourPlate,
            EquipmentFingerprint: EquipmentFingerprint.Of(itemIds));
    }

    /// <summary>
    /// Reads while reporting a failure rather than letting one escape into the host.
    /// </summary>
    internal static IReadOnlyList<GearsetSnapshot> ReadSafely(IGearsetReader reader)
    {
        try
        {
            return reader.Read();
        }
        catch (Exception ex)
        {
            GearbookServices.Log.Error(ex, "Reading the gearsets failed. Keeping the previous list.");
            return [];
        }
    }
}
