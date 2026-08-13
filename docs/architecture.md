# Architecture: Gearbook

Purpose: the technical architecture. What the parts are, where the boundaries run, and why they
run there rather than somewhere else.

## Overview

A plugin shares the game's process. A mistake here does not produce a stack trace and a
recovered feature, it closes the client somebody is playing on. Two consequences shape
everything below: the code that touches the game is kept thin enough to read in one sitting, and
the code that can be wrong in an interesting way is kept where a test can reach it.

That splits the product in two:

```
src/Gearbook.Core/     pure logic, references no platform assembly, fully tested
src/Gearbook/          the plugin, thin adapters over the platform, not testable
tests/Gearbook.Core.Tests/
```

The split is enforced by the compiler rather than by a reviewer noticing: the core project
references neither Dalamud nor FFXIVClientStructs nor Lumina, so a platform type cannot appear in
it by accident.

## Components

### Gearbook.Core

```
Model/          GearsetSnapshot, GearsetRecord, JobInfo, JobRole, JobCategory
Identity/       GearsetReconciler, ReconciliationResult
Filtering/      FilterSpec, FilterEngine
Views/          SavedView, ViewCollection
Sorting/        BarOrder, ListSort
Settings/       GearbookSettings, CharacterSettings, SettingsMigrator
Localization/   LanguageCatalog, LanguageResolver, LocKeys, Resources/en.json, de.json
News/           ReleaseNotesLoader, VersionComparer, Resources/en.json, de.json
Bis/            BisSnapshot, BisPayloadParser
```

**Two gearset types, not one.** `GearsetSnapshot` is what was read out of the game this frame:
slot, job, name, item level, which pieces are missing, the linked glamour plate, and a
fingerprint of the equipment. `GearsetRecord` is what this plugin saved: its own stable id,
favourite, tags, note, last used, position on the bar. One is transient and the other outlives
the session. Merging them is how a note ends up attached to the wrong gearset, so they are
separate types and the reconciler is the only thing that joins them.

**Job facts are passed in, not read.** The core needs a job's role and category for the filters
and must not know Lumina. So the plugin reads the job table once and hands the core a list of
`JobInfo`. The filters are then testable without a game, which is the whole point of the split.

**The BiS payload parser lives here** even though the integration comes later. Parsing a string
that arrives from another plugin is pure logic, it has to survive empty, partial and malformed
input, and it will eventually run where an exception is not a caught error.

### Gearbook

```
Plugin.cs           entry point, registration and teardown in reverse order
Services/           the holder for the injected Dalamud services
Adapters/
  IGearsetReader    reads RaptureGearsetModule on the framework thread
  IGearsetEquipper  the single gate to the server
  IGameStateProbe   logged in, in combat, in a cutscene
  IJobDataSource    the job table and icon ids from Lumina
  IIconProvider     textures
  IServerBarEntry   the entry in the server info bar
  IBisProvider      the later Eorzea Arsenal interface
UI/                 BarWindow, LibraryWindow, SettingsWindow, ReleaseNotesWindow
Configuration/      ConfigurationStore, the thin wrapper over the core settings model
Commands/           registration of the long and the short form
```

Every adapter is as close to a pass-through as it can be, because none of it can be covered by a
test. A pass-through with logic in it is untested logic.

### The single gate

`IGearsetEquipper` is the only type in the codebase that calls `EquipGearset`. Nothing else holds
an instance of it. Every call is logged with its trigger, so a log line says whether a switch came
from the bar, from the library, or from a command.

This exists so that the automation boundary can be reviewed by reading one file. Dalamud's
restrictions forbid interacting with the game servers automatically, and a reviewer has to be able
to prove a negative. Spread across the plugin, that proof would mean auditing everything.

There is no path that equips a gearset without a player action. No queue, no retry, no deferred
switch that fires when combat ends. A switch that cannot happen now is reported and dropped.

## The threading model

Game memory is read on the framework thread only, into an immutable snapshot. The ImGui draw
callback reads that snapshot and never touches game memory, not even through a null-checked
accessor. A click does not call anything; it records an intent that the next framework tick
carries out.

`try`/`catch` is not protection here. It catches a managed exception, and an access violation is
not one and ends the process regardless of any handler. Only the discipline above prevents a
crash; the handler buys early detection and a clean unload, not invulnerability.

Re-reading happens when something can have changed: login, job change, equipment change, a window
opening, plus a slow tick as a net underneath. Not per frame.

Teardown sets a flag at the top of `Dispose` that stops every read immediately, then detaches the
draw callback first and unregisters everything else in reverse order of registration. A plugin is
loaded and unloaded repeatedly inside one game session, so a leak compounds instead of being
cleaned up by process exit, and a hot reload happens while the game is running and a window is
open.

