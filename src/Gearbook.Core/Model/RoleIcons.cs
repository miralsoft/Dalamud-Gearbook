namespace Gearbook.Core.Model;

/// <summary>
/// The game's own role symbols, as icon numbers.
/// </summary>
/// <remarks>
/// Here rather than in the plugin project because these are facts about the game's data, which
/// is where the rest of this namespace lives, and because a number is a number: nothing here
/// needs a platform assembly, so the mapping stays testable.
///
/// The numbers were read out of the installed client rather than remembered. Icon 62570 is a
/// label reading "ROLE BASE" and 62580 one reading "ROLE FRAMED", which is the game marking the
/// start of the group; the framed set is 62581 to 62589 and runs from the general to the
/// specific: tank, healer, damage, melee damage, ranged damage, physical ranged damage, magical
/// ranged damage, hand, land.
///
/// That reading is not an assumption about the order. 62585 is visibly the two pictures 62586
/// and 62587 drawn on top of each other, which only makes sense if it is the general case of
/// both, and that fixes the whole sequence around it.
///
/// Four of the nine are unused. 62583, the plain sword, is the game's symbol for damage of any
/// kind, and 62585 is ranged damage of either kind; this plugin separates physical from magical
/// everywhere else, so neither has anything to label. The last two, which the ordering says are
/// the hand and the land, lost to a pair of tool symbols for the reason given below.
///
/// The rest of the icon folder was swept for anything better before settling on tools: every
/// picture between 0 and 79999 in a plausible size was measured by the colour just inside its
/// frame. There is no crafting or gathering emblem in it beyond these, and no group label for
/// one either, where the game marks its other groups with label tiles it draws itself.
/// </remarks>
public static class RoleIcons
{
    /// <summary>No game symbol fits, and the caller should fall back to its own.</summary>
    public const uint None = 0;

    /// <summary>The three role colours stacked, which is the game's own "every combat role".</summary>
    private const uint AllCombatRoles = 62576;

    private const uint Tank = 62581;
    private const uint Healer = 62582;
    private const uint MeleeDamage = 62584;
    private const uint PhysicalRangedDamage = 62586;
    private const uint MagicalRangedDamage = 62587;

    /// <summary>An anvil, and a pickaxe, both framed the way the role symbols are.</summary>
    /// <remarks>
    /// These are tools rather than role emblems, and that is the second attempt at these two.
    /// The role block ends with a pair that is almost certainly the hand and the land, and they
    /// were used first for that reason, but they draw four metal discs and three nuggets on a
    /// green ground and nobody reading the bar could tell what either meant. A symbol that has to
    /// be explained has already failed at the only thing it does.
    ///
    /// The game gives these two to the blacksmith and the miner, so strictly a job's picture is
    /// standing in for a whole category. That is a real objection and it loses to a plainer one:
    /// an anvil reads as making things and a pickaxe as digging them up, to anybody, immediately.
    /// It also stays inside the game's own art, at the same size and in the same frame as
    /// everything beside it.
    /// </remarks>
    private const uint DiscipleOfTheHand = 62109;

    /// <inheritdoc cref="DiscipleOfTheHand"/>
    private const uint DiscipleOfTheLand = 62116;

    /// <summary>The game's symbol for a role, or <see cref="None"/>.</summary>
    public static uint For(JobRole role) => role switch
    {
        JobRole.Tank => Tank,
        JobRole.Healer => Healer,
        JobRole.MeleeDps => MeleeDamage,
        JobRole.PhysicalRangedDps => PhysicalRangedDamage,
        JobRole.MagicalRangedDps => MagicalRangedDamage,
        JobRole.Crafter => DiscipleOfTheHand,
        JobRole.Gatherer => DiscipleOfTheLand,
        _ => None,
    };

    /// <summary>The game's symbol for a category, or <see cref="None"/>.</summary>
    /// <remarks>
    /// Crafting and gathering share their symbol with the role of the same name, because they
    /// are the same set of gearsets seen from one level up. Giving them a second picture would
    /// claim a difference that does not exist.
    /// </remarks>
    public static uint For(JobCategory category) => category switch
    {
        JobCategory.Combat => AllCombatRoles,
        JobCategory.Crafting => DiscipleOfTheHand,
        JobCategory.Gathering => DiscipleOfTheLand,
        _ => None,
    };
}
