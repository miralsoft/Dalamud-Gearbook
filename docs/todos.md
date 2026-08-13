# To-dos: Gearbook

Purpose: the to-do list, grouped per area. Planned work. Open questions live in `open-points.md`.

## Build and solution

- [ ] `global.json` pinning SDK 10.0.303
- [ ] `NuGet.config` clearing inherited feeds, nuget.org only
- [ ] `Directory.Build.props`: version, nullable, warnings as errors, analysers, documentation file
- [ ] `Directory.Build.local.props.example` and the `GEARBOOK_DEVTOOLS` symbol
- [ ] `.editorconfig`
- [ ] `Gearbook.slnx` and the three projects
- [ ] `build.ps1` running format check, build and tests, publishing into a staging folder and
      copying the main assembly last

## Gearbook.Core

- [ ] Model: `GearsetSnapshot`, `GearsetRecord`, `JobInfo`, `JobRole`, `JobCategory`
- [ ] `GearsetReconciler` and `ReconciliationResult`, tests first
- [ ] `FilterSpec` and `FilterEngine`, including the three filter levels
- [ ] `SavedView` and `ViewCollection`
- [ ] `BarOrder` and `ListSort`
- [ ] `GearbookSettings`, `CharacterSettings`, `SettingsMigrator` with a logged migration
- [ ] `LanguageCatalog`, `LanguageResolver`, `LocKeys`, catalogues for `en` and `de`
- [ ] `ReleaseNotesLoader` and a tolerant `VersionComparer`
- [ ] `BisSnapshot` and `BisPayloadParser`

## Gearbook (the plugin)

- [ ] `Plugin.cs` with registration and reverse-order teardown, plus the teardown flag
- [ ] Service holder
- [ ] `IGearsetReader` and its implementation, framework thread only
- [ ] `IGearsetEquipper`, the single gate, every call logged with its trigger
- [ ] `IGameStateProbe` for logged in, combat and cutscene
- [ ] `IJobDataSource` and `IIconProvider` over Lumina
- [ ] `IServerBarEntry`
- [ ] `IBisProvider` with an implementation that returns nothing until Arsenal offers the gate
- [ ] `BarWindow`
- [ ] `LibraryWindow`
- [ ] `SettingsWindow`
- [ ] `ReleaseNotesWindow`
- [ ] `ConfigurationStore`
- [ ] Commands, long form relied on and short form allowed to fail
- [ ] Wire `UiBuilder.OpenMainUi` and `OpenConfigUi` to the same toggles the commands use

## Workflows

- [ ] `content-checks.yml` copied with its provenance line
- [ ] `ci.yml`: restore, build, test, format check, local-props-not-committed, vulnerable packages
- [ ] `release.yml`: tag against built version, packaged archive, single-entry index file

## Before the first release

- [ ] The icon, exactly 512 by 512, one source file feeding both resolution paths
- [ ] `README.md` written for players, with the section on what the plugin does not do, the game's
      trademark, the non-affiliation statement and the note about the host framework
- [ ] Verify the two runtime facts listed in `status.md`
- [ ] Measure per-frame cost with Dalamud's own plugin statistics window rather than estimating
- [ ] The crash-safety audit over every unsafe block, pointer dereference and game call reachable
      from an interface callback, recorded in `status.md`
