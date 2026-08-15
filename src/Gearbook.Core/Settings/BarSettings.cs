using Gearbook.Core.Model;
using Gearbook.Core.Sorting;

namespace Gearbook.Core.Settings;

/// <summary>What the quick-switch bar holds.</summary>
public enum BarContents
{
    /// <summary>
    /// The favourites, which is the default. A bar is worth having because it is short enough to
    /// take in at a glance, and a player with thirty gearsets who puts all of them on it has
    /// rebuilt the long list they installed this to avoid.
    /// </summary>
    Favourites = 0,

    /// <summary>
    /// Every gearset. For somebody who would rather not mark anything and is content to read the
    /// icons. The favourites keep their arrangement at the front and the rest follow.
    /// </summary>
    All,
}

/// <summary>
/// How the quick-switch bar looks and behaves, for one character.
/// </summary>
/// <remarks>
/// Every default here is chosen so that somebody who never opens the settings still gets a
/// working bar. Settings tune a product that works; they do not make a broken one work.
/// </remarks>
public sealed class BarSettings
{
    /// <summary>
    /// Whether the bar holds the favourites or everything.
    /// </summary>
    /// <remarks>
    /// Retired in favour of <see cref="ViewKind"/>, which says the same thing and more. Kept as
    /// a member so that a configuration written by the version that had it still loads and can
    /// be read by the migration to layout version 4, rather than failing or silently losing the
    /// choice. Nothing writes it any more.
    /// </remarks>
    [Obsolete("Superseded by ViewKind. Only the migration to layout version 4 reads it.")]
    public BarContents Contents { get; set; } = BarContents.Favourites;

    /// <summary>
    /// What the bar is showing: the favourites, everything, one role, one category, or one tag.
    /// </summary>
    /// <remarks>
    /// Favourites by default, because that is what makes it a bar rather than a second copy of
    /// the list. The switcher on the bar itself changes it, so the choice is made where its
    /// effect is visible instead of two windows away.
    /// </remarks>
    public BarViewKind ViewKind { get; set; } = BarViewKind.Favourites;

    /// <summary>The role shown when the view is a role.</summary>
    public JobRole ViewRole { get; set; } = JobRole.Tank;

    /// <summary>The category shown when the view is a category.</summary>
    public JobCategory ViewCategory { get; set; } = JobCategory.Combat;

    /// <summary>
    /// The tag shown when the view is a tag. A tag that no gearset carries any more shows an
    /// empty bar rather than falling back to something else, because a bar that quietly shows
    /// something other than what its own switcher says is worse than an empty one.
    /// </summary>
    public string ViewTag { get; set; } = string.Empty;

    /// <summary>
    /// How the bar is ordered, or null for the arrangement the player made by hand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Null by default, and null is not an oversight. A bar is a click target, and the value of
    /// a click target is that the third icon is always in the third place: the hand learns the
    /// position and stops reading the picture. Any rule that reorders the bar when something
    /// changes throws that away, which is why the game's own hotbars do not sort themselves
    /// either.
    /// </para>
    /// <para>
    /// A rule is offered anyway, because arranging thirty icons by hand is work somebody may not
    /// want to do, and reading a bar grouped by role is a perfectly good way to use it. The
    /// choice is the player's; the default is the one that protects what they have learned.
    /// </para>
    /// </remarks>
    public Filtering.GearsetSortOrder? Sort { get; set; }

    /// <summary>Icons per row. One column gives a vertical bar, a large number a horizontal one.</summary>
    public int Columns { get; set; } = 6;

    /// <summary>Icon edge length in pixels before the interface scale is applied.</summary>
    /// <remarks>
    /// Thirty rather than the forty this shipped with first. Forty was chosen before anybody had
    /// seen the bar in a real client, and against the game's own hotbars it is oversized. The
    /// migration to layout version 2 corrects it for anybody still sitting on the old default,
    /// and leaves it alone for anybody who moved the slider.
    /// </remarks>
    public float IconSize { get; set; } = DefaultIconSize;

    /// <summary>The shipped icon size, named because the migration compares against it.</summary>
    internal const float DefaultIconSize = 30f;

    /// <summary>What <see cref="IconSize"/> shipped as before layout version 2.</summary>
    internal const float PreviousDefaultIconSize = 40f;

    /// <summary>Spacing between icons in pixels.</summary>
    public float IconSpacing { get; set; } = 4f;

    /// <summary>Draw the item level in the corner of each icon.</summary>
    public bool ShowItemLevel { get; set; } = true;

    /// <summary>Mark favourites on the icon.</summary>
    public bool ShowFavourite { get; set; } = true;

    /// <summary>Outline the gearset currently worn.</summary>
    public bool HighlightActive { get; set; } = true;

    /// <summary>
    /// Hide while in combat. Off by default: the bar is small and somebody who put it on screen
    /// probably wants it there, so the quieter default is the one that changes less.
    /// </summary>
    public bool HideInCombat { get; set; }

    /// <summary>
    /// Hide during cutscenes. On by default, because nothing should sit over a cutscene, and
    /// this is the one case where the player is definitely not switching gear.
    /// </summary>
    public bool HideInCutscene { get; set; } = true;

    /// <summary>
    /// Locked bars have no title bar and cannot be dragged. Off by default so the bar can be
    /// placed at all on the first run; a locked bar that has never been positioned would sit
    /// wherever it happened to open with no way to move it.
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Show the view switcher as the first tile. On by default, because a control nobody can
    /// see is a control nobody uses, and off for anybody who has settled on one view and wants
    /// the space back.
    /// </summary>
    public bool ShowViewSwitcher { get; set; } = true;

    /// <summary>Show the bar when the plugin loads.</summary>
    public bool ShowOnStart { get; set; } = true;
}
