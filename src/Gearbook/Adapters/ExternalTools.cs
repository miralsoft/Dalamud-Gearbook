using Gearbook.Services;

namespace Gearbook.Adapters;

/// <summary>Another plugin Gearbook can put a shortcut to on the bar.</summary>
internal enum ExternalTool
{
    /// <summary>Artisan, the crafting plugin.</summary>
    Artisan = 0,

    /// <summary>Ice's Cosmic Exploration.</summary>
    Cosmic,
}

/// <summary>
/// Shortcuts to other plugins' own windows.
/// </summary>
/// <remarks>
/// <para>
/// Through the host's own <c>OpenMainUi</c> and nothing else. Dalamud publishes the installed
/// plugins and lets one open another's main window, which is the supported way and the only one
/// used here: no text command is typed on the player's behalf, nothing is sent to the game, and
/// the other plugin decides what its own button does.
/// </para>
/// <para>
/// A tool is offered only when it is installed, loaded, and says it has a main window to open.
/// All three, because a plugin that is installed but disabled would give a shortcut that does
/// nothing, and one without a main window would give a shortcut with nothing to show.
/// </para>
/// </remarks>
internal interface IExternalTools
{
    /// <summary>True when this tool could be opened right now.</summary>
    bool IsAvailable(ExternalTool tool);

    /// <summary>The name the other plugin gives itself, for a label nobody has to translate.</summary>
    string NameOf(ExternalTool tool);

    /// <summary>Opens the tool's own main window.</summary>
    void Open(ExternalTool tool);
}

/// <inheritdoc />
internal sealed class ExternalTools : IExternalTools
{
    /// <summary>
    /// The internal names, which are what the host keys on and are not the display names.
    /// </summary>
    /// <remarks>
    /// Read off the installed manifests rather than guessed: Ice's Cosmic Exploration calls
    /// itself `ICE` internally and shows a much longer name. Keying on the display name would
    /// have missed it, and would break the moment either plugin was translated.
    /// </remarks>
    private static readonly Dictionary<ExternalTool, string> InternalNames = new()
    {
        [ExternalTool.Artisan] = "Artisan",
        [ExternalTool.Cosmic] = "ICE",
    };

    /// <inheritdoc />
    public bool IsAvailable(ExternalTool tool) => Find(tool) is not null;

    /// <inheritdoc />
    public string NameOf(ExternalTool tool) =>
        Find(tool)?.Name ?? InternalNames[tool];

    /// <inheritdoc />
    public void Open(ExternalTool tool)
    {
        var plugin = Find(tool);
        if (plugin is null)
        {
            // Between the frame that drew the tile and the frame that handled the click, the
            // player can have disabled the plugin from the installer. Saying so beats throwing.
            GearbookServices.Log.Information(
                "Opening {Tool} was asked for, but it is no longer available.",
                InternalNames[tool]);

            return;
        }

        GearbookServices.Log.Information("Opening {Tool}, asked for from the bar.", plugin.Name);
        plugin.OpenMainUi();
    }

    private static Dalamud.Plugin.IExposedPlugin? Find(ExternalTool tool)
    {
        var wanted = InternalNames[tool];

        foreach (var plugin in GearbookServices.PluginInterface.InstalledPlugins)
        {
            if (string.Equals(plugin.InternalName, wanted, StringComparison.Ordinal)
                && plugin.IsLoaded
                && plugin.HasMainUi)
            {
                return plugin;
            }
        }

        return null;
    }
}
