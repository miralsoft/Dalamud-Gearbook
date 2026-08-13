using Gearbook.Core.Model;

namespace Gearbook.Core.Settings;

/// <summary>
/// The stored form of a <see cref="GearsetRecord"/>.
/// </summary>
/// <remarks>
/// A separate, plainly mutable type rather than persisting the record directly. The record is
/// immutable with a positional constructor, which several serialisers handle only with the
/// right options set, and a configuration that fails to load is a failure that lands on the
/// player's machine rather than on ours. The mapping below is dull on purpose and is covered by
/// a round-trip test.
/// </remarks>
public sealed class GearsetRecordData
{
    public int Id { get; set; }

    public uint ClassJobId { get; set; }

    public string LastKnownName { get; set; } = string.Empty;

    public int LastKnownSlot { get; set; }

    public string LastKnownFingerprint { get; set; } = string.Empty;

    public bool IsFavourite { get; set; }

    public List<string> Tags { get; set; } = [];

    public string Note { get; set; } = string.Empty;

    public int? BarPosition { get; set; }

    public DateTimeOffset? LastUsedUtc { get; set; }

    public DateTimeOffset? LastSeenUtc { get; set; }

    /// <summary>Converts to the model type the rest of the core works in.</summary>
    public GearsetRecord ToRecord() => new(
        Id: Id,
        ClassJobId: ClassJobId,
        LastKnownName: LastKnownName ?? string.Empty,
        LastKnownSlot: LastKnownSlot,
        LastKnownFingerprint: LastKnownFingerprint ?? string.Empty,
        IsFavourite: IsFavourite,
        Tags: Tags is null ? [] : [.. Tags],
        Note: Note ?? string.Empty,
        BarPosition: BarPosition,
        LastUsedUtc: LastUsedUtc,
        LastSeenUtc: LastSeenUtc);

    /// <summary>Converts from the model type for storage.</summary>
    public static GearsetRecordData FromRecord(GearsetRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new GearsetRecordData
        {
            Id = record.Id,
            ClassJobId = record.ClassJobId,
            LastKnownName = record.LastKnownName,
            LastKnownSlot = record.LastKnownSlot,
            LastKnownFingerprint = record.LastKnownFingerprint,
            IsFavourite = record.IsFavourite,
            Tags = [.. record.Tags],
            Note = record.Note,
            BarPosition = record.BarPosition,
            LastUsedUtc = record.LastUsedUtc,
            LastSeenUtc = record.LastSeenUtc,
        };
    }
}
