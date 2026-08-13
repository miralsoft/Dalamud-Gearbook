using Gearbook.Core.Model;
using Xunit;

namespace Gearbook.Core.Tests.Model;

public class EquipmentFingerprintTests
{
    [Fact]
    public void The_same_equipment_always_produces_the_same_digest()
    {
        uint[] items = [1000, 2000, 3000];

        Assert.Equal(EquipmentFingerprint.Of(items), EquipmentFingerprint.Of([1000, 2000, 3000]));
    }

    [Fact]
    public void Different_equipment_produces_a_different_digest()
    {
        Assert.NotEqual(
            EquipmentFingerprint.Of([1000, 2000, 3000]),
            EquipmentFingerprint.Of([1000, 2000, 3001]));
    }

    [Fact]
    public void The_same_pieces_in_different_slots_are_a_different_set()
    {
        Assert.NotEqual(
            EquipmentFingerprint.Of([1000, 2000]),
            EquipmentFingerprint.Of([2000, 1000]));
    }

    [Fact]
    public void Nothing_equipped_yields_an_empty_digest_rather_than_a_shared_one()
    {
        // The matching stage that uses this excludes empty digests. Without that, every set with
        // nothing in it would match every other one and the reconciler would pair them at random.
        Assert.Equal(string.Empty, EquipmentFingerprint.Of([]));
        Assert.Equal(string.Empty, EquipmentFingerprint.Of([0, 0, 0]));
    }

    [Fact]
    public void A_digest_is_short_enough_to_sit_in_a_configuration_file()
    {
        Assert.Equal(8, EquipmentFingerprint.Of([1000, 2000, 3000]).Length);
    }

    [Fact]
    public void An_empty_slot_among_real_pieces_still_counts_towards_the_digest()
    {
        Assert.NotEqual(
            EquipmentFingerprint.Of([1000, 0, 3000]),
            EquipmentFingerprint.Of([1000, 3000]));
    }
}
