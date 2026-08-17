# Status: Gearbook

Purpose: the current state of the project. The shared handover channel between sessions and
between different AIs. Updated at the end of every working session (M-08, R-02).

## Overall

Version `0.1.0` is written and unreleased, on `main`, unpushed. Everything discussed in the
scoping conversation is implemented. R-16 permits working directly on `main` until the first
release other people can install, which has not happened.

What is verified and what is not is the important part of this document, so it is stated
plainly at the end.

## Repository foundation

- **Done:** Repository renamed to `Dalamud-Gearbook` with its description set. Committer
  identity `Sanaka`. Foundation hooks installed and both **observed blocking a real violation**,
  then the probes removed in the same session (R-08, R-20). Entrypoint with its M-19 provenance
  line, enforcement configuration, ignore rules, attributes, AGPL-3.0 licence.
- **Next:** nothing outstanding.

## Project documentation

- **Done:** All seven documents plus `release.md`. `README.md` written for players with the
  section on what the plugin does not do, the trademark and the non-affiliation statement.
  `CHANGELOG.md` for whoever works on it.
- **Next:** keep `decisions.md` current. It is append-only.

## Build and solution

- **Done:** `global.json` pinning SDK 10.0.303 with roll-forward disabled, `NuGet.config`
  clearing inherited feeds, `Directory.Build.props` as the one authoritative place for the
  version, the developer-tools example file, `.editorconfig`, the solution, the three projects,
  and `build.ps1` running the same gates CI runs and staging a build the host can load.
- **Next:** nothing outstanding.

## Gearbook.Core

- **Done:** Model, the reconciler, filtering with its three levels, saved views, bar order,
  settings with a layout version and a migration step, localisation for German and English, the
  release notes, the job classifier, the equipment fingerprint, and the best-in-slot payload
  parser. 152 tests.
- **Next:** nothing outstanding for this version.

## Gearbook (the plugin)

- **Done:** The entry point with reverse-order teardown and a flag that stops every read at the
  top of `Dispose`. Adapters for reading gearsets, the single equip gate, the game state, the
  job table and the icons, the server info bar entry, and the Arsenal interface. The four
  windows. The configuration store. The commands, long form relied on and short form allowed to
  fail. `UiBuilder.OpenMainUi` and `OpenConfigUi` wired to the same toggles the commands use.
- **Next:** everything left needs a running game. See below.

## Workflows

- **Done:** `content-checks.yml` copied from the foundation with its provenance line, `ci.yml`
  with the build, tests, format check, developer-tools guard and dependency audit, and
  `release.yml` with the tag-against-built-version check and the post-publish inspection.
- **Next:** neither workflow has run on the server yet, because nothing has been pushed.

## What is verified, and how

- `dotnet build --configuration Release` over the solution: **zero warnings, zero errors**.
- `dotnet test`: **152 of 152 passing**.
- `dotnet format --verify-no-changes`: **clean**.
- `build.ps1`: runs all of the above and produces a staging folder the host can be pointed at.
- Both git hooks: **observed rejecting a planted violation and accepting a clean input**.
- Both CI guards whose failure would look like success (the committed developer-tools file and
  the vulnerability scan): **run locally and observed firing in both directions**.
- The platform API surface: **established by reflecting over the installed Dalamud 15.0.3 with
  FFXIVClientStructs 7.51.0**, not from memory.
- The build SDK version and the action digests: **looked up rather than recalled**.
- The packaged output: the manifest inside the built archive reports version `0.1.0.0` at API
  level 15, and the generated index is a JSON array as the client requires.

## The first session with the game running (2026-08-15)

**The plugin loads and works.** Observed in a live client: the bar draws with the game's own job
icons and the item levels beside them, the settings window is in German, and switching a gearset
from the bar succeeds, with the game's own log confirming both a black mage and a dark knight
change. The full list of what that session settled is in `open-points.md`.

One design error found the moment it met real data. A gearset missing a piece was being refused,
and the game itself does not refuse: it offers a suitable substitute and lets the player answer.
The block is gone, the marking stays. Recorded in `decisions.md` as superseding the original
decision.

Still to do in the game, in this order:

1. The glamour plate case: what `EquipGearset` expects as its second argument for a set that is
   linked to a plate. Switching an unlinked set with a zero works; the linked case is untested.
2. Check the job classification. Two constants in `JobClassifier` were derived from the table's
   structure rather than from a running game, and if either is wrong the affected jobs appear
   under "Other" rather than under their role. That is the designed failure mode, so it is
   visible rather than silent. A machinist and a black mage answer one, a crafter and a gatherer
   the other.
3. Exercise the reconciler against real data: rename a set, reorder one, delete one, and check
   that the notes and favourites follow.
