using Dalamud.Interface.Windowing;
using Gearbook.Core.News;
using Gearbook.Services;

namespace Gearbook.UI;

/// <summary>
/// Owns the windows and the toggles that reach them.
/// </summary>
/// <remarks>
/// Every entry point calls the same toggle rather than a variant of it: the text command, the
/// installer's own open button and gear, the server info bar entry, and the cross-links between
/// the windows. Three entry points that drift apart is three behaviours to explain.
/// </remarks>
internal sealed class WindowManager : IDisposable
{
    private readonly WindowSystem windows = new("Gearbook");
    private readonly GearbookState state;
    private readonly BarWindow bar;
    private readonly LibraryWindow library;
    private readonly SettingsWindow settings;
    private readonly ReleaseNotesWindow news;
    private readonly ArrangeWindow arrange;

    private bool disposed;
    private bool checkedForUpdate;

    public WindowManager(GearbookState state)
    {
        this.state = state;

        // Every window can reach every other from its title bar, which is where the host puts
        // its own controls and therefore where a player already looks. The toggles are the same
        // ones the commands and the installer's buttons call, not variants of them.
        library = new LibraryWindow(state, ToggleBar, ToggleSettings, ToggleNews, ToggleArrange);
        settings = new SettingsWindow(state, ToggleLibrary, ToggleNews);
        news = new ReleaseNotesWindow(state, ToggleLibrary, ToggleSettings);
        bar = new BarWindow(state, ToggleLibrary, ToggleSettings);
        arrange = new ArrangeWindow(state, ToggleLibrary, ToggleSettings);

        windows.AddWindow(bar);
        windows.AddWindow(library);
        windows.AddWindow(settings);
        windows.AddWindow(news);
        windows.AddWindow(arrange);
    }

    /// <summary>Shows or hides the library.</summary>
    public void ToggleLibrary() => library.Toggle();

    /// <summary>Shows or hides the settings.</summary>
    public void ToggleSettings() => settings.Toggle();

    /// <summary>Shows or hides the release notes.</summary>
    public void ToggleNews() => news.Toggle();

    /// <summary>Shows or hides the window that sorts the game's own list.</summary>
    public void ToggleArrange() => arrange.Toggle();

    /// <summary>Shows or hides the quick-switch bar.</summary>
    public void ToggleBar() => bar.IsOpen = !bar.IsOpen;

    /// <summary>Opens the bar if the player asked for it to be there on load.</summary>
    public void ApplyStartupVisibility()
    {
        var character = state.Character;
        bar.IsOpen = character?.Bar.ShowOnStart ?? true;
    }

    /// <summary>
    /// The per-frame draw. Stops as soon as teardown begins, because a hot reload happens while
    /// the game is running and a window is open.
    /// </summary>
    public void Draw()
    {
        if (disposed)
        {
            return;
        }

        OpenNewsIfThisIsAnUpdate();
        windows.Draw();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        disposed = true;
        windows.RemoveAllWindows();
    }

    /// <summary>
    /// Opens the notes once after an update, and never on a first installation.
    /// </summary>
    /// <remarks>
    /// The check runs once the character's settings are known rather than at load, because a
    /// first installation is told apart from an update by whether a configuration existed at
    /// all. The last-seen version cannot make that distinction: somebody updating from a build
    /// that predates it has also seen nothing.
    /// </remarks>
    private void OpenNewsIfThisIsAnUpdate()
    {
        if (checkedForUpdate)
        {
            return;
        }

        var character = state.Character;
        if (character is null)
        {
            return;
        }

        checkedForUpdate = true;

        var shouldOpen = ReleaseNotesLoader.ShouldOpenAutomatically(
            state.IsFirstSightOfCharacter,
            character.LastSeenNewsVersion,
            GearbookVersion.Current,
            character.OpenNewsAfterUpdate);

        if (shouldOpen)
        {
            news.IsOpen = true;
            return;
        }

        // A first installation is marked as seen rather than left alone. Leaving it would make
        // the very next start look like an update and the notes would appear then, which is
        // worse than either alternative because it looks random.
        if (state.IsFirstSightOfCharacter
            && !string.Equals(character.LastSeenNewsVersion, GearbookVersion.Current, StringComparison.Ordinal))
        {
            character.LastSeenNewsVersion = GearbookVersion.Current;
            GearbookServices.Log.Debug("First run on this character; the release notes are marked as seen.");
            state.Save();
        }
    }
}
