using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Gearbook.Core.Localization;
using Gearbook.Core.News;
using Gearbook.Core.Settings;

namespace Gearbook.UI;

/// <summary>
/// Which version is running, read from the assembly rather than written down a second time.
/// </summary>
internal static class GearbookVersion
{
    /// <summary>The version of the running build.</summary>
    public static string Current { get; } =
        typeof(GearbookVersion).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}

/// <summary>
/// Whether there is anything unread, asked from more than one window.
/// </summary>
internal static class ReleaseNotesState
{
    /// <summary>
    /// True when the running version is newer than the newest the player has opened.
    /// </summary>
    /// <remarks>
    /// The same comparison that decides whether the window opens by itself, deliberately, rather
    /// than a second flag. Two sources for one fact drift apart and the one nobody is looking at
    /// is the one that goes wrong.
    /// </remarks>
    public static bool HasUnread(CharacterSettings character) =>
        ReleaseNotesLoader.HasUnread(character.LastSeenNewsVersion, GearbookVersion.Current);
}

/// <summary>
/// The "what is new" window.
/// </summary>
internal sealed class ReleaseNotesWindow : Window
{
    private const string WindowId = "Gearbook: what is new###GearbookNews";

    private readonly GearbookState state;

    public ReleaseNotesWindow(GearbookState state)
        : base(WindowId)
    {
        this.state = state;

        Size = new Vector2(520, 420);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    /// <inheritdoc />
    public override void OnOpen()
    {
        // Marked as seen when the window opens, not when it closes. A glance counts, and tying
        // it to closing means the window reopens for somebody who read it and then reloaded.
        var character = state.Character;
        if (character is null)
        {
            return;
        }

        if (!string.Equals(character.LastSeenNewsVersion, GearbookVersion.Current, StringComparison.Ordinal))
        {
            character.LastSeenNewsVersion = GearbookVersion.Current;
            state.Save();
        }
    }

    /// <inheritdoc />
    public override void PreDraw() =>
        WindowName = $"{state.Loc.Get(LocKeys.WindowNewsTitle)}###GearbookNews";

    /// <inheritdoc />
    public override void Draw()
    {
        var loc = state.Loc;
        var document = ReleaseNotesLoader.Load(loc.ActiveCode, message =>
            Services.GearbookServices.Log.Warning("{Message}", message));

        var versions = document.Newest();
        if (versions.Count == 0)
        {
            UiTheme.Muted(loc.Get(LocKeys.NewsEmpty));
            return;
        }

        foreach (var version in versions)
        {
            using var id = ImRaii.PushId(version.Version);

            ImGui.TextUnformatted(version.Version);
            if (!string.IsNullOrWhiteSpace(version.Date))
            {
                ImGui.SameLine();
                UiTheme.Muted(version.Date);
            }

            if (!string.IsNullOrWhiteSpace(version.Summary))
            {
                ImGui.PushTextWrapPos(0f);
                ImGui.TextUnformatted(version.Summary);
                ImGui.PopTextWrapPos();
            }

            // The order of the categories is fixed here rather than taken from the file, so a
            // notes file cannot bury a new feature under a list of fixes.
            Section(loc.Get(LocKeys.NewsCategoryAdded), version.Added);
            Section(loc.Get(LocKeys.NewsCategoryChanged), version.Changed);
            Section(loc.Get(LocKeys.NewsCategoryFixed), version.Fixed);
            Section(loc.Get(LocKeys.NewsCategoryRemoved), version.Removed);

            ImGui.Separator();
        }
    }

    private static void Section(string heading, List<string>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return;
        }

        ImGui.Spacing();
        UiTheme.Muted(heading);

        foreach (var entry in entries)
        {
            ImGui.Bullet();
            ImGui.PushTextWrapPos(0f);
            ImGui.TextUnformatted(entry);
            ImGui.PopTextWrapPos();
        }
    }
}
