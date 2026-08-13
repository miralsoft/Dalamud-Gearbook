namespace Gearbook.Core.Model;

/// <summary>
/// Turns the raw numbers out of the game's job table into a role and a category.
/// </summary>
/// <remarks>
/// <para>
/// Pure, and given the raw values rather than reading the table, so the mapping is testable
/// without a game. The adapter on the plugin side reads the row and passes the numbers in.
/// </para>
/// <para>
/// Two of the values below could not be derived from a flag and are named constants here, in one
/// place, with the reason. Everything else comes from a field the table carries explicitly.
/// </para>
/// </remarks>
public static class JobClassifier
{
    /// <summary>
    /// The role values the game's own table uses. Established from the table's shape rather
    /// than from a job list, so a new job in a future patch classifies itself.
    /// </summary>
    private const byte RoleNone = 0;
    private const byte RoleTank = 1;
    private const byte RoleMeleeDps = 2;
    private const byte RoleRangedDps = 3;
    private const byte RoleHealer = 4;

    /// <summary>
    /// The primary attribute values used to tell a physical ranged job from a magical one. The
    /// table has no flag for that distinction: both are role 3, and the only thing separating
    /// them is which attribute their damage scales from.
    /// </summary>
    /// <remarks>
    /// These two numbers are the one part of this mapping that has not been checked against a
    /// running game, only against the table's structure. If either is wrong, the affected jobs
    /// classify as <see cref="JobRole.Unknown"/> and appear under "Other" in the filter, which
    /// is a visible and reportable failure rather than a quietly wrong one. That is deliberate:
    /// guessing the other way round would put a black mage under physical ranged and nothing
    /// would ever say so.
    /// </remarks>
    private const byte PrimaryStatDexterity = 2;
    private const byte PrimaryStatIntelligence = 4;

    /// <summary>
    /// How many of the hand and land indices belong to the hand jobs. The table sequences the
    /// eight crafting jobs first and the three gathering jobs after them, and carries no flag
    /// that separates the two, so this is the boundary.
    /// </summary>
    /// <remarks>
    /// A named constant rather than a job list, so a hypothetical twelfth land job classifies
    /// itself. If the sequence ever changes, the failure is that crafters and gatherers swap
    /// categories, which is immediately visible in the filter rather than silent.
    /// </remarks>
    private const sbyte HandJobCount = 8;

    /// <summary>
    /// Classifies a job.
    /// </summary>
    /// <param name="role">The table's own role value.</param>
    /// <param name="dohDolJobIndex">The table's index among the hand and land jobs. Negative for
    /// a combat job, which is the flag that separates the three categories.</param>
    /// <param name="primaryStat">The attribute the job scales from.</param>
    public static (JobRole Role, JobCategory Category) Classify(
        byte role,
        sbyte dohDolJobIndex,
        byte primaryStat)
    {
        // A hand or land job. The table gives these role 0, the same value it gives the
        // starting classes, so the index is what tells them apart rather than the role.
        if (dohDolJobIndex >= 0)
        {
            return dohDolJobIndex < HandJobCount
                ? (JobRole.Crafter, JobCategory.Crafting)
                : (JobRole.Gatherer, JobCategory.Gathering);
        }

        return role switch
        {
            RoleTank => (JobRole.Tank, JobCategory.Combat),
            RoleHealer => (JobRole.Healer, JobCategory.Combat),
            RoleMeleeDps => (JobRole.MeleeDps, JobCategory.Combat),

            RoleRangedDps => primaryStat switch
            {
                PrimaryStatDexterity => (JobRole.PhysicalRangedDps, JobCategory.Combat),
                PrimaryStatIntelligence => (JobRole.MagicalRangedDps, JobCategory.Combat),

                // Ranged, but the split could not be established. Combat is still known, so the
                // set keeps its category and only loses the finer filter.
                _ => (JobRole.Unknown, JobCategory.Combat),
            },

            // Role 0 with no hand or land index is a starting class rather than a job. It is
            // still a combat gearset and still switchable, it simply has no role of its own.
            RoleNone => (JobRole.Unknown, JobCategory.Combat),

            // A role value from a future patch. Not an error, and not a reason to hide the
            // gearset.
            _ => (JobRole.Unknown, JobCategory.Unknown),
        };
    }
}
