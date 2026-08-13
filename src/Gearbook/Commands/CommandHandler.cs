using System.Globalization;
using Dalamud.Game.Command;
using Gearbook.Adapters;
using Gearbook.Core.Localization;
using Gearbook.Core.Views;
using Gearbook.Services;
using Gearbook.UI;

namespace Gearbook.Commands;

/// <summary>
/// The text commands. Everything switchable is reachable from one, so the plugin can be driven
/// from a macro without opening a window.
/// </summary>
internal sealed class CommandHandler : IDisposable
{
    /// <summary>The form the plugin relies on: the internal name in lowercase.</summary>
    private const string LongCommand = "/gearbook";

    /// <summary>
    /// A short form, registered as well and allowed to fail. Another plugin may already own it,
    /// and the game itself owns some, so the registration reports whether it succeeded and
    /// teardown only removes what was actually claimed.
    /// </summary>
    private const string ShortCommand = "/gb";

    private readonly GearbookState state;
    private readonly WindowManager windows;

    private bool longRegistered;
    private bool shortRegistered;

    public CommandHandler(GearbookState state, WindowManager windows)
    {
        this.state = state;
        this.windows = windows;

        longRegistered = GearbookServices.Commands.AddHandler(LongCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = state.Loc.Get(LocKeys.CommandHelpMain),
            ShowInHelp = true,
        });

        shortRegistered = GearbookServices.Commands.AddHandler(ShortCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = state.Loc.Get(LocKeys.CommandHelpShort),
            ShowInHelp = true,
        });

        if (!shortRegistered)
        {
            // Logged, not treated as an error. Somebody else owning /gb is a normal state and
            // the long form still works.
            GearbookServices.Log.Information(
                "The short command {Command} is already taken, so it was not registered. {Long} is unaffected.",
                ShortCommand,
                LongCommand);
        }

        if (!longRegistered)
        {
            GearbookServices.Log.Error(
                "The command {Command} could not be registered. Use the plugin installer or the server info bar entry.",
                LongCommand);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (longRegistered)
        {
            GearbookServices.Commands.RemoveHandler(LongCommand);
            longRegistered = false;
        }

        if (shortRegistered)
        {
            GearbookServices.Commands.RemoveHandler(ShortCommand);
            shortRegistered = false;
        }
    }

    private void OnCommand(string command, string arguments)
    {
        var loc = state.Loc;
        var trimmed = arguments?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            windows.ToggleLibrary();
            return;
        }

        var separator = trimmed.IndexOf(' ', StringComparison.Ordinal);
        var verb = (separator < 0 ? trimmed : trimmed[..separator]).ToLowerInvariant();
        var rest = separator < 0 ? string.Empty : trimmed[(separator + 1)..].Trim();

        switch (verb)
        {
            case "bar":
                windows.ToggleBar();
                break;

            case "settings":
            case "config":
                windows.ToggleSettings();
                break;

            case "news":
                windows.ToggleNews();
                break;

            case "view":
                SwitchView(rest);
                break;

            case "switch":
                SwitchGearset(rest);
                break;

            default:
                Report(loc.Get(LocKeys.CommandUnknownSubcommand, verb));
                break;
        }
    }

    private void SwitchView(string name)
    {
        var loc = state.Loc;
        var character = state.Character;

        if (character is null)
        {
            return;
        }

        var view = SavedViews.Find(character.Views, name);
        if (view is null)
        {
            Report(loc.Get(LocKeys.CommandViewNoMatch, name));
            return;
        }

        character.ActiveViewName = view.Name;
        character.CurrentFilter = view.Filter.Clone();
        state.Save();
    }

    private void SwitchGearset(string argument)
    {
        var loc = state.Loc;

        if (string.IsNullOrWhiteSpace(argument))
        {
            Report(loc.Get(LocKeys.CommandSwitchNoMatch, argument));
            return;
        }

        // A number is the unambiguous form and is what the plugin suggests when a name matches
        // more than one set, so it is tried first.
        if (int.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            var bySlot = state.Gearsets.FirstOrDefault(g => g.Gearset.Slot == number - 1);
            if (bySlot is not null)
            {
                state.RequestEquip(bySlot.Gearset.Slot, EquipTrigger.Command);
                return;
            }
        }

        var matches = state.Gearsets
            .Where(g => string.Equals(g.Gearset.Name, argument, StringComparison.CurrentCultureIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            matches = state.Gearsets
                .Where(g => g.Gearset.Name.Contains(argument, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
        }

        switch (matches.Count)
        {
            case 0:
                Report(loc.Get(LocKeys.CommandSwitchNoMatch, argument));
                break;

            case 1:
                state.RequestEquip(matches[0].Gearset.Slot, EquipTrigger.Command);
                break;

            default:
                // Refused rather than guessed. Several sets sharing a name is this plugin's
                // whole reason for existing, so picking one at random here would be the one
                // place it does exactly what it was built to avoid.
                Report(loc.Get(LocKeys.CommandSwitchAmbiguous, argument));
                break;
        }
    }

    private static void Report(string message) =>
        GearbookServices.Log.Information("{Message}", message);
}
