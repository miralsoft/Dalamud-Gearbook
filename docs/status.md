# Status: Gearbook

Purpose: the current state of the project. The shared handover channel between sessions and
between different AIs. Updated at the end of every working session (M-08, R-02).

## Overall

Project set up, nothing functional yet. The scope, the name, the licence, the identity strategy
and the architecture are agreed and written down. Version `0.1.0` is unreleased and work goes
directly onto `main`, which R-16 permits until the first release other people can install.

## Repository foundation

- **Done:** Repository renamed to `Dalamud-Gearbook` and given its description. Committer identity
  set to `Sanaka`. Foundation hooks installed and both **observed blocking a real violation**: the
  commit-msg hook rejected a planted attribution marker, the pre-commit hook rejected a planted
  em-dash, and the probe was removed in the same session (R-08, R-20). Entrypoint, enforcement
  configuration, ignore rules, attributes and the AGPL-3.0 licence committed.
- **Next:** nothing outstanding.

## Project documentation

- **Done:** All seven documents plus `release.md`.
- **Next:** keep `decisions.md` current as work proceeds. It is append-only.

## Platform facts

- **Done:** Established by reflecting over the installed Dalamud 15.0.3 with FFXIVClientStructs
  7.51.0 on 2026-08-13, rather than from memory. `GearsetEntry` carries `Id`, `ClassJob`, a 48-byte
  name, `ItemLevel`, `GlamourSetLink`, `BannerIndex`, `Flags` and fourteen equipment slots.
  `EquipGearset(int, byte)` is the switch. `GearsetFlag.MainHandMissing` and the per-item flags make
  an incomplete set detectable. `GetClassJobIconForGearset` gives the icon the game itself draws.
  `IUiBuilder` supplies the cutscene and interface-hidden signals, `ICondition` the combat state,
  `IDalamudPluginInterface.UiLanguage` plus its change event the language, `ICallGateSubscriber` the
  later IPC. `IClientState.LocalPlayer` does not exist in v15, as the profile records.
- **Next:** two facts still need a running game rather than reflection, and are not to be written
  into an adapter before they are checked: what the second parameter of `EquipGearset` expects when
  a set is linked to a glamour plate, and whether `Id` is zero-based or one-based.

## Build and solution

- **Done:** nothing yet.
- **Next:** `global.json`, `NuGet.config`, `Directory.Build.props`, the developer-tools example
  file, `.editorconfig`, the solution and the three projects, `build.ps1`.

## Gearbook.Core

- **Done:** nothing yet.
- **Next:** the model, then `GearsetReconciler` with its tests first, because it is the piece whose
  failure silently destroys a player's notes and favourites. Then filters, settings and migration,
  localisation, release notes, BiS payload parsing.

## Gearbook (the plugin)

- **Done:** nothing yet.
- **Next:** entry point and teardown, the adapters, the single equip gate, the four windows, the
  configuration store, the commands and the server info bar entry.

## Workflows

- **Done:** nothing yet.
- **Next:** `content-checks.yml` copied from the foundation with its provenance line, `ci.yml`,
  `release.yml`.

## Not started, deliberately

- The Eorzea Arsenal BiS integration. The interface and the parser are part of the first version,
  the call is not, because the other side does not offer the gate yet.
- Globally bound keyboard shortcuts.
- The icon. Needed before the first release, not before the first commit.