4. Measure the per-frame cost with Dalamud's own plugin statistics window rather than estimating
   it.
5. Do the crash-safety audit over every unsafe block, pointer dereference and game call
   reachable from an interface callback, as a checklist pass over grep hits rather than a
   feeling, and record the outcome here.

## Not started, deliberately

- Globally bound keyboard shortcuts. They compete with the game for input focus.
- The icon. Needed before the first release, not before the first commit.
- Branch protection. Recorded in `open-points.md` as a rule that binds while nothing enforces it.

## The crash-safety audit (2026-08-16, before `1.0.0`)

A checklist pass over every place this plugin touches game memory, run as the release procedure
requires rather than by impression. The surface is small and was enumerated with grep rather than
from memory: four `unsafe` classes, all of them adapters, and sixteen pointer dereferences between
them. No other file in the plugin holds a pointer, and the core library cannot: it references no
platform assembly, which the compiler enforces.

**Guarding.** Every dereference sits behind a null check on the instance accessor obtained in the
same method, and the reader additionally checks each entry pointer and asks the game whether the
slot is valid first. A try block would not help here and none is used for this purpose: it catches
a managed exception, and an access violation is not one.

**Two defects found, both fixed in this release.**

The arranger fetched the module pointer once and dereferenced it up to thirty-three times inside a
loop that reads game memory between moves. A player can reach the title screen in the middle of a
sort, and a pointer that was good thirty moves ago is not a pointer. It now fetches and checks on
every move, and treats a module that has gone away as a stop rather than a crash.

The bar asked the game for a job icon **while drawing**, once per tile per frame. That is a
violation of GB-03, which says the draw callback reads this plugin's own lists and nothing else,
and it was also the wrong cost in the wrong place for a number that changes only when the list
does. The icons are now collected on the framework thread with everything else and looked up while
drawing.

Both are the same mistake in two shapes: a game call written where it was convenient rather than
where the rule puts it. Worth naming, because the next one will look convenient too.

## The per-frame cost, measured (2026-08-16, before `1.0.0`)

Read out of the host's own plugin statistics window rather than estimated, which is what the
pre-release list asked for.

Drawing, with the bar on screen and no window open: **0.16 ms** on average. Twelve other plugins
were loaded at the time and the nearest were Ice's Cosmic Exploration at 0.11 and vnavmesh at
0.11, with most of the rest between 0.003 and 0.04. The framework tick averages 0.14 ms.

Gearbook is therefore still the most expensive drawer of the thirteen, and that is structural
rather than a defect: its surface is permanent and holds a dozen or more tiles, where most of the
others cost what they cost only while a window is open. A frame at sixty per second is 16.6 ms, so
this is about one per cent of it.

Both averages carry a one-off inside them. The longest single draw was 21 ms and the longest tick
13 ms; those are the first frame loading a texture per job icon and the first tick reading the job
and territory tables. Over a few hundred frames a single 21 ms outlier accounts for a large share
of a 0.16 ms average, so the settled figure is lower than the one written above.

The first reading was 0.20 ms, taken before the two defects the reading itself uncovered were
fixed: a game read per tile per frame from the draw callback, and the bar re-deriving its contents
every frame. Measuring is what found them; neither was visible in the code by reading it, and one
had already survived a deliberate audit.

## Where the project stands (2026-08-16, after `1.0.0`)

Written so that a session starting from nothing can pick this up. The documents in `docs/` are the
only handover there is (M-08, I-07), and everything below is here because it would otherwise live
only on one machine.

**`1.0.0` is released.** Tag `v1.0.0`, both assets published, the manifest inside the archive names
the version, and the icon resolves now that the repository is pushed. What remains of the release
chain is not in this repository: the entry in `miralsoft/Dalamud-Plugins`, which is one line in
`plugins.json` plus a section in its README, and which somebody adds over there (M-18).

**`main` is protected, and administrators are not exempt.** Every change now reaches it through a
pull request with `Build` and `Content checks` passing on the merged result. Zero approving reviews
are required, because a single maintainer requiring one could never merge. Committing straight to
`main` will be refused, and that refusal is the rule working rather than a fault to route around.

**The last thing discussed and refused** was a button to extract materia from every eligible piece
at once. It is bulk automation of a game action and the plugin's own manifest promises users that
nothing happens on its own. Artisan is where that belongs. Two alternatives were offered and both
withdrawn: a warning that extraction destroys the gear, which is simply false, the game only resets
spiritbond to one per cent; and an overview of spiritbond across gearsets, which the game's own
materia window already provides. Recorded because both were confidently wrong and the next person
should not spend the afternoon rediscovering it.

