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
/// start of the group. Within it, 62581 is the tank, 62582 the healer, 62583 damage of any kind,
/// 62584 melee damage, 62586 physical ranged damage and 62587 magical ranged damage.
///
/// None of that is assumed from the order. 62585 is visibly the two pictures 62586 and 62587
/// drawn on top of each other, which only makes sense if it is the general case of both, and the
/// same six numbers are used the same way by DelvUI, which has shipped them for years.
///
/// **There is no role symbol for crafting or gathering, and the search for one is finished.**
/// Three separate findings say so. The game labels every one of its icon groups with a tile it
/// draws itself, and all 132 of those labels were read: there is `CLASS JOB`, `CLASS JOB FRAMED`,
/// `GTR TYPE`, `ROLE BASE`, `ROLE FRAMED`, `GEAR SET` and no group for job categories anywhere.
/// The two entries at the end of the role block, 62588 and 62589, sit on exactly the healer's
/// green ground, pixel for pixel, while the crafting and gathering jobs use the dark one, so they
/// are not the hand and the land whatever else they are; no plugin on GitHub uses either number.
/// And DelvUI, asked for the role icon of a crafter or a gatherer, returns that job's own icon,
/// which is the same answer this file arrives at below.
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
    /// A job's picture standing in for a whole category, which is the borrowing this project
    /// refused for the favourites star. It is right here for a reason that does not apply there:
    /// the game has no symbol for these two categories at all, so there is nothing to borrow
    /// from and nothing being misrepresented. DelvUI reaches the same fallback from the same dead
    /// end, by returning the job's own icon when asked for a crafter's or a gatherer's role icon.
    ///
    /// The blacksmith's anvil and the miner's pickaxe were chosen from among the jobs because
    /// they read as making things and digging them up without being explained, which is the whole
    /// job of a symbol, and because they are framed and shaded like everything beside them.
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
