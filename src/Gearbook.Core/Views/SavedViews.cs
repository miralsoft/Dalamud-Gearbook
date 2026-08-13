namespace Gearbook.Core.Views;

/// <summary>
/// The rules around a character's saved views. Pure functions over a list, so the naming rules
/// are tested rather than scattered through the window that edits them.
/// </summary>
public static class SavedViews
{
    /// <summary>Why a view could not be saved under a given name.</summary>
    public enum NameProblem
    {
        None = 0,
        Empty,
        AlreadyTaken,
    }

    /// <summary>
    /// Checks a name before it is used. Names are compared case-insensitively, because two
    /// views called "Raid" and "raid" are the same view to everybody except a comparison.
    /// </summary>
    /// <param name="existing">The views already saved.</param>
    /// <param name="name">The proposed name.</param>
    /// <param name="renaming">The view being renamed, which is allowed to keep its own name.</param>
    public static NameProblem CheckName(
        IReadOnlyList<SavedView> existing,
        string? name,
        SavedView? renaming = null)
    {
        ArgumentNullException.ThrowIfNull(existing);

        if (string.IsNullOrWhiteSpace(name))
        {
            return NameProblem.Empty;
        }

        var trimmed = name.Trim();

        foreach (var view in existing)
        {
            if (ReferenceEquals(view, renaming))
            {
                continue;
            }

            if (string.Equals(view.Name, trimmed, StringComparison.CurrentCultureIgnoreCase))
            {
                return NameProblem.AlreadyTaken;
            }
        }

        return NameProblem.None;
    }

    /// <summary>
    /// Finds a view by name, case-insensitively, or null. Used by the window and by the text
    /// command, so that both reach the same view for the same word.
    /// </summary>
    public static SavedView? Find(IReadOnlyList<SavedView> views, string? name)
    {
        ArgumentNullException.ThrowIfNull(views);

        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();

        foreach (var view in views)
        {
            if (string.Equals(view.Name, trimmed, StringComparison.CurrentCultureIgnoreCase))
            {
                return view;
            }
        }

        return null;
    }

    /// <summary>
    /// The view after the current one, wrapping at the end. What a cycle control and a "next
    /// view" command both use, so the two cannot disagree about what comes next.
    /// </summary>
    public static SavedView? Next(IReadOnlyList<SavedView> views, string? currentName)
    {
        ArgumentNullException.ThrowIfNull(views);

        if (views.Count == 0)
        {
            return null;
        }

        var current = Find(views, currentName);
        if (current is null)
        {
            return views[0];
        }

        for (var i = 0; i < views.Count; i++)
        {
            if (ReferenceEquals(views[i], current))
            {
                return views[(i + 1) % views.Count];
            }
        }

        return views[0];
    }
}