## The parts of the setup that are not in this repository

A fresh clone needs these, and none of them is carried by git. `CLAUDE.md` covers the first three;
the fourth is written down here because nothing else says it.

1. **The foundation clone.** `git clone` it into `.foundation-docs/` and exclude it through
   `.git/info/exclude`, never `.gitignore`.
2. **The git hooks**, copied out of the foundation into `.git/hooks/`. They are not part of any
   repository, so a fresh clone has none.
3. **`.git/info/exclude`** holds two lines: `.foundation-docs/` and `.claude/`.
4. **The automatic staging hook.** The owner tests in a running client, and a build that stays in
   `bin/` is a change they cannot see. A `Stop` hook in `.claude/settings.local.json` runs
   `build.ps1 -SkipChecks` after every turn and stamps `dist/last-build.txt` so that "it ran" and
   "they can see it ran" are the same thing. That file holds absolute paths, so it is deliberately
   not committed; recreate it pointing at this checkout.

Dalamud is pointed at `dist/Gearbook/Gearbook.dll` as a development plugin. That registration lives
in the host's own configuration and survives independently of anything here.

## How the game's own data was read, when it was needed

Several answers in this project came from reading the installed client rather than from memory: the
job table that exposed a wrong constant, the role icon numbers, the job display order, the cosmic
exploration zones, and the wording of the materia extraction dialog. The method is worth keeping
because it settled questions nothing else could.

A throwaway console project referencing `Lumina.dll` and `Lumina.Excel.dll` out of the Dalamud
install, pointed at the game's `sqpack` folder, reads any sheet or texture. Icons were written out
as PNG contact sheets and looked at. None of it belongs in this repository, and all of it is a few
minutes to rebuild when the next question arrives.

## The crash-safety audit, redone by reachability (2026-08-17, raising the foundation to 3.1.0)

R-21 says an audit follows reachability, not form, and records what it looked for: start at the
places a rule constrains (here, GB-03's "the draw callback reads only this plugin's own snapshot")
and follow every call path outward, rather than searching the source for the shape of the thing
already known to be wrong. The 2026-08-16 pass had searched for pointer dereferences and found two
real defects that way; this pass started instead from every method the windowing system calls each
frame (`Draw`, `DrawConditions`, `PreDraw`) and from every popup and tooltip block inside them, and
followed each one outward until it was settled whether it reached a live host or game read.

**What it found.** Five call chains reached a live read from inside a draw path, none of them a
pointer dereference and none of them a crash risk in the sense the framework profile's crash-safety
section means: `BarWindow.DrawConditions` read `IGameStateProbe.IsInCutscene` and `IsInCombat`
directly to decide whether to hide the bar; `BarWindow.DrawExternalTools` read
`IsInCosmicExploration` the same way; `UiTheme.GearsetTile` and `LibraryWindow`'s detail panel both
reached `GearsetEquipper.CheckCanChangeGear()` through `GearbookState.CheckCanEquip`, once per tile
per frame; `ArrangeWindow` reached the same gate through `GearsetArranger.CanArrange()`; and both
`BarWindow` and `SettingsWindow` read `ExternalTools.IsAvailable`/`NameOf`, which walk the host's
installed-plugin list, as a checkbox label and a shortcut-tile condition drawn every frame.

Every one of these reads like an ordinary property or method call at the point of use, which is
exactly why grep for a pointer operator, the shape the previous pass searched for, could not have
found them. The `CheckCanEquip` one is the same mistake the icon-fetch defect was, in a different
member: a game-reaching call sitting where it was convenient, once per tile per frame, on a bar
that is already the most expensive drawer of the plugins measured against it.

**What was searched, so the next pass can see what this one covers.** Every `Draw`, `DrawConditions`
and `PreDraw` override in `src/Gearbook/UI/`, every call they make into `GearbookState` and from
there into the four adapter interfaces, and the two `IExternalTools` methods reachable from a
tooltip or a checkbox label. Not searched: the settings and release-notes windows' own layout code,
which touches no adapter, and the framework-tick path itself, which was already the audited-safe
side of the split.

**The fix.** `GearbookState` now snapshots `IsInCutscene`, `IsInCombat`, `IsInCosmicExploration`,
`EquipAvailability`, `ArrangeAvailability`, tool availability and tool names once per tick, on the
framework thread, the same place the icon lookup and the equip queue already lived. Every draw-path
caller now reads the cached value. Combat and cutscene detection stay as responsive as before,
because the tick runs every frame regardless; nothing here waits for the three-second slow refresh
that gates the expensive reconciliation. Build clean, format clean, all 286 tests still passing (the
change touches only the plugin project, which has no unit tests of its own by design).
