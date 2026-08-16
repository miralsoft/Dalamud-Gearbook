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
    private readonly IGearsetEquipper equipper;

    public GearsetArranger(IGearsetReader reader, IGearsetEquipper equipper)
    {
        this.reader = reader;
        this.equipper = equipper;
    }

    /// <inheritdoc />
    public ArrangeResult Arrange(IReadOnlyList<string> wantedOrder, Action<int>? onProgress = null)
    {
        ArgumentNullException.ThrowIfNull(wantedOrder);

        var allowed = CanArrange();
        if (allowed != ArrangeOutcome.Ready)
        {
            GearbookServices.Log.Debug("Sorting the game's gearset list was skipped: {Reason}.", allowed);
            return new ArrangeResult(allowed, 0);
        }

        if (RaptureGearsetModule.Instance() is null)
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

            // Asked again before every move, not only at the start. A run of thirty moves takes
            // long enough for somebody to begin a craft in the middle of it, and the game answers
            // a reordering it will not take by doing nothing at all rather than by refusing. Read
            // back, silence is indistinguishable from a move that landed somewhere unexpected, so
            // without this the run would stop and give the wrong reason for stopping.
            var stillAllowed = CanArrange();
            if (stillAllowed != ArrangeOutcome.Ready)
            {
                GearbookServices.Log.Information(
                    "Sorting the game's gearset list stopped after {Applied} moves: {Reason}.",
                    applied,
                    stillAllowed);

                return new ArrangeResult(stillAllowed, applied);
            }

            // Fetched again for every single move rather than once before the loop. This runs up
            // to one pass per gearset with a read of game memory between each, and an instance
            // accessor may answer null at any point in that: a player can reach the title screen
            // in the middle of a sort. A pointer that was good thirty moves ago is not a pointer.
            var module = RaptureGearsetModule.Instance();
            if (module is null)
            {
                GearbookServices.Log.Warning(
                    "Sorting the game's gearset list stopped after {Applied} moves: the gearset module went away.",
                    applied);

                return new ArrangeResult(ArrangeOutcome.Stopped, applied);
            }

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

    /// <inheritdoc />
    /// <remarks>
    /// The same question the equip gate asks, asked through it rather than answered again here.
    /// If the game will not let the player change gear at this moment, it will not accept the
    /// list being rearranged either, and keeping two lists of conditions would guarantee that one
    /// day one of them is missing the condition that mattered.
    ///
    /// Every kind of busy collapses to one answer, because the player does not need to be told
    /// which: the remedy is the same in all of them, which is to finish what they are doing.
    /// </remarks>
    public ArrangeOutcome CanArrange() => equipper.CheckCanChangeGear() switch
    {
        EquipOutcome.Sent => ArrangeOutcome.Ready,
        EquipOutcome.NotLoggedIn => ArrangeOutcome.NotLoggedIn,
        _ => ArrangeOutcome.Busy,
    };

    private static IReadOnlyList<string> Keys(IReadOnlyList<GearsetSnapshot> gearsets) =>
        [.. gearsets.OrderBy(g => g.Slot).Select(ListArrangement.KeyFor)];
}
