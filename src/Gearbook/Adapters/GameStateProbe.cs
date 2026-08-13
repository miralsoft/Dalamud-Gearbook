using Dalamud.Game.ClientState.Conditions;
using Gearbook.Services;

namespace Gearbook.Adapters;

/// <summary>
/// The parts of the game's state that decide whether a gearset can be changed and whether the
/// bar should be on screen.
/// </summary>
internal interface IGameStateProbe
{
    bool IsLoggedIn { get; }

    bool IsInCombat { get; }

    bool IsInCutscene { get; }

    /// <summary>
    /// Busy with something the game will not interrupt: an event, a summoning bell, crafting,
    /// gathering, fishing, or a zone change.
    /// </summary>
    bool IsOccupied { get; }

    /// <summary>The gearset currently worn, by the game's own number, or null.</summary>
    int? CurrentGearsetSlot { get; }
}

/// <summary>
/// Reads the host's condition flags.
/// </summary>
internal sealed class GameStateProbe : IGameStateProbe
{
    /// <summary>
    /// The states that mean the game will not accept a gearset change.
    /// </summary>
    /// <remarks>
    /// A list rather than a single flag because the game has many ways of being busy and they
    /// are not related to each other. Being wrong in the permissive direction here means a
    /// change the game refuses, which is reported and harmless; being wrong in the restrictive
    /// direction means a tile greyed out for no reason, which looks broken. So this errs
    /// towards permissive and lets the game have the final say.
    /// </remarks>
    private static readonly ConditionFlag[] OccupiedFlags =
    [
        ConditionFlag.Occupied,
        ConditionFlag.OccupiedInEvent,
        ConditionFlag.OccupiedInQuestEvent,
        ConditionFlag.OccupiedSummoningBell,
        ConditionFlag.Crafting,
        ConditionFlag.ExecutingCraftingAction,
        ConditionFlag.Gathering,
        ConditionFlag.ExecutingGatheringAction,
        ConditionFlag.Fishing,
        ConditionFlag.BetweenAreas,
        ConditionFlag.BetweenAreas51,
        ConditionFlag.Casting,
    ];

    private static readonly ConditionFlag[] CutsceneFlags =
    [
        ConditionFlag.WatchingCutscene,
        ConditionFlag.WatchingCutscene78,
        ConditionFlag.OccupiedInCutSceneEvent,
    ];

    private readonly IGearsetReader reader;

    public GameStateProbe(IGearsetReader reader)
    {
        this.reader = reader;
    }

    /// <inheritdoc />
    public bool IsLoggedIn => GearbookServices.ClientState.IsLoggedIn;

    /// <inheritdoc />
    public bool IsInCombat => GearbookServices.Condition[ConditionFlag.InCombat];

    /// <inheritdoc />
    public bool IsInCutscene =>
        GearbookServices.UiBuilderCutsceneActive || GearbookServices.Condition.Any(CutsceneFlags);

    /// <inheritdoc />
    public bool IsOccupied => GearbookServices.Condition.Any(OccupiedFlags);

    /// <inheritdoc />
    public int? CurrentGearsetSlot => reader.CurrentSlot();
}
