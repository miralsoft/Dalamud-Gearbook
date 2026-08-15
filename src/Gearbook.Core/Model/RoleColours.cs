using System.Numerics;

namespace Gearbook.Core.Model;

/// <summary>
/// The colour the game gives each role.
/// </summary>
/// <remarks>
/// <para>
/// Four of the five are read off the game's own `ROLE BASE` tiles, the empty framed colours it
/// stacks its role symbols onto, rather than picked to taste. Blue for the tank, green for the
/// healer and red for damage are what a player has already learned from the party list and the
/// duty finder, so borrowing them costs nothing to explain.
/// </para>
/// <para>
/// Crafting is the one that is chosen rather than found, and it is called out here rather than
/// quietly slipped in with the others. The game's own ground for a crafter is the dark one, which
/// is not a colour but the absence of one, and an absence cannot be a signal: as a highlight it
/// would say nothing at all. Violet is the nearest hue no role has already spoken for, and it is
/// what the owner had seen used for crafting elsewhere.
/// </para>
/// <para>
/// Written down rather than sampled from the texture at runtime. Reading a pixel out of a game
/// picture every frame to learn a number that has not changed since the tile was drawn would be
/// work in the wrong place, and the numbers are as much a fact about the game data as the icon
/// numbers beside them.
/// </para>
/// </remarks>
public static class RoleColours
{
    // The interiors of 62571, 62572, 62573 and 62575, measured just inside the frame.
    private static readonly Vector4 Tank = From(44, 62, 124);
    private static readonly Vector4 Healer = From(50, 79, 40);
    private static readonly Vector4 Damage = From(83, 49, 49);
    private static readonly Vector4 Land = From(124, 122, 44);

    // Chosen, not measured. See the remark above.
    private static readonly Vector4 Hand = From(96, 72, 148);

    /// <summary>The role's colour, or null where the game gives it none.</summary>
    /// <remarks>
    /// Null rather than a grey, so a caller can leave the host's own colour in place instead of
    /// painting something that looks deliberate over a job the job table does not describe.
    /// </remarks>
    public static Vector4? For(JobRole role) => role switch
    {
        JobRole.Tank => Tank,
        JobRole.Healer => Healer,
        JobRole.MeleeDps or JobRole.PhysicalRangedDps or JobRole.MagicalRangedDps => Damage,
        JobRole.Crafter => Hand,
        JobRole.Gatherer => Land,
        _ => null,
    };

    /// <summary>
    /// The same hue, turned up until it can carry a signal.
    /// </summary>
    /// <param name="colour">A role's colour.</param>
    /// <remarks>
    /// <para>
    /// The game's tiles are grounds for a silver tool to sit on, so they are dark and muted by
    /// design. Used as they are over a job icon they read as a smudge, and the meaning goes with
    /// the saturation.
    /// </para>
    /// <para>
    /// Mixing them towards white was the first attempt and it was the wrong operation: it does
    /// brighten, but it also drains the colour, so a blue arrives as a pale grey-blue and the
    /// brighter it gets the less it says. This scales instead, so the brightest channel reaches
    /// full, and then squares each one, which widens the gap between them. Brightness and
    /// saturation both go up and the hue is untouched, which is the only part doing any work.
    /// </para>
    /// </remarks>
    public static Vector4 Vivid(Vector4 colour)
    {
        var peak = Math.Max(colour.X, Math.Max(colour.Y, colour.Z));
        if (peak <= 0f)
        {
            return colour;
        }

        var scale = 1f / peak;

        return new Vector4(
            colour.X * scale * colour.X * scale,
            colour.Y * scale * colour.Y * scale,
            colour.Z * scale * colour.Z * scale,
            colour.W);
    }

    /// <summary>The same colour at a different opacity.</summary>
    public static Vector4 WithAlpha(Vector4 colour, float alpha) =>
        new(colour.X, colour.Y, colour.Z, alpha);

    private static Vector4 From(byte r, byte g, byte b) =>
        new(r / 255f, g / 255f, b / 255f, 1f);

    /// <summary>The three roles that share damage's colour, for a caller that wants to say so.</summary>
    /// <remarks>
    /// The game gives melee, physical ranged and magical ranged one colour between them and three
    /// separate symbols. Both halves of that are kept: the colour groups them, the symbol tells
    /// them apart, and inventing two more colours would disagree with every other window the
    /// player has seen.
    /// </remarks>
    public static bool SharesDamageColour(JobRole role) =>
        role is JobRole.MeleeDps or JobRole.PhysicalRangedDps or JobRole.MagicalRangedDps;
}
