using Dalamud.Plugin;
using Gearbook.Adapters;
using Gearbook.Commands;
using Gearbook.Configuration;
using Gearbook.Core.Localization;
using Gearbook.Services;
using Gearbook.UI;

namespace Gearbook;

/// <summary>
/// The plugin entry point.
/// </summary>
/// <remarks>
/// Everything this type registers, it unregisters in teardown, in reverse order, with the
/// per-frame callbacks detached first. A plugin is loaded and unloaded repeatedly inside one
/// game session, so a leak compounds instead of being cleaned up by process exit, and a hot
/// reload happens while the game is running and a window is open.
/// </remarks>
public sealed class Plugin : IDalamudPlugin
{
    private readonly ConfigurationStore configuration;
    private readonly Localizer localizer;
    private readonly GearsetReader reader;
    private readonly GameStateProbe gameState;
    private readonly GearsetEquipper equipper;
    private readonly GearsetArranger arranger;
    private readonly JobDataSource jobData;
    private readonly ArsenalBisProvider bis;
    private readonly GearbookState state;
    private readonly WindowManager windows;
    private readonly CommandHandler commands;

    private bool disposed;

    /// <summary>
    /// Builds the plugin. Services arrive through the host's injection rather than being
    /// constructed, and nothing here blocks: the work that takes time is a read of the local
    /// game data, which happens on the first framework tick rather than in this constructor.
    /// </summary>
    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        ArgumentNullException.ThrowIfNull(pluginInterface);
        pluginInterface.Create<GearbookServices>();

        configuration = ConfigurationStore.Load();

        var catalogues = LanguageCatalog.LoadAll(message =>
            GearbookServices.Log.Warning("{Message}", message));

        localizer = new Localizer(catalogues);

        reader = new GearsetReader();
        gameState = new GameStateProbe(reader);
        equipper = new GearsetEquipper(gameState);
        arranger = new GearsetArranger(reader, equipper);
        jobData = new JobDataSource();
        bis = new ArsenalBisProvider();

        state = new GearbookState(reader, equipper, arranger, gameState, jobData, bis, configuration, localizer);
        state.ApplyLanguage();

        windows = new WindowManager(state);
        commands = new CommandHandler(state, windows);

        GearbookServices.Framework.Update += OnFrameworkUpdate;
        GearbookServices.ClientState.Login += OnLogin;
        GearbookServices.ClientState.Logout += OnLogout;
        GearbookServices.ClientState.ClassJobChanged += OnClassJobChanged;
        GearbookServices.PluginInterface.LanguageChanged += OnHostLanguageChanged;

        var ui = GearbookServices.PluginInterface.UiBuilder;
        ui.Draw += windows.Draw;
        ui.OpenMainUi += windows.ToggleLibrary;
        ui.OpenConfigUi += windows.ToggleSettings;

        windows.ApplyStartupVisibility();

        GearbookServices.Log.Information("Gearbook {Version} loaded.", GearbookVersion.Current);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // The draw callback goes first. Order is not cosmetic here: it is the one that runs
        // while teardown is in progress, and anything it reaches must still be intact when it
        // does. Everything else follows in reverse order of registration.
        var ui = GearbookServices.PluginInterface.UiBuilder;
        ui.Draw -= windows.Draw;
        ui.OpenMainUi -= windows.ToggleLibrary;
        ui.OpenConfigUi -= windows.ToggleSettings;

        GearbookServices.PluginInterface.LanguageChanged -= OnHostLanguageChanged;
        GearbookServices.ClientState.ClassJobChanged -= OnClassJobChanged;
        GearbookServices.ClientState.Logout -= OnLogout;
        GearbookServices.ClientState.Login -= OnLogin;
        GearbookServices.Framework.Update -= OnFrameworkUpdate;

        commands.Dispose();
        windows.Dispose();
        state.Dispose();
        bis.Dispose();

        // Last, because a setting changed in the same frame as the unload should still reach
        // the file.
        configuration.Save();

        GearbookServices.Log.Information("Gearbook unloaded.");
    }

    private void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework framework)
    {
        if (disposed)
        {
            return;
        }

        state.OnTick();
    }

    private void OnLogin()
    {
        state.RequestRefresh();
        state.ApplyLanguage();
        windows.ApplyStartupVisibility();
    }

    private void OnLogout(int type, int code) => state.RequestRefresh();

    private void OnClassJobChanged(uint classJobId) => state.RequestRefresh();

    /// <summary>
    /// The host changed its own interface language.
    /// </summary>
    /// <remarks>
    /// Re-resolved only when the setting is on automatic, which the resolver enforces. Without
    /// that check an explicit choice would be silently overwritten the next time the player
    /// changed the host's language, and that reads as the plugin forgetting a setting rather
    /// than as a rule.
    /// </remarks>
    private void OnHostLanguageChanged(string language) => state.ApplyLanguage();
}
