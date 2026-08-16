namespace Gearbook.Adapters;

/// <summary>How a rearrangement of the game's own gearset list ended.</summary>
internal enum ArrangeOutcome
{
    /// <summary>The list now reads as asked.</summary>
    Done = 0,

    /// <summary>Nothing is preventing a run right now. Only <c>CanArrange</c> answers this.</summary>
    Ready,

    /// <summary>It already did before anything was moved.</summary>
    NothingToDo,

    /// <summary>The player is not logged in.</summary>
    NotLoggedIn,

    /// <summary>
    /// Crafting, gathering, fishing, casting, in combat, in a cutscene, or changing zone.
    /// </summary>
    /// <remarks>
    /// The game will not take a reordering while it is busy with something else, and it does not
    /// say so: the call simply does nothing. Read back, that looks exactly like a move landing
    /// somewhere unexpected, so without this the run would stop with the wrong explanation.
    /// </remarks>
    Busy,

    /// <summary>The gearset module was not there to ask.</summary>
    Unavailable,

    /// <summary>
    /// A move did not land where it was aimed, so everything after it was abandoned.
    /// </summary>
    /// <remarks>
    /// The one outcome this whole component is shaped around. The game's own reordering call is
    /// used without a written guarantee of what it does to the entries in between, so every move
    /// is read back, and the first surprise stops the run rather than continuing into a list
    /// nobody can recognise.
    /// </remarks>
    Stopped,
}

/// <summary>What one rearrangement did.</summary>
/// <param name="Outcome">How it ended.</param>
/// <param name="MovesApplied">How many entries were moved before it ended.</param>
internal readonly record struct ArrangeResult(ArrangeOutcome Outcome, int MovesApplied);

/// <summary>
/// The one component allowed to reorder the game's own gearset list.
/// </summary>
/// <remarks>
/// <para>
/// A second gate beside the one that equips, and for the same reason: the line Dalamud draws
/// around automation has to be reviewable by reading one file per kind of thing this plugin does
/// to the game. This one writes to the player's gearset list, which is the more valuable of the
/// two, because an equip can be undone by equipping something else and an order built over years
/// cannot be typed back in.
/// </para>
/// <para>
/// It is on the allowed side of that line and stays there by construction. Nothing here happens
/// on a timer, on login or in reaction to anything the game does; every run begins with a player
/// pressing a button, and the operation used is the same one the game's own window performs when
/// that player drags an entry somewhere else. What it does not do is anything the player could
/// not do by hand, only faster.
/// </para>
/// </remarks>
internal interface IGearsetArranger
{
    /// <summary>
    /// Rearranges the game's gearset list into the wanted order.
    /// </summary>
    /// <param name="wantedOrder">The keys, by position, in the order the list should end up in.
    /// Built by the caller from the same sort the rest of the plugin uses.</param>
    /// <param name="onProgress">Called after each applied move with the number applied so far,
    /// so a long run can say something while it is happening.</param>
    ArrangeResult Arrange(IReadOnlyList<string> wantedOrder, Action<int>? onProgress = null);

    /// <summary>
    /// Whether a run could start right now, or what is preventing it.
    /// </summary>
    /// <remarks>
    /// Reads the host's condition flags and nothing else, so the interface may ask it while
    /// drawing to grey the button out with a reason. The same question is asked again on the
    /// framework thread before the run and before every single move, because the answer can
    /// change between a player reading a button and pressing it, and again while a run of thirty
    /// moves is under way.
    /// </remarks>
    ArrangeOutcome CanArrange();
}
