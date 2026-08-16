# Changelog

All notable changes to Gearbook are recorded here. The format follows Keep a Changelog and the
project adheres to Semantic Versioning.

This file is for whoever works on the plugin. The release notes embedded in the plugin are for
whoever plays with it, and the description in the plugin index is for somebody deciding whether
to install at all. The three describe the same releases in different words and none is generated
from another; a test only checks that they agree on which versions exist.

A withdrawn release stays here, marked as withdrawn, rather than being deleted.

## [Unreleased]

## [1.0.0] - 2026-08-16

The first public release. Everything below came out of playing with `0.1.0` in a running client,
which is the only way most of it could have been found: the plugin was complete on paper and
wrong in about a dozen places that only show up under a hand.

Major rather than `0.2.0` because the interface a player learns is settled now, and because one
entry below removes something a global rule requires. A version that other people install is
worth saying out loud.

### Added

- **A view switcher on the bar.** The first tile opens a strip that swaps the bar between the
  favourites, everything, one role, one category, or one of your own tags. Choosing is a press on
  a picture rather than a walk to a settings window.
- **Sorting the game's own gearset list**, on request and never by itself. It uses the same
  operation the game's window performs when you drag an entry, puts your list into the order
  Gearbook shows, keeps the order it found so you can have it back, and reads every move back out
  of the game before making the next. It asks the same question the equip gate asks before it
  starts and again before every move, so it will not begin while you are crafting, gathering,
  fishing, casting, in combat or in a cutscene, and stops cleanly if any of those begins mid-run.
  That check is not politeness: the game answers a reordering it will not take by doing nothing
  at all, which read back is indistinguishable from a move landing somewhere unexpected.
- **The game's own role symbols**, established by reading the icon files out of the installed
  client and cross-checked against a mature plugin that has shipped the same numbers for years.
  Crafting and gathering have no such symbol in the game at all, so theirs are assembled at
  drawing time from a coloured ground and a tool.
- **The role's colour under the pointer.** Blue for a tank, green for a healer, red for damage,
  in the colours the game uses in its own party list.
- **Multi-select with bulk editing** in the library, plus select-all, so tagging thirty gearsets
  is one gesture rather than thirty.
- **Shortcuts to Artisan and to Ice's Cosmic Exploration** at the end of the bar, off by default.
  They appear only where they are useful: Artisan with the crafting views showing, the cosmic one
  with crafting or gathering and only while you are standing in that content, which the game's
  own territory table is asked about rather than a list of zone numbers. Pressing one opens that
  plugin's own window through the host's supported call; nothing is typed or sent on your behalf.
- **Sorting by role**, in an order you arrange yourself, and within each role an order for the
  jobs. Only within: the role separates first, so no arrangement can put a healer among your
  tanks, which is why the setting is presented per role rather than as one long list. Untouched,
  the jobs take the position the game's own character window gives them, read from its job table
  so a job added in a later patch files itself in without a line changing here.

### Changed

- The bar and the library share one order. There used to be a setting on each, they could
  disagree, and a bar left on "by job" while the library was grouped by role read as sorting that
  did not work. What is left on the bar is the one question the library cannot answer: whether
  your own arrangement of the favourites still wins.
- The favourite mark and "on the bar" became one thing. The rule that joined them could not be
  explained, which is usually the sign that there is only one idea present.
- The default icon size is 30 rather than 40, which is what the bar wanted once it had been seen
  beside the game's own hotbars.
- The bar keeps itself inside the screen. Icons per row is a wish now: honoured where it fits,
  cut where it is too wide, widened where it is too tall, and nothing is ever dropped.

### Fixed

- Gatherers were filed as crafters. The job table restarts its numbering for them, and every test
  covering it had been written from the same wrong assumption, so the suite agreed with the
  defect. Found by reading the real table out of the installed game.
- The role order grew by eight entries on every load, because a serialiser appends to a collection
  that already holds items. It had reached eight copies of every role before anybody opened the
  settings window.
- Incomplete gearsets are no longer refused. The game offers a substitute and lets you decide;
  refusing took away something its own window gives you.
- A locked bar can be unlocked again from any icon, which it could not before without editing the
  configuration by hand.
- Tooltips no longer flash past as a tall narrow column before settling. They wrapped at the
  window's edge, and a tooltip has no width on its first frame.
- Twenty-five translated texts were declared, translated and never drawn. A test now fails the
  build on any unused key, and it was proved to fail by planting one.

### Removed

- The entry beside the clock in the server info bar, which `rules/frameworks/dalamud.md` requires
  of every plugin. This is a knowing departure from a global rule, taken by the owner, recorded in
  `decisions.md` with both sides of the weighing, and it stands until the foundation grows an
  exception for plugins whose primary surface is permanently on screen.

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
