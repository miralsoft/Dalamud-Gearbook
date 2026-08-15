using System.Globalization;

namespace Gearbook.Core.Settings;

/// <summary>
/// The whole of what Gearbook stores, across every character on this installation.
/// </summary>
public sealed class GearbookSettings
{
    /// <summary>
    /// The layout version of this file. Raised when a stored value stops meaning what it used
    /// to, not when a default changes.
    /// </summary>
    public int LayoutVersion { get; set; } = SettingsMigrator.CurrentVersion;

    /// <summary>
    /// One entry per character, keyed by that character's own content id as a string.
    /// </summary>
    /// <remarks>
    /// A string key rather than the number itself, because a dictionary keyed by a 64-bit
    /// integer is written as a string by most serialisers anyway and reading it back is where
    /// the round trip goes wrong. Only the player's own characters appear here; no other
    /// player's identifier is read or stored.
    /// </remarks>
    public Dictionary<string, CharacterSettings> Characters { get; set; } = [];

    /// <summary>
    /// The settings for a character, created on first sight.
    /// </summary>
    public CharacterSettings For(ulong contentId)
    {
        var key = contentId.ToString(CultureInfo.InvariantCulture);

        if (!Characters.TryGetValue(key, out var settings))
        {
            settings = new CharacterSettings();
            settings.NormaliseRoleOrder();
            Characters[key] = settings;
        }

        return settings;
    }

    /// <summary>
    /// Whether anything is stored for a character yet. This is what tells a first installation
    /// apart from an update, which the release notes window needs and cannot get from the
    /// last-seen version: somebody updating from a build that predates that setting has also
    /// seen nothing.
    /// </summary>
    public bool Knows(ulong contentId) =>
        Characters.ContainsKey(contentId.ToString(CultureInfo.InvariantCulture));
}
