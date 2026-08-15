using Gearbook.Core.Filtering;
using Gearbook.Core.Localization;
using Gearbook.Core.Model;
using Gearbook.Core.Views;

namespace Gearbook.Core.Settings;

/// <summary>
/// Everything Gearbook remembers about one character.
/// </summary>
/// <remarks>
/// Everything, including the settings that look global such as the language and the filter
/// level (GB-02). Gearsets belong to a character, so the things hanging off them do too, and a
/// player running a crafter on one character and a raider on another would otherwise have to
/// change the same setting twice a session.
/// </remarks>
public sealed class CharacterSettings
{
    /// <summary>The saved records, one per gearset this plugin has ever seen on this character.</summary>
    public List<GearsetRecordData> Gearsets { get; set; } = [];

    /// <summary>
    /// The next identifier to issue. Held here rather than derived from the highest id in use,
    /// because deleting the highest record would otherwise let the next one reuse its number and
    /// inherit whatever still referred to it.
    /// </summary>
    public int NextGearsetId { get; set; } = 1;

    /// <summary>The player's saved views.</summary>
    public List<SavedView> Views { get; set; } = [];

    /// <summary>Which view is active, by name, or empty for none.</summary>
    public string ActiveViewName { get; set; } = string.Empty;

    /// <summary>The filter currently in the panel, which is not necessarily a saved view.</summary>
    public FilterSpec CurrentFilter { get; set; } = new();

    /// <summary>How much of the filter panel to show.</summary>
    public FilterLevel FilterLevel { get; set; } = FilterLevel.Simple;

    /// <summary>
    /// The order the roles are grouped in when the list is sorted by role.
    /// </summary>
    /// <remarks>
    /// The default is the order the game's own character window uses, so the grouping matches
    /// what a player already knows before they touch it. A role missing from this list, which is
    /// what an older stored order looks like after a new role is added, sorts to the end rather
    /// than to the front.
    /// </remarks>
    /// <remarks>
    /// Deliberately empty rather than pre-filled with the default. A serialiser reading a list
    /// into a property that already holds items appends to it rather than replacing it, so a
    /// pre-filled default grew by eight entries on every single load. It reached eight copies of
    /// every role before anybody saw the settings window. <see cref="NormaliseRoleOrder"/> is
    /// what puts the default in, and it is also what repairs a file this already happened to.
    /// </remarks>
    public List<JobRole> RoleOrder { get; set; } = [];

    /// <summary>The order the roles ship in.</summary>
    public static IReadOnlyList<JobRole> DefaultRoleOrder { get; } =
    [
        JobRole.Tank,
        JobRole.Healer,
        JobRole.MeleeDps,
        JobRole.PhysicalRangedDps,
        JobRole.MagicalRangedDps,
        JobRole.Crafter,
        JobRole.Gatherer,
        JobRole.Unknown,
    ];

    /// <summary>
    /// The language, or <see cref="LanguageResolver.Automatic"/> to follow the host. The
    /// reserved code is a value of the same type as a language code rather than a separate
    /// flag, so this stays one field and the resolver has one input.
    /// </summary>
    public string Language { get; set; } = LanguageResolver.Automatic;

    public BarSettings Bar { get; set; } = new();

    public LibrarySettings Library { get; set; } = new();

    /// <summary>The newest release notes version this character has seen, or empty.</summary>
    public string LastSeenNewsVersion { get; set; } = string.Empty;

    /// <summary>Open the release notes once after an update.</summary>
    public bool OpenNewsAfterUpdate { get; set; } = true;

    /// <summary>
    /// Makes the role order hold every role exactly once, keeping the order it already
    /// describes.
    /// </summary>
    /// <remarks>
    /// Three jobs at once, and all three are needed. It fills an empty list with the default,
    /// which is how a new character gets a sensible order. It removes duplicates, which repairs
    /// a file that grew every time it was loaded. And it appends any role the list does not
    /// mention, which is what an order saved before a new role existed looks like.
    /// </remarks>
    public void NormaliseRoleOrder()
    {
        var normalised = new List<JobRole>(DefaultRoleOrder.Count);

        foreach (var role in RoleOrder)
        {
            if (!normalised.Contains(role))
            {
                normalised.Add(role);
            }
        }

        foreach (var role in DefaultRoleOrder)
        {
            if (!normalised.Contains(role))
            {
                normalised.Add(role);
            }
        }

        RoleOrder = normalised;
    }

    /// <summary>The saved records as model objects.</summary>
    public IReadOnlyList<GearsetRecord> ToRecords() =>
        [.. Gearsets.Select(g => g.ToRecord())];

    /// <summary>Replaces the saved records from the model objects.</summary>
    public void SetRecords(IEnumerable<GearsetRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        Gearsets = [.. records.Select(GearsetRecordData.FromRecord)];
    }
}
