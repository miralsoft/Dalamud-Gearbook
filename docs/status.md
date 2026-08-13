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

## What is not verified, and cannot be from here

**The plugin has never been loaded into the game.** Everything above is a build-time and
test-time statement. Nothing here says the bar draws correctly, that a gearset actually changes,
or that the reconciler behaves the same against real data as against constructed data.

The first session with the game running should, in this order:

1. Point Dalamud at the staging folder `dist/Gearbook` that `build.ps1` produces, and load it.
2. Answer the two open questions in `open-points.md` that need a running game: what
   `EquipGearset` expects as its second argument for a set linked to a glamour plate, and
   whether `GearsetEntry.Id` is zero-based or one-based. The library window shows the number it
   believes; compare it against the game's own list.
3. Check the job classification. Two constants in `JobClassifier` were derived from the table's
   structure rather than from a running game, and if either is wrong the affected jobs appear
   under "Other" rather than under their role. That is the designed failure mode, so it is
   visible rather than silent.
4. Measure the per-frame cost with Dalamud's own plugin statistics window rather than estimating
   it.
5. Do the crash-safety audit over every unsafe block, pointer dereference and game call
   reachable from an interface callback, as a checklist pass over grep hits rather than a
   feeling, and record the outcome here.

## Not started, deliberately

- Globally bound keyboard shortcuts. They compete with the game for input focus.
- The icon. Needed before the first release, not before the first commit.
- Branch protection. Recorded in `open-points.md` as a rule that binds while nothing enforces it.
