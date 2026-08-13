using Gearbook.Core.Model;

namespace Gearbook.Core.Identity;

/// <summary>
/// A gearset that exists in the game, together with what this plugin knows about it and how
/// the two were connected.
/// </summary>
/// <param name="Record">The saved record, with its "last known" values already brought up to
/// date. What the player owns is untouched.</param>
/// <param name="Gearset">The set as it exists in the game right now.</param>
/// <param name="Stage">Which stage of the matching made the connection.</param>
public sealed record ReconciledGearset(
    GearsetRecord Record,
    GearsetSnapshot Gearset,
    MatchStage Stage);
