namespace Gearbook.Core.Identity;

/// <summary>
/// How a saved record was matched to a gearset that exists in the game. Ordered from most
/// certain to least, which is also the order the stages run in.
/// </summary>
/// <remarks>
/// This is kept and surfaced rather than discarded because it is the only thing that can
/// explain a wrong match afterwards. Without it, a note on the wrong gearset is a mystery; with
/// it, the log says which stage made the claim.
/// </remarks>
public enum MatchStage
{
    /// <summary>Slot, job and name all agree. Nothing changed.</summary>
    Exact = 0,

    /// <summary>Job and name agree and the match was unique. The set was moved.</summary>
    Moved,

    /// <summary>Slot and job agree and the match was unique. The set was renamed.</summary>
    Renamed,

    /// <summary>Job and the equipment digest agree and the match was unique. The set was both
    /// renamed and moved.</summary>
    Fingerprint,

    /// <summary>Several sets share a job and a name and could not be told apart, so they were
    /// paired in slot order. Always accompanied by an entry in the ambiguity list.</summary>
    SlotOrderFallback,

    /// <summary>No saved record matched, so a new one was issued.</summary>
    Created,
}
