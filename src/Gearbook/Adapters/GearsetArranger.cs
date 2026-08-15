using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Gearbook.Core.Arranging;
using Gearbook.Core.Model;
using Gearbook.Services;

namespace Gearbook.Adapters;

/// <summary>
/// The only type in this codebase that calls
/// <see cref="RaptureGearsetModule.ReassignGearsetId"/>.
/// </summary>
/// <remarks>
/// <para>
/// Every move is read back before the next one is decided. That is not defensive habit, it is
/// the design: the game's reordering call is used without a written guarantee of what it does to
/// the entries between the two ends of a move, and a wrong assumption applied thirty times to a
/// list somebody built over years is not something an apology fixes. Applied once and checked,
/// it is a move the player can drag back.
/// </para>
/// <para>
/// The loop is bounded by the number of entries rather than by reaching the goal. A run that
/// makes no progress stops, where "until it is sorted" would spin forever against a game that
/// quietly refuses.
/// </para>
/// </remarks>
internal sealed unsafe class GearsetArranger : IGearsetArranger
{
    private readonly IGearsetReader reader;
    private readonly IGameStateProbe gameState;

    public GearsetArranger(IGearsetReader reader, IGameStateProbe gameState)
    {
        this.reader = reader;
        this.gameState = gameState;
    }

    /// <inheritdoc />
    public ArrangeResult Arrange(IReadOnlyList<string> wantedOrder, Action<int>? onProgress = null)
    {
        ArgumentNullException.ThrowIfNull(wantedOrder);

        if (!gameState.IsLoggedIn)
        {
            return new ArrangeResult(ArrangeOutcome.NotLoggedIn, 0);
        }

        var module = RaptureGearsetModule.Instance();
        if (module is null)
        {
            GearbookServices.Log.Warning("Sorting the game's gearset list was asked for, but the gearset module was not available.");
            return new ArrangeResult(ArrangeOutcome.Unavailable, 0);
        }

        var applied = 0;

        // One pass per entry is the most a front-filling arrangement can need, and it is a
        // ceiling rather than a target: the loop leaves as soon as there is nothing left to move.
        for (var attempt = 0; attempt <= wantedOrder.Count; attempt++)
        {
            // Ordered here rather than trusting the reader's loop to have done it, because the
            // position a move names is a position in the game's list and nothing else.
            var current = reader.Read().OrderBy(g => g.Slot).ToList();

            var move = ListArrangement.NextMove(Keys(current), wantedOrder);
            if (move is null)
            {
                return new ArrangeResult(
                    applied == 0 ? ArrangeOutcome.NothingToDo : ArrangeOutcome.Done,
                    applied);
            }

            var from = current[move.Value.FromPosition].Slot;
            var to = current[move.Value.ToPosition].Slot;

            GearbookServices.Log.Information(
                "Moving gearset {From} to {To} while sorting the game's list, triggered by the player.",
                from,
                to);

            // The game's own argument order: where it should end up first, what is being moved
            // second. Named the other way round here because every caller thinks in terms of
            // moving something somewhere.
            module->ReassignGearsetId(to, from);
            applied++;
            onProgress?.Invoke(applied);

            // Read back and check that the position actually settled. Anything else, including
            // the game having done something reasonable that was not this, ends the run.
            var after = Keys(reader.Read());
            if (move.Value.ToPosition >= after.Count
                || !string.Equals(after[move.Value.ToPosition], wantedOrder[move.Value.ToPosition], StringComparison.Ordinal))
            {
                GearbookServices.Log.Warning(
                    "Sorting the game's gearset list stopped after {Applied} moves: position {Position} did not settle as asked.",
                    applied,
                    move.Value.ToPosition);

                return new ArrangeResult(ArrangeOutcome.Stopped, applied);
            }
        }

        // The ceiling was reached with work still outstanding, which means moves were landing
        // where they were aimed without the list converging. Reported as a stop rather than as
        // success, because it is one.
        GearbookServices.Log.Warning(
            "Sorting the game's gearset list stopped after {Applied} moves without reaching the wanted order.",
            applied);

        return new ArrangeResult(ArrangeOutcome.Stopped, applied);
    }

    private static IReadOnlyList<string> Keys(IReadOnlyList<GearsetSnapshot> gearsets) =>
        [.. gearsets.OrderBy(g => g.Slot).Select(ListArrangement.KeyFor)];
}
