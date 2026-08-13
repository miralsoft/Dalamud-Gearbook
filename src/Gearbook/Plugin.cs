using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Gearbook;

/// <summary>
/// The plugin entry point. Everything this type registers, it unregisters in reverse order,
/// with the per-frame callbacks detached first. A plugin is loaded and unloaded repeatedly
/// inside one game session, so a leak compounds instead of being cleaned up by process exit.
/// </summary>
public sealed class Plugin : IDalamudPlugin
{
    private readonly IPluginLog log;

    /// <summary>
    /// Initialises the plugin. Services arrive through the host's injection rather than being
    /// constructed. Long initialisation belongs off this constructor, which blocks the game.
    /// </summary>
    public Plugin(IPluginLog pluginLog)
    {
        log = pluginLog;
        log.Information("Gearbook loaded.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        log.Information("Gearbook unloaded.");
    }
}
