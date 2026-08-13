# Changelog

All notable changes to Gearbook are recorded here. The format follows Keep a Changelog and the
project adheres to Semantic Versioning.

This file is for whoever works on the plugin. The release notes embedded in the plugin are for
whoever plays with it, and the description in the plugin index is for somebody deciding whether
to install at all. The three describe the same releases in different words and none is generated
from another; a test only checks that they agree on which versions exist.

A withdrawn release stays here, marked as withdrawn, rather than being deleted.

## [Unreleased]

## [0.1.0] - 2026-08-13

The first version.

### Added

- `Gearbook.Core`, a project that references no platform assembly, so the logic that can be wrong
  in an interesting way is testable without a running game.
- The reconciler that keeps favourites, tags and notes attached to the right gearset across
  renames, reordering and deletion. Four staged matches, each accepting only unique pairings,
  then a reported slot-order fallback for sets that genuinely cannot be told apart. Unmatched
  records are kept as orphans rather than deleted.
- The filter engine: free text across name, job, tags and notes, roles, categories, tags,
  favourites, completeness, bar membership, glamour plate links, and time since last use. Three
  filter levels that change how much of the panel is drawn without discarding what is stored.
- Saved views, with the naming rules as pure functions.
- The bar order, kept contiguous from zero so that moving an entry is unambiguous.
- The settings model with a layout version, a migration step per version, and the helper that
  corrects a default nobody chose while leaving a chosen value alone.
- Localisation with German and English catalogues, a resolver that follows the host by default
  and keeps an explicit choice, and tests that fail the build when a key is missing from either
  language or present in a catalogue without being declared.
- Release notes as structured data per language, with a tolerant version comparison that never
  throws.
- The best-in-slot payload parser, ahead of the integration itself, so that nothing has to be
  rebuilt when the sibling plugin offers its interface.
- The plugin project: the four windows, the adapters over the platform, the single component
  that is allowed to equip a gearset, the text commands and the server info bar entry.
- `build.ps1`, running the same gates CI runs and staging a build the host can load.
- CI mirroring the git hooks, plus the build, the tests, the format check, the developer-tools
  guard and the vulnerable dependency scan.
