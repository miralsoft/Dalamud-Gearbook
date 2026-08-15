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
/// Every value below was read off the real job table in an installed client on 2026-08-15,
/// not inferred. The first attempt at this was inferred, and it was wrong in a way that only a
/// player noticed: it split crafters from gatherers by their position in the hand-and-land
/// sequence, and that sequence restarts at zero for the gatherers. A blacksmith and a botanist
/// both sit at index one, so every gatherer was filed as a crafter.
/// </para>
/// </remarks>
public static class JobClassifier
{
    /// <summary>
    /// The role values the game's own table uses. Verified against the table: gladiator and
    /// paladin are 1, pugilist and monk 2, archer and black mage 3, conjurer and white mage 4,
    /// and every hand or land job is 0, as are the starting classes' own rows.
    /// </summary>
    private const byte RoleNone = 0;
    private const byte RoleTank = 1;
    private const byte RoleMeleeDps = 2;
    private const byte RoleRangedDps = 3;
    private const byte RoleHealer = 4;

    /// <summary>
    /// The job category rows, which are the game's own grouping and the only field that
    /// separates a crafter from a gatherer. Verified by reading the category sheet: 30 is
    /// Disciple of War, 31 Disciple of Magic, 32 Disciple of the Land, 33 Disciple of the Hand.
    /// </summary>
    private const uint CategoryDiscipleOfWar = 30;
    private const uint CategoryDiscipleOfMagic = 31;
    private const uint CategoryDiscipleOfTheLand = 32;
    private const uint CategoryDiscipleOfTheHand = 33;

    /// <summary>
    /// The primary attributes that tell a physical ranged job from a magical one. The table has
    /// no flag for that distinction, both are role 3, and the only thing separating them is
    /// which attribute their damage scales from. Verified against the table: archer, bard,
    /// machinist and dancer scale from dexterity, while thaumaturge, black mage, arcanist,
    /// summoner, red mage, blue mage and pictomancer scale from intelligence.
    /// </summary>
    private const byte PrimaryStatDexterity = 2;
    private const byte PrimaryStatIntelligence = 4;

    /// <summary>
    /// Classifies a job.
    /// </summary>
    /// <param name="role">The table's own role value.</param>
    /// <param name="classJobCategoryRowId">The row the job's category points at. This is what
    /// separates the three categories, because the table gives crafters, gatherers and the
    /// starting classes the same role.</param>
    /// <param name="primaryStat">The attribute the job scales from.</param>
    public static (JobRole Role, JobCategory Category) Classify(
        byte role,
        uint classJobCategoryRowId,
        byte primaryStat)
    {
        switch (classJobCategoryRowId)
        {
            case CategoryDiscipleOfTheHand:
                return (JobRole.Crafter, JobCategory.Crafting);

            case CategoryDiscipleOfTheLand:
                return (JobRole.Gatherer, JobCategory.Gathering);

            default:
                break;
        }

        // Anything else is a fighting job. A category this build has never seen keeps its role
        // and loses only the coarser grouping, which is the harmless direction: the gearset is
        // still listed, still switchable, and still reachable by its role.
        var category = classJobCategoryRowId is CategoryDiscipleOfWar or CategoryDiscipleOfMagic
            ? JobCategory.Combat
            : JobCategory.Unknown;

        return role switch
        {
            RoleTank => (JobRole.Tank, category),
            RoleHealer => (JobRole.Healer, category),
            RoleMeleeDps => (JobRole.MeleeDps, category),

            RoleRangedDps => primaryStat switch
            {
                PrimaryStatDexterity => (JobRole.PhysicalRangedDps, category),
                PrimaryStatIntelligence => (JobRole.MagicalRangedDps, category),
                _ => (JobRole.Unknown, category),
            },

            // Role 0 in a fighting category is a starting class rather than a job. Still a
            // gearset, still switchable, simply without a role of its own.
            RoleNone => (JobRole.Unknown, category),

            _ => (JobRole.Unknown, category),
        };
    }
}
