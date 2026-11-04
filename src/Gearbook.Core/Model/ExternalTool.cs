namespace Gearbook.Core.Model;

/// <summary>
/// Another plugin Gearbook can offer a shortcut to.
/// </summary>
/// <remarks>
/// Here rather than in the plugin project because a setting stores it, and the settings are core.
/// It is a list of names and nothing else: which plugin each one is, whether it is installed and
/// how to open it all stay on the platform side, where they need the host to answer them.
///
/// The values are stored in the settings (the hidden shortcuts), so a new tool is appended and no
/// existing value ever moves: a moved number would hide or show the wrong shortcut for everybody
/// who had already chosen.
/// </remarks>
public enum ExternalTool
{
    /// <summary>Artisan, the crafting plugin.</summary>
    Artisan = 0,

    /// <summary>Ice's Cosmic Exploration.</summary>
    Cosmic = 1,

    /// <summary>Eorzea Arsenal, which compares gear against best in slot.</summary>
    Arsenal = 2,

    /// <summary>GatherBuddy Reborn, the gathering and fishing plugin.</summary>
    GatherBuddy = 3,
}
