using System.Globalization;
using Gearbook.Core.Filtering;
using Gearbook.Core.Model;
using Gearbook.Core.Sorting;

namespace Gearbook.Core.Settings;

/// <summary>
/// Brings a stored configuration forward one layout version at a time.
/// </summary>
/// <remarks>
/// <para>
/// An update never silently changes what a player configured. The file carries a version, each
/// step from one version to the next has a migration, and a setting that is removed or renamed
/// is migrated rather than dropped. The failure this prevents is a configuration file that no
/// longer loads, and it surfaces on the player's machine rather than on ours.
/// </para>
/// <para>
/// There is exactly one version so far. The mechanism exists anyway, because retrofitting it
/// at the moment the second version is needed means the first one shipped without a way to be
/// upgraded, and by then the files it wrote are on other people's machines.
/// </para>
/// </remarks>
public static class SettingsMigrator
{
    /// <summary>The layout version this build writes.</summary>
    public const int CurrentVersion = 6;

    /// <summary>
    /// Migrates in place and reports what it did.
    /// </summary>
    /// <param name="settings">The freshly loaded configuration.</param>
    /// <param name="log">Called with a line per step, because a migration that ran silently is
    /// indistinguishable from one that did not, and the difference matters when somebody is
    /// looking at a value they cannot explain.</param>
    /// <returns>True when the configuration is safe to use.</returns>
    public static bool Migrate(GearbookSettings settings, Action<string> log)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(log);

        if (settings.LayoutVersion > CurrentVersion)
        {
            // Written by a newer build. Downgrading would mean guessing what a value this build
            // has never heard of used to mean, so it is left exactly as it is and reported.
            // Failing closed here keeps the file intact for the build that understands it.
            log(string.Format(
                CultureInfo.InvariantCulture,
                "Configuration is layout version {0}, which is newer than this build understands ({1}). "
                    + "It is being used as it stands and nothing will be rewritten. "
                    + "Install the newer version of Gearbook, or the settings this build does not know about will be lost.",
                settings.LayoutVersion,
                CurrentVersion));

            return false;
        }

