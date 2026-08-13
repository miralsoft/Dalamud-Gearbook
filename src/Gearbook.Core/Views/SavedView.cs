using Gearbook.Core.Filtering;

namespace Gearbook.Core.Views;

/// <summary>
/// A named filter. What turns "tanks, favourites, not on the bar" from something the player
/// rebuilds every evening into something they pick once.
/// </summary>
public sealed class SavedView
{
    /// <summary>The name the player gave it. Unique within a character, case-insensitively.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>What it shows.</summary>
    public FilterSpec Filter { get; set; } = new();

    /// <summary>A copy that shares nothing with this one.</summary>
    public SavedView Clone() => new() { Name = Name, Filter = Filter.Clone() };
}
