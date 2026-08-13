using Dalamud.Game.Gui.Dtr;
using Gearbook.Core.Localization;
using Gearbook.Services;

namespace Gearbook.Adapters;

/// <summary>
/// The entry in the host's server info bar.
/// </summary>
/// <remarks>
/// This is how somebody who does not know the command finds the plugin again. Without it, a
/// player who closes every window and forgets the command has no route back at all.
/// </remarks>
internal sealed class ServerBarEntry : IDisposable
{
    private const string EntryTitle = "Gearbook";

    private readonly IDtrBarEntry? entry;

    public ServerBarEntry(GearbookState state, Action onClick)
    {
        try
        {
            entry = GearbookServices.DtrBar.Get(EntryTitle, "Gearbook");
            entry.OnClick = _ => onClick();
            entry.Tooltip = state.Loc.Get(LocKeys.DtrTooltip);
            entry.Shown = true;
        }
        catch (Exception ex)
        {
            // Losing this costs a way in, not a feature. The plugin still works from its command
            // and from the installer, so it is reported and carried on from.
            GearbookServices.Log.Warning(ex, "The server info bar entry could not be created.");
            entry = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            entry?.Remove();
        }
        catch (Exception ex)
        {
            GearbookServices.Log.Warning(ex, "The server info bar entry could not be removed.");
        }
    }
}
