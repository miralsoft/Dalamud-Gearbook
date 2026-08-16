# Gearbook

Every gearset, sorted, filtered, one click to switch. A plugin for Final Fantasy XIV, for the
Dalamud framework.

## The problem it solves

Put all your gearsets on hotbars and you spend bar after bar on them, which is space you needed
for something else. It gets worse with every crafter and gatherer. Use the game's own gearset
list instead and you get a long column of names with nothing to sort or filter by, where working
out which entry is which set means reading every line.

Gearbook gives you both halves of what is missing: a small bar of job icons you can leave on
screen and click, and a window where you can actually search, filter and organise.

It is built for the case the game handles worst: **several sets for the same job**. A dark knight
set for one fight, another for a different one, and a third that only exists for a glamour are
three separate things, and every other tool treats them as one job to switch to.

## What it does

- **A bar of job icons.** One click switches. Arrange it as a row, a column or a grid, put it
  where you want it, then lock it so it stays there. It can hide itself during cutscenes and, if
  you want, in combat.
- **A library window.** Search across names, jobs, your tags and your notes at once.
- **Favourites, tags and notes**, per gearset, per character.
- **Filters** by role and category, plus incomplete sets, what is on the bar, what is linked to a
  glamour plate, and what you have not worn in a while.
- **Saved views**, so a filter you use every week is one click instead of five.
- **A filter panel that can be turned down.** Three levels, from favourites only to everything.
  Turning it down hides controls; it never discards what you set up.
- **A warning when two of your gearsets share a job and a name**, which the game itself gives you
  no way to tell apart.
- **Text commands for everything**, so it all works from a macro.
- **German and English**, following whatever language Dalamud is set to unless you choose one.

## What it does not do

This matters more than the list above, so it is not at the bottom of the page by accident.

- **Nothing happens on its own.** Gearbook never switches a gearset, never sends anything to the
  game, and never acts in any way unless you click or type something. There is no automation, no
  queueing, no "switch for me when combat ends". A switch you asked for that cannot happen right
  now is refused and told to you, not remembered for later.
- **It does not touch combat.** No rotation help, no parsing, no damage meters, no advice.
- **It does not need an account, a server, or an internet connection.** There is no login, no
  telemetry and no phoning home. Nothing about you or your characters leaves your machine, ever.
- **It does not read anything about other players.**
- **It does not create, delete or modify your gearsets.** It reads them and it equips them. The
  notes and tags you add live in Gearbook's own configuration, not in your gearsets.

## Later: Eorzea Arsenal

If you also use [Eorzea Arsenal](https://github.com/miralsoft/Dalamud-Eorzea-Arsenal), a future
version of Gearbook will be able to show how far each gearset is from its best-in-slot target,
right next to it in the list. That is not in this version yet.

It will always be optional. Without Eorzea Arsenal installed, nothing about Gearbook changes: no
empty column, no placeholder, no message. Gearbook computes nothing about best in slot itself and
never will; it only displays what the other plugin tells it.

## Installing

Gearbook is distributed through the plugin index at:

```
https://xivarsenal.app/plugins.json
```

Add that address in Dalamud's settings under the experimental section, then find Gearbook in the
plugin installer.

## Commands

| Command | What it does |
|---|---|
| `/gearbook` | Opens the library window |
| `/gearbook bar` | Shows or hides the bar |
| `/gearbook settings` | Opens the settings |
| `/gearbook news` | Opens the release notes |
| `/gearbook view <name>` | Switches to a saved view |
| `/gearbook switch <name or number>` | Equips a gearset |
| `/gb` | Short form, if no other plugin has claimed it |

## Licence

AGPL-3.0-or-later. See [LICENSE](LICENSE).

## Legal

FINAL FANTASY is a registered trademark of Square Enix Holdings Co., Ltd. FINAL FANTASY XIV
© SQUARE ENIX CO., LTD. Gearbook is an unofficial, fan-made tool and is **not affiliated with,
endorsed by, or connected to Square Enix** in any way.

Dalamud is a third-party framework that is likewise not affiliated with Square Enix. Using it, and
using any plugin written for it, is your own choice and at your own risk.