        while (settings.LayoutVersion < CurrentVersion)
        {
            var from = settings.LayoutVersion;

            switch (from)
            {
                case 0:
                    // Version 0 is a file written before this plugin had a layout version at
                    // all, which in practice means a hand-edited or truncated file. Nothing
                    // needs converting, the fields are the same, so the step only stamps the
                    // version on it.
                    settings.LayoutVersion = 1;
                    break;

                case 1:
                    // The shipped bar icon size changed from forty to thirty. Forty was chosen
                    // before the bar had been seen in a real client, where it sits noticeably
                    // larger than the game's own hotbars.
                    //
                    // Corrected only where the stored value still stands at exactly the old
                    // default, which is the test for whether anybody ever expressed an opinion
                    // about it. A value that moved was a decision and stays the player's.
                    foreach (var character in settings.Characters.Values)
                    {
                        if (character?.Bar is null)
                        {
                            continue;
                        }

                        character.Bar.IconSize = CorrectUnchosenDefault(
                            character.Bar.IconSize,
                            BarSettings.PreviousDefaultIconSize,
                            BarSettings.DefaultIconSize);
                    }

                    settings.LayoutVersion = 2;
                    break;

                case 2:
                    // The favourite mark and "on the bar" used to be two separate things, and
                    // the rule joining them could not be explained. They are one thing now:
                    // a favourite is on the bar, and the position only orders them.
                    //
                    // Anything that was on the bar becomes a favourite, because that is what the
                    // player arranged and it must survive. A favourite that was not on the bar
                    // keeps its mark and joins the bar at the end, which is a change they will
                    // see, and the alternative is dropping a mark they set deliberately.
                    foreach (var character in settings.Characters.Values)
                    {
                        if (character?.Gearsets is null)
                        {
                            continue;
                        }

                        foreach (var gearset in character.Gearsets)
                        {
                            if (gearset.BarPosition is not null)
                            {
                                gearset.IsFavourite = true;
                            }
                        }
                    }

                    settings.LayoutVersion = 3;
                    break;

                case 3:
                    // The bar's two-way "favourites or everything" became a view that can also
                    // be a role, a category or a tag. The old choice is carried across rather
                    // than reset, because somebody who had switched the bar to everything meant
                    // it, and a migration that drops a choice is the failure migrations exist
                    // to prevent.
                    foreach (var character in settings.Characters.Values)
                    {
                        if (character?.Bar is null)
                        {
                            continue;
                        }

#pragma warning disable CS0618 // Reading the retired member is the entire point of this step.
                        character.Bar.ViewKind = character.Bar.Contents == BarContents.All
                            ? BarViewKind.All
                            : BarViewKind.Favourites;
#pragma warning restore CS0618
                    }

                    settings.LayoutVersion = 4;
                    break;

                case 4:
                    // The switcher stopped offering crafter and gatherer as roles, because the
                    // crafting and gathering categories select exactly the same gearsets and the
                    // menu was listing each of them twice under two names.
                    //
                    // A bar left standing on one of those two roles is moved to the matching
                    // category rather than reset. It holds the same gearsets either way, so the
                    // player sees no change at all, which is the point: the entry they chose is
                    // gone from the menu and the bar must not quietly become something else.
                    foreach (var character in settings.Characters.Values)
                    {
                        if (character?.Bar is null || character.Bar.ViewKind != BarViewKind.Role)
                        {
                            continue;
                        }

                        switch (character.Bar.ViewRole)
                        {
                            case JobRole.Crafter:
                                character.Bar.ViewKind = BarViewKind.Category;
                                character.Bar.ViewCategory = JobCategory.Crafting;
                                break;

                            case JobRole.Gatherer:
                                character.Bar.ViewKind = BarViewKind.Category;
                                character.Bar.ViewCategory = JobCategory.Gathering;
                                break;

                            default:
                                break;
                        }
                    }

                    settings.LayoutVersion = 5;
                    break;

                case 5:
                    // The bar stopped having a sort of its own and follows the library's. Two
                    // controls both answering "in what order" could disagree, and a bar left on
                    // "by job" while the library was grouped by role read as sorting that simply
                    // did not work.
                    //
                    // What is left on the bar is the one question the library cannot answer:
                    // whether the arrangement the player dragged into shape still wins.
                    foreach (var character in settings.Characters.Values)
                    {
                        if (character?.Bar is null)
                        {
                            continue;
                        }

#pragma warning disable CS0618 // Reading the retired member is the entire point of this step.
                        var storedSort = character.Bar.Sort;
#pragma warning restore CS0618

                        character.Bar.UseArrangement = storedSort is null;

                        // An order the player picked for the bar is carried over to the library
                        // rather than dropped, but only where the library is still sitting on the
                        // value it shipped with. A library that was set deliberately is an answer
                        // somebody gave, and the bar's old setting does not get to overwrite it.
                        if (storedSort is { } sort
                            && character.CurrentFilter is { Sort: GearsetSortOrder.Slot })
                        {
                            character.CurrentFilter.Sort = sort;
                        }
                    }

                    settings.LayoutVersion = 6;
                    break;

                default:
                    // Reached only if CurrentVersion was raised without adding the step. Stop
                    // rather than spin, and say so.
                    log(string.Format(
                        CultureInfo.InvariantCulture,
                        "No migration exists from layout version {0}. The configuration is left as it is.",
                        from));
                    return false;
            }

            log(string.Format(
                CultureInfo.InvariantCulture,
                "Migrated the configuration from layout version {0} to {1}.",
                from,
                settings.LayoutVersion));
        }

        return true;
    }

    /// <summary>
    /// Corrects a default that nobody ever chose, and leaves a value somebody did choose alone.
    /// </summary>
    /// <remarks>
    /// The test is whether the stored value still stands at exactly the old default. If it
    /// does, no opinion was ever expressed and the corrected default should reach the people who
    /// already installed. If it moved, that was a decision and it stays theirs.
    /// </remarks>
    /// <param name="stored">What is in the file.</param>
    /// <param name="oldDefault">What this build used to ship as the default.</param>
    /// <param name="newDefault">What it ships now.</param>
    public static T CorrectUnchosenDefault<T>(T stored, T oldDefault, T newDefault)
        where T : IEquatable<T> =>
        stored.Equals(oldDefault) ? newDefault : stored;
}
