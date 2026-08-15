using Dalamud.Configuration;
using Gearbook.Core.Settings;
using Gearbook.Services;

namespace Gearbook.Configuration;

/// <summary>
/// The thin wrapper the host's configuration store needs, around the settings model that lives
/// in the core.
/// </summary>
/// <remarks>
/// Two version numbers appear here and they mean different things. <see cref="Version"/> belongs
/// to the host and is part of its own interface. <see cref="GearbookSettings.LayoutVersion"/> is
/// this plugin's, and it is the one the migration reads. Keeping them separate is deliberate:
/// the host's number cannot be raised without the host's permission, and tying a migration to
/// somebody else's counter is how a migration ends up running at the wrong moment.
/// </remarks>
internal sealed class ConfigurationStore : IPluginConfiguration
{
    /// <inheritdoc />
    public int Version { get; set; } = 1;

    /// <summary>Everything this plugin remembers.</summary>
    public GearbookSettings Settings { get; set; } = new();

    /// <summary>
    /// Loads the configuration and brings it forward if it was written by an older build.
    /// </summary>
    public static ConfigurationStore Load()
    {
        ConfigurationStore store;

        try
        {
            store = GearbookServices.PluginInterface.GetPluginConfig() as ConfigurationStore
                    ?? new ConfigurationStore();
        }
        catch (Exception ex)
        {
            // A configuration that cannot be read is a bad day, but starting with defaults is
            // better than not loading at all, and the old file is left on disk rather than
            // overwritten until something is actually saved.
            GearbookServices.Log.Error(
                ex,
                "The configuration could not be read and defaults are being used. "
                    + "The existing file has not been changed.");

            store = new ConfigurationStore();
        }

        store.Settings ??= new GearbookSettings();

        var migrated = SettingsMigrator.Migrate(store.Settings, message =>
            GearbookServices.Log.Information("{Message}", message));

        // Repairs whatever the file actually holds rather than trusting it. This is not a
        // migration step: it has to run on every load, because the thing it repairs was caused
        // by loading, and a file already at the current layout version can still be wrong.
        foreach (var character in store.Settings.Characters.Values)
        {
            character?.NormaliseRoleOrder();
        }

        if (migrated && store.Settings.LayoutVersion == SettingsMigrator.CurrentVersion)
        {
            // Saved straight after migrating so the file on disk matches what is in memory. A
            // migration that only exists in memory runs again on every start, and the log then
            // says it migrated something that was never written.
            store.Save();
        }

        return store;
    }

    /// <summary>Writes the configuration back.</summary>
    public void Save()
    {
        try
        {
            GearbookServices.PluginInterface.SavePluginConfig(this);
        }
        catch (Exception ex)
        {
            GearbookServices.Log.Error(ex, "The configuration could not be saved.");
        }
    }
}
