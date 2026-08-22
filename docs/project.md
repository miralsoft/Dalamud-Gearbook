# Project: Gearbook

Purpose: the identity card of the project. Who it is, what it is, which rules apply to it.

## Identity

- **Name:** Gearbook
- **Slug:** gearbook
- **Owner:** Sanaka
- **Summary:** A Dalamud plugin for Final Fantasy XIV that lets a player find, filter and switch
  gearsets without spending hotbar slots. A bar of job icons replaces the hotbars, and a library
  window adds what the game's own gearset list cannot do: search, filters, favourites, tags and
  notes. Everything a player can already see, arranged so it can be used.
- **Kind:** external (M-12). The documentation lives here in `docs/`. Nothing is ever written
  back to the foundation, and the foundation carries no folder for this project.
- **Committer identity:** `Sanaka` (R-03, R-19). A private project, so not the company name. The
  email does not vary. `.miralsoft-enforcement` at the repository root matches this declaration.

## Declared languages, frameworks, and active profiles

- **C#**, which activates `rules/languages/csharp.md`.
- **Dalamud**, the plugin framework for Final Fantasy XIV, which activates
  `rules/frameworks/dalamud.md`.
- The blueprint `blueprints/dalamud-plugin.md` applies and is binding (M-14).

## Targeted foundation version

**3.1.0.** Raised from 2.0.0 on 2026-08-17; see `decisions.md` for the review and the work it
required. Reviewed whenever a release is cut (M-17): read the foundation changelog from this
version onward, then either raise it and do the work, or leave it and record why. Raising it
includes re-copying anything copied out of the foundation and updating its provenance line (M-19).

## Platform versions

| Item | Value |
|---|---|
| Dalamud | 15.0.3 |
| API level | 15 |
| FFXIVClientStructs | 7.51.0 |
| Runtime | .NET 10 |
| SDK | 10.0.303, pinned in `global.json` |
| Build SDK | `Dalamud.NET.Sdk/15.0.0` |

Established on 2026-08-13 by reflecting over the installed Dalamud rather than from memory or
documentation. A Dalamud major version changes the API level and can break the plugin outright,
so it is reviewed before adoption and the outcome recorded in `decisions.md`.

## Code repositories

- `miralsoft/Dalamud-Gearbook`, public. Holds the source, the manifest, the icon, the build and
  the two workflows. This is the only repository this project owns.

Two repositories this project depends on and never writes to (M-18):

- `miralsoft/Dalamud-Plugins`, the aggregate index that collects the releases and serves the
  address players subscribe to. Its entry, its description and its icon address belong to
  whoever maintains it.
- `miralsoft/Dalamud-Eorzea-Arsenal`, the sibling plugin that will later offer the BiS interface
  this plugin reads. The dependency points one way only.

## Hosting and deploy summary

No server and no backend. The plugin reads the local game process, stores its configuration in
the host's own configuration directory, and needs neither an account nor an internet connection
for any feature it has.

Delivery: a version tag is pushed, the release workflow builds, checks the tag against the built
version, and publishes a release carrying the packaged archive plus a single-entry index file.
The aggregate index picks the release up on its next run. The full procedure, with the delays
and their durations, is in `release.md`.

## High-level architecture summary

Two projects. `Gearbook.Core` holds everything that can fail in an interesting way and nothing
that touches the platform: the reconciliation that keeps favourites and notes attached to the
right gearset across renames and reordering, the filters, the settings model with its
migrations, the language catalogues and the release notes. It is tested without a running game.
`Gearbook` is the plugin: thin adapters over the platform, four windows, and exactly one type
that is allowed to equip a gearset. The full picture is in `architecture.md`.
