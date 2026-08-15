namespace Gearbook.Core.Settings;

/// <summary>
/// How the quick-switch bar looks and behaves, for one character.
/// </summary>
/// <remarks>
/// Every default here is chosen so that somebody who never opens the settings still gets a
/// working bar. Settings tune a product that works; they do not make a broken one work.
/// </remarks>
public sealed class BarSettings
{
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

    /// <summary>Show the bar when the plugin loads.</summary>
    public bool ShowOnStart { get; set; } = true;
}
