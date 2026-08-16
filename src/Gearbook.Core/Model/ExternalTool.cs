namespace Gearbook.Core.Model;

/// <summary>
/// Another plugin Gearbook can offer a shortcut to.
/// </summary>
/// <remarks>
/// Here rather than in the plugin project because a setting stores it, and the settings are core.
/// It is a list of names and nothing else: which plugin each one is, whether it is installed and
/// how to open it all stay on the platform side, where they need the host to answer them.
/// </remarks>
public enum ExternalTool
{
    /// <summary>Artisan, the crafting plugin.</summary>
    Artisan = 0,

    /// <summary>Ice's Cosmic Exploration.</summary>
    Cosmic,

    /// <summary>Eorzea Arsenal, which compares gear against best in slot.</summary>
    Arsenal,
}
