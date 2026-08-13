using System.Globalization;

namespace Gearbook.Core.Model;

/// <summary>
/// A short, stable digest of a gearset's equipment.
/// </summary>
/// <remarks>
/// The last resort when matching a gearset that was both renamed and moved. It has to be stable
/// across sessions and cheap enough to compute for every set on every read, and it never leaves
/// this machine, so a non-cryptographic hash is the right tool. FNV-1a because it is four lines,
/// has no dependencies, and produces the same answer on every platform, which a runtime string
/// hash explicitly does not.
/// </remarks>
public static class EquipmentFingerprint
{
    private const uint OffsetBasis = 2166136261;
    private const uint Prime = 16777619;

    /// <summary>
    /// The digest of a sequence of item identifiers, in slot order. Order matters: the same
    /// pieces worn in different slots is a different set.
    /// </summary>
    /// <returns>An eight-character hexadecimal string, or an empty string when nothing is
    /// equipped. Empty on purpose: the matching stage that uses this excludes empty digests,
    /// because otherwise every set with nothing in it would match every other one.</returns>
    public static string Of(IEnumerable<uint> itemIds)
    {
        ArgumentNullException.ThrowIfNull(itemIds);

        var hash = OffsetBasis;
        var any = false;

        foreach (var id in itemIds)
        {
            if (id != 0)
            {
                any = true;
            }

            unchecked
            {
                for (var shift = 0; shift < 32; shift += 8)
                {
                    hash ^= (byte)(id >> shift);
                    hash *= Prime;
                }
            }
        }

        return any ? hash.ToString("x8", CultureInfo.InvariantCulture) : string.Empty;
    }
}
