namespace Gearbook.Core.Sorting;

/// <summary>
/// Putting a stored job order back the way it shipped, in whole or in part.
/// </summary>
/// <remarks>
/// A reset for one role has to leave the other roles alone, which is why this is not simply an
/// assignment. The stored order is one flat list holding every job, and the roles are drawn out
/// of it for display; so putting the tanks back in their default order means touching only the
/// places in that list where tanks sit, and leaving every other entry exactly where it was.
/// </remarks>
public static class JobOrderEditing
{
    /// <summary>
    /// Puts one group of jobs back into a wanted order, leaving everything else untouched.
    /// </summary>
    /// <param name="order">The stored order, holding every job.</param>
    /// <param name="group">The jobs to put back, which is one role's worth.</param>
    /// <param name="wanted">The order they should end up in, usually the game's own. May hold
    /// jobs outside the group, which are ignored.</param>
    /// <remarks>
    /// The group's members are refilled into the positions they already occupy, in the wanted
    /// sequence. That keeps the list's shape and touches nothing outside the group, which matters
    /// because a reset of one role must not quietly reorder another.
    ///
    /// A job in the group but missing from <paramref name="wanted"/> keeps a place at the end of
    /// the group rather than being dropped, because a reset is not a deletion.
    /// </remarks>
    public static IReadOnlyList<uint> Reset(
        IReadOnlyList<uint> order,
        IReadOnlyCollection<uint> group,
        IReadOnlyList<uint> wanted)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(wanted);

        var members = new HashSet<uint>(group);

        var replacement = new List<uint>(members.Count);

        foreach (var job in wanted)
        {
            if (members.Contains(job) && !replacement.Contains(job))
            {
                replacement.Add(job);
            }
        }

        // Anything in the group the wanted order does not mention. It follows the ones that were
        // named, in the order it already had, which is the least surprising place for it.
        foreach (var job in order)
        {
            if (members.Contains(job) && !replacement.Contains(job))
            {
                replacement.Add(job);
            }
        }

        var result = new List<uint>(order.Count);
        var next = 0;

        foreach (var job in order)
        {
            if (members.Contains(job))
            {
                result.Add(next < replacement.Count ? replacement[next] : job);
                next++;
            }
            else
            {
                result.Add(job);
            }
        }

        return result;
    }
}
