using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Gearbook.Services;

/// <summary>
/// The host services this plugin uses, obtained through the framework's injection rather than
/// constructed.
/// </summary>
/// <remarks>
/// One holder rather than passing eight parameters through every constructor. The properties are
/// filled by the host before the plugin's own constructor runs, which is why they are static and
/// why the null-forgiving initialisers are correct rather than lazy.
/// </remarks>
internal sealed class GearbookServices
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService] internal static ICommandManager Commands { get; private set; } = null!;

    [PluginService] internal static IClientState ClientState { get; private set; } = null!;

    [PluginService] internal static ICondition Condition { get; private set; } = null!;

    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;

    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    [PluginService] internal static ITextureProvider Textures { get; private set; } = null!;


    /// <summary>
    /// Whether a cutscene is running, as the host reports it.
    /// </summary>
    /// <remarks>
    /// Read through here rather than from the condition flags alone. The host tracks this for
    /// its own interface hiding and knows about cases the flags do not cover cleanly, so the two
    /// together are more reliable than either.
    /// </remarks>
    internal static bool UiBuilderCutsceneActive => PluginInterface.UiBuilder.CutsceneActive;
}
