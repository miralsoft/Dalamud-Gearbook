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
    /// <summary>
    /// The name the host files this entry under. Not what is displayed.
    /// </summary>
    private const string EntryTitle = "Gearbook";

    /// <summary>
    /// What is actually displayed, and it is deliberately two characters.
    /// </summary>
    /// <remarks>
    /// The server info bar is shared with the clock, the world name and every other plugin that
    /// wants a corner of it, so a word costs everybody else room. Two characters keep the entry
    /// findable while taking almost none, and the full name is in the tooltip where it costs
    /// nothing at all.
    /// </remarks>
    private const string EntryLabel = "GB";

    private readonly IDtrBarEntry? entry;

    public ServerBarEntry(GearbookState state, Action onClick)
    {
        try
        {
            entry = GearbookServices.DtrBar.Get(EntryTitle, EntryLabel);
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

    /// <summary>
    /// Shows or hides the entry, and keeps its tooltip in the current language.
    /// </summary>
    /// <remarks>
    /// Called from the framework tick rather than when the setting changes, because the setting
    /// belongs to a character and the character changes on login. Reading it every tick is one
    /// boolean and cannot fall out of step.
    /// </remarks>
    public void Update(GearbookState state)
    {
        if (entry is null)
        {
            return;
        }

        try
        {
            entry.Shown = state.Character?.ShowServerBarEntry ?? true;
            entry.Tooltip = state.Loc.Get(LocKeys.DtrTooltip);
        }
        catch (Exception ex)
        {
            GearbookServices.Log.Warning(ex, "The server info bar entry could not be updated.");
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
