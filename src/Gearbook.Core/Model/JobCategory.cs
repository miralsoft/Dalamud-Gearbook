namespace Gearbook.Core.Model;

/// <summary>
/// The coarse grouping above <see cref="JobRole"/>. Kept as its own value rather than derived
/// at every call site, because it is what the top level of the filter sidebar groups by and a
/// derivation repeated in four places is a rule in four places.
/// </summary>
public enum JobCategory
{
    /// <summary>The category could not be determined from the job table.</summary>
    Unknown = 0,

    Combat,
    Crafting,
    Gathering,
}
