namespace Gearbook.Core.Model;

/// <summary>
/// One symbol, as up to two of the game's pictures stacked.
/// </summary>
/// <param name="Base">The picture underneath, or zero for none at all.</param>
/// <param name="Overlay">A picture drawn over it, or zero when the base says everything.</param>
/// <remarks>
/// Two layers because the game keeps its role tiles in two halves and this project needs a
/// combination the game never assembled. `ROLE BASE` is a group of empty framed colours and the
/// class symbols are silver tools on nothing; the game puts the tools it wanted onto the colours
/// it wanted and ships the result. For crafting and gathering it never made that pair, so this
/// makes it at drawing time out of the same two halves.
///
/// Assembling rather than shipping a finished picture is the whole point. Everything stays in
/// the player's own installation, which is why plugins address game art by number instead of
/// bundling it.
/// </remarks>
public readonly record struct RoleSymbol(uint Base, uint Overlay)
{
    /// <summary>No game symbol fits, and the caller should fall back to its own.</summary>
    public static RoleSymbol None => default;

    /// <summary>One of the game's finished pictures, with nothing over it.</summary>
    public static RoleSymbol Plain(uint icon) => new(icon, 0);

    /// <summary>True when there is nothing to draw.</summary>
    public bool IsNone => Base == 0;

    /// <summary>True when a second picture goes over the first.</summary>
    public bool IsLayered => Overlay != 0;
}

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
/// **There is no finished role symbol for crafting or gathering, and the search for one is over.**
/// The game labels every one of its icon groups with a tile it draws itself, and all 132 of those
/// labels were read: there is `CLASS JOB`, `CLASS JOB FRAMED`, `GTR TYPE`, `ROLE BASE`,
/// `ROLE FRAMED`, `GEAR SET` and no group for job categories anywhere. The two entries at the end
/// of the role block, 62588 and 62589, sit on exactly the healer's green ground, pixel for pixel,
/// while the crafting and gathering jobs use the dark one, so they are not the hand and the land
/// whatever else they are; no plugin on GitHub uses either number. DelvUI, asked for the role
/// icon of a crafter or a gatherer, falls back to that job's own icon.
///
/// So these two are built rather than found, from the halves the game does provide.
/// </remarks>
public static class RoleIcons
{
    /// <summary>No game symbol fits, and the caller should fall back to its own.</summary>
    public static RoleSymbol None => RoleSymbol.None;

    // The empty framed colours of the `ROLE BASE` group. Blue, green and red belong to the tank,
    // the healer and damage, so the two left over are the two the categories can have without
    // claiming a role's colour.
    private const uint DarkGround = 62574;
    private const uint EarthGround = 62575;

    /// <summary>The three role colours stacked, which is the game's own "every combat role".</summary>
    private const uint AllCombatRoles = 62576;

    private const uint Tank = 62581;
    private const uint Healer = 62582;
    private const uint MeleeDamage = 62584;
    private const uint PhysicalRangedDamage = 62586;
    private const uint MagicalRangedDamage = 62587;

    // The silver tools of the `CLASS JOB` group, drawn on nothing, which is what makes them
    // usable as a layer. An anvil reads as making things and a pickaxe as digging them up,
    // without being explained, which is the whole job of a symbol.
    private const uint AnvilGlyph = 62009;
    private const uint PickaxeGlyph = 62016;

    /// <summary>The game's symbol for a role, or <see cref="None"/>.</summary>
    public static RoleSymbol For(JobRole role) => role switch
    {
        JobRole.Tank => RoleSymbol.Plain(Tank),
        JobRole.Healer => RoleSymbol.Plain(Healer),
        JobRole.MeleeDps => RoleSymbol.Plain(MeleeDamage),
        JobRole.PhysicalRangedDps => RoleSymbol.Plain(PhysicalRangedDamage),
        JobRole.MagicalRangedDps => RoleSymbol.Plain(MagicalRangedDamage),
        JobRole.Crafter => For(JobCategory.Crafting),
        JobRole.Gatherer => For(JobCategory.Gathering),
        _ => None,
    };

    /// <summary>The game's symbol for a category, or <see cref="None"/>.</summary>
    /// <remarks>
    /// Crafting and gathering share their symbol with the role of the same name, because they are
    /// the same set of gearsets seen from one level up. Giving them a second picture would claim
    /// a difference that does not exist.
    ///
    /// The two built ones deliberately avoid the colours the roles use. On a blue, green or red
    /// ground they would read as a fourth and fifth role rather than as the level above one, and
    /// the tricolour is already spoken for by combat.
    /// </remarks>
    public static RoleSymbol For(JobCategory category) => category switch
    {
        JobCategory.Combat => RoleSymbol.Plain(AllCombatRoles),
        JobCategory.Crafting => new RoleSymbol(DarkGround, AnvilGlyph),
        JobCategory.Gathering => new RoleSymbol(EarthGround, PickaxeGlyph),
        _ => None,
    };
}