## Identity, the part that can quietly destroy user data

The game offers no stable identity for a gearset. `GearsetEntry.Id` is the number shown in the
list and `ReassignGearsetId` is what the game's own "change number" does to it. The name is free
text and can repeat, including for the same job. So the plugin assigns its own id and re-derives
the mapping on every load.

`GearsetReconciler` matches saved records against the sets currently in the game, in stages, most
certain first:

1. slot, job and name all agree;
2. job and name agree and the match is unique (the set was moved);
3. slot and job agree and the match is unique (the set was renamed);
4. job and the equipment fingerprint agree and the match is unique (renamed and moved);
5. whatever is left is unmatched.

Unmatched records are **kept as orphans**, not deleted, and surfaced in the library so the player
decides. A remaining ambiguity is resolved by slot order and logged, never guessed silently.

This is the single most important piece of logic in the product, it is pure, and it is tested
against every case: two sets sharing a job and a name, a moved set, a renamed set, a deleted set,
a new set, and combinations of those in one load.

## What is stored

One configuration file, managed by the host, carrying a layout version. Loading migrates forward
one step at a time; each step carries a comment saying what changed meaning and why, and the
migration logs that it ran and between which versions.

Everything inside is per character, keyed by the character's own content id, which the gearset
module itself carries. No other character's identifier is read or stored. Per character: the
gearset records, the saved views, the bar layout and order, the display settings, the language
choice and the filter level.

A default nobody chose may be corrected on upgrade; a value somebody chose may not. The test is
whether the stored value still stands at exactly the old default. Where an old value cannot be
converted honestly it is reset rather than carried across, because a converted number that meant
something different is a wrong figure wearing the right name.

A retired setting stays in the code as an obsolete member with a reason rather than being deleted,
so a file written by an older version still loads.

## The windows

Four, all registered with Dalamud's window system, which is what supplies the per-window draw
hooks, the escape-key handling, and the remembered position and size.

- **BarWindow**, the hotbar replacement. Job icons only, taken from the game's own icon for that
  gearset. Size, column count, order and position are the player's. Once locked it has no title
  bar and no frame. Hides in cutscenes, optionally in combat, and follows the player hiding the
  game interface.
- **LibraryWindow**, the management view. Search across name, job, tag and note. Filter sidebar.
  The list carries the game's own gearset number so the player can cross-reference. A detail panel
  holds the note, the tags, the favourite, the bar membership, and the fourteen slots with their
  state.
- **SettingsWindow**, grouped into tabs by topic once it outgrows one screen. Explanations behind
  a help affordance rather than permanently under the control. Dependent settings disabled rather
  than hidden, so the window does not change height while in use.
- **ReleaseNotesWindow**, once automatically after an update, never on a first installation, and
  afterwards only through a control that shows there is something unread.

Any window whose title is translated uses a stable identity suffix, or it forgets its position and
size whenever the language changes. Style pushes happen before any early return and the matching
pops are unconditional: an unbalanced style stack does not corrupt this plugin's window, it
corrupts every window drawn after it, including other plugins'.

Reachability without knowing a command is an entry in the server info bar. The host's own entry
points, the installer's open button and its gear, call the same toggles the commands call rather
than variants of them.

## Contract with Eorzea Arsenal

Later, not now, and designed so that nothing has to be rebuilt when it arrives.

The dependency points one way: Arsenal offers, Gearbook asks. Never the reverse and never both,
because two plugins that need each other can no longer be released independently. The BiS logic
stays entirely with Arsenal; this plugin computes nothing and displays what it is given, so there
is only ever one truth about what counts as a match.

Proposed shape, one call with no arguments returning one JSON string:

```jsonc
{
  "state": "ok",          // ok | noaccount | loading | nodata
  "entries": [
    { "id": 4, "matched": 14, "total": 16, "target": "Ultimate BiS" }
  ]
}
```

One call rather than one per gearset, because ninety gearsets would otherwise mean ninety calls at
exactly the moment a window opens. A `state` field because otherwise an empty column cannot be told
apart from an absent account, and a feature that switches itself off has to say so somewhere the
player can find.

Obligations on this side: the gate can be absent, carry a different version, or throw while Arsenal
reloads, and all three are normal and mean no badge. The result is cached and never fetched per
frame. Nothing ever blocks. The cache is tied to a recognisable state of the gearset list and
thrown away whenever it changes, because a cached badge keyed by slot would otherwise be shown
against the wrong set after a reorder, which is worse than no badge.

Without Arsenal nothing reserves space. No empty box, no placeholder, no message.

## Deploy and update mechanism

No deploy. A release is a tag, a built archive attached to a GitHub release, and an aggregate index
that reads it. The procedure and the two silent delays in that chain are in `release.md`.
