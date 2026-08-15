# Decisions: Gearbook

Purpose: append-only decision log. Never edit or delete past decisions, only add new ones. A later
decision may supersede an earlier one, but the history stays (M-08). The paths that were rejected
are recorded with the reason, not only the ones that were taken (M-15).

Each entry: date, decision, rationale, and what was rejected.

## Log

- **(2026-08-13) Gearbook is a standalone plugin, not a feature of Eorzea Arsenal.** Rationale: a
  gearset switcher needs no account, and Arsenal is at its core the client of a service. Building
  this into Arsenal would force everybody who wants a better selection to install a plugin that
  asks them for a web account, which most will not do. Binding a tool with a broad audience to a
  bottleneck it does not need is the expensive mistake here and is hard to undo later. Two further
  reasons: Arsenal's release rhythm hangs on its server contract, so a stuck filter would either
  wait for that or force a server release; and Arsenal is already large. Rejected: building it into
  Arsenal, and the older counter-argument that users will not install a second plugin, which the
  shared plugin index has since made false.

- **(2026-08-13) The plugin works completely without Eorzea Arsenal, without an account and without
  an internet connection.** Rationale: this follows from the decision above and is stated separately
  because it is the one that constrains every later feature. There is no function that requires a
  connection. None.

- **(2026-08-13) Named `Gearbook`.** Rationale: checked against all 480 entries of the official
  API-15 plugin index; the word "book" appears in no plugin name at all, so it is distinctive in a
  list where "gear something" is not. The internal name lowercased is `/gearbook`, so the command
  needs no deviation from the blueprint. And the metaphor covers the whole feature set rather than
  just the switching: a book has bookmarks (favourites), margin notes (notes), a register (filters)
  and tabs (saved views).

  Rejected: anything containing "Helper", because `GearsetHelperPlugin` by KhloeLeclair is in the
  official index and the display name "Gearset Helper" is effectively taken; a collision there does
  not look untidy, it makes a plugin impossible to install, and the failure surfaces in the client
  log where nobody looks. Rejected: `Eorzea Quartermaster`, which fit the sibling plugin's family
  and the act of issuing equipment precisely, but would have cost a deviation from the blueprint's
  command convention and a name nobody wants to type. Rejected: `Gearset Palette`, whose only
  advantage was being found by somebody searching for "gearset", which the manifest's own search
  terms cover independently of the name. Accepted cost: Gearbook and Eorzea Arsenal do not sort
  next to each other in the plugin list.

- **(2026-08-13) The repository is `miralsoft/Dalamud-Gearbook`, renamed from
  `Dalamud-Gearset-Helper`.** Rationale: done before the first commit, when it is one click. After
  the first release it is a move, and the old name echoed a competing product.

- **(2026-08-13) Licence AGPL-3.0-or-later.** Rationale: the standing proposal for plugins of this
  kind, put to the owner rather than set quietly, and accepted. The failure this ecosystem actually
  sees is somebody taking an open plugin, renaming it, and putting it behind a paywall, which a
  permissive licence expressly allows. Accepted cost, recorded so it is not a surprise later: nobody
  can lift parts of this into a closed plugin, and relicensing would need the agreement of every
  accepted contributor.

- **(2026-08-13) Committer identity `Sanaka`.** Rationale: a private project, so not the company
  name (R-03, R-19). `.miralsoft-enforcement` matches the declaration in `project.md`.

- **(2026-08-13) German and English, with English as the technical fallback.** Rationale: German is
  the language the texts are written in, English is what a missing key resolves to and what the
  completeness tests compare against. Keeping English as the fallback is the blueprint's convention,
  so no deviation is recorded. Layout is still checked in the longest shipped language, which in
  practice will be German. Rejected: making German the technical fallback, which would have been a
  convention deviation and would have shown German text to an English player when a translation
  slipped through.

- **(2026-08-13) The active language follows Dalamud's own interface language by default, and an
  explicit choice in the plugin wins and keeps winning.** Rationale: required by the framework
  profile. Worth recording because the original briefing said the language should follow the plugin
  setting "and not the game client", which is not a contradiction: Dalamud's interface language is
  not the game client's language, so following the host satisfies both.

- **(2026-08-13) The plugin assigns its own stable id per gearset and re-derives the mapping on
  every load.** Rationale: the game offers nothing stable. `GearsetEntry.Id` is the displayed number
  and the game's own "change number" reassigns it; the name is free text and can repeat, including
  for the same job, which this owner's setup actually does. The reconciler matches in stages, most
  certain first, keeps unmatched records as orphans rather than deleting them, and logs an ambiguity
  rather than resolving it silently.

  Rejected: keying on the slot number, which breaks the first time anything is reordered and takes
  favourites and notes with it, silently. Rejected: keying on job plus name, which breaks on a
  rename and is ambiguous for this owner's duplicate sets. Rejected: deleting records that no longer
  match, because a failed match and a deleted gearset look identical and only one of them should
  cost the player their notes.

- **(2026-08-13) Switching is done through `RaptureGearsetModule.EquipGearset`, not through the
  game's gearset text command.** Rationale: established by reflecting over the installed Dalamud
  15.0.3 with FFXIVClientStructs 7.51.0, not from memory. The direct call is one function reached
  from one component, whereas the text command route means composing a command and handing it to
  the game's own input processing, which is a wider surface and closer to the line the framework
  profile draws around operating a game window.

- **(2026-08-13) Exactly one type may equip a gearset, and nothing else holds an instance of it.**
  Rationale: the framework profile requires one gate to the server. This records the shape it takes
  here, so that the automation boundary can be reviewed by reading one file and a reviewer can prove
  a negative.

- **(2026-08-13) A switch that cannot happen now is reported and dropped, never queued.** Rationale:
  the tile greys out and the tooltip says why. A switch that fires later by itself is exactly the
  automatic interaction with the game servers that the framework profile forbids, and it would look
  like a convenience while being the one thing the plugin must not do.

- **(2026-08-13) All persisted state is per character, including the settings that look global.**
  Rationale: recorded as GB-02 with its reasoning. Rejected: keeping the language choice and the
  filter level global, which was the original proposal and was overruled because a player with a
  crafter on one character and a raider on another would have to change them twice a session.

- **(2026-08-13) Four windows rather than the blueprint's three.** The blueprint names main,
  settings and release notes as a convention. Gearbook adds a fourth, the bar, because the bar and
  the library are genuinely different objects: one is a permanently visible control surface with no
  title bar, the other is a management view opened rarely. Folding them into one window would mean
  a single window that is either too heavy to leave on screen or too thin to manage a hundred
  gearsets. This is a convention deviation and costs only this line.

- **(2026-08-13) The long command is `/gearbook`, the short form `/gb`.** Rationale: the internal
  name in lowercase, so the blueprint's convention holds with no deviation. The short form is
  registered and allowed to fail, because another plugin may already own it; the failure is logged,
  not treated as an error, and teardown only removes what was actually claimed.

- **(2026-08-13) The foundation's `content-checks.yml` is taken as its own workflow rather than
  pasted into the build job.** Rationale: the foundation's own README says pasting is the right
  answer where branch protection names a single required check by job name, and this repository has
  no such constraint yet. As a separate workflow it stays a straight copy, which makes re-copying it
  when the declared foundation version is raised a replacement rather than a merge. Consequence to
  honour when protection is switched on: "Content checks" has to be added to the required status
  checks, or it reports without blocking.

- **(2026-08-13) The BiS integration is designed now and built later.** Rationale: the interface,
  the model and the payload parser exist from the first version so nothing has to be rebuilt, but
  no call is made, because the other side does not offer the gate yet. Building the plugin alone
  first also means the information it eventually shows is chosen from use rather than guessed, and
  no interface is created on the Arsenal side on suspicion. The proposed contract and the feedback
  on it are in `open-points.md`, marked as belonging to that repository (M-18).

- **(2026-08-13) Globally bound keyboard shortcuts are deferred.** Rationale: they compete with the
  game for input focus, which is the part of a Dalamud interface most likely to misbehave, and the
  bar plus the commands already cover the need that motivated the project. Recorded rather than
  dropped, so the next session does not rediscover it as a gap.

- **(2026-08-13) The bar falls back to favourites, and then to the current filter, when nothing
  has been arranged on it.** Rationale: a bar that starts empty and stays empty until its owner
  reads an explanation is a bar nobody keeps, and P-04 requires the shipped defaults to be the
  working configuration. Putting a gearset on the bar explicitly still wins over both fallbacks,
  so arranging it is never undone by this. Rejected: starting empty with a hint, which is honest
  but means the first impression of the product is a blank rectangle.

- **(2026-08-13) `GearsetRecord` has its equality written out rather than generated.** Rationale:
  a record's generated equality compares the tags by reference, and a record read back from the
  configuration never shares a list with the one that wrote it, so anything asking whether it had
  changed would always have answered yes. Found by a round-trip test that failed for a reason
  that had nothing to do with what it was testing. The cost is a field list written twice, which
  is why the round-trip test now covers it.

- **(2026-08-13) A command that matches several gearsets refuses rather than picking one.**
  Rationale: several sets sharing a name is this product's whole reason for existing, so guessing
  there would be the one place it does exactly what it was built to avoid. The message says to
  use the number instead, which is unambiguous.

- **(2026-08-13) Two values in the job classification could not be derived from a flag and are
  named constants.** Rationale: the job table gives physical and magical ranged jobs the same
  role, and gives crafters and gatherers the same role as the starting classes. The constants
  carry the reason at the point where they are set. What matters is the failure mode chosen: when
  a constant is wrong the job lands under "Other" rather than under a confidently wrong role, so
  the mistake is visible in the filter rather than silent. Rejected: a hardcoded list of job ids,
  which would be correct today and wrong the first time a job is added.

- **(2026-08-15) Gearbook ships no entry in the server info bar. This is a deliberate departure
  from a global rule, not from a blueprint convention, and it is recorded here as one.**

  The rule, in `rules/frameworks/dalamud.md` under "What every plugin ships": *"Reachability
  without a command. An entry in the server info bar or an icon at the minimap. Somebody who does
  not know the command otherwise never finds the plugin again."* Gearbook now has neither.

  **The owner's reasoning, which is the reason this was decided the way it was.** Gearbook is not
  shaped like the plugin the rule was written for. That plugin has no permanent presence: its
  windows are shut, nothing of it is on screen, and without an entry beside the clock there is
  genuinely no way back. Gearbook's whole purpose is a bar that is on screen all the time, and
  every icon on that bar carries a right-click menu holding the library and the settings. Spending
  a slice of a bar shared with the clock, the world name and every other plugin, in order to
  provide a second way in for a product whose first way in is always visible, is a cost paid for
  nothing.

  **The case against, stated in full rather than summarised away.** The bar can be closed, hidden
  in combat, and switched off at start, and each of those is one click. In any of those states
  the owner's justification stops being literally true, and somebody who does not know `/gearbook`
  is then down to one route: the open button in Dalamud's own plugin installer, which this plugin
  does wire up. That route is real and always present, but it is outside the game's interface and
  the rule exists precisely because people do not think to look there. The rule is also not a
  suggestion: M-01 says a project may tighten a global rule and never weaken one, so unlike a
  blueprint convention this cannot be settled by a line in this file alone.

  **What follows from that.** The code change is the narrow one: the entry and its setting are
  gone, nothing else. The rule itself needs an exception written into the foundation, for the case
  of a plugin whose primary surface is permanently on screen, and that belongs in the foundation
  repository which this one never writes to (M-18). It is recorded in `open-points.md` as work
  belonging there. Until that happens this project is knowingly out of compliance with one rule,
  which is a different and more honest state than believing itself compliant.

  Rejected: keeping the entry at two characters with a switch to hide it, which was built first
  and would have satisfied the rule by default while costing almost no room. The owner judged
  that even that was more than the feature is worth here, and the reasoning above is why.

- **(2026-08-15) Crafters and gatherers are separated by the job's category row, not by its
  position in the hand-and-land sequence.** Rationale: the first attempt split them at index
  eight, on the reasoning that the eight crafting jobs come first. They do, but the sequence
  **restarts at zero for the gatherers**: a blacksmith and a botanist both sit at index one. Every
  gatherer was therefore filed as a crafter, and a player noticed before any test did, because
  every value in the test had been invented to match the assumption.

  Fixed by reading the real job table out of the installed client rather than reasoning about it
  again. The category row is the game's own grouping, 33 for Disciple of the Hand and 32 for
  Disciple of the Land, and it separates them exactly. The same reading confirmed the two
  attribute constants for the physical and magical ranged split, which were right.

  The lesson is not about this table. Both the wrong value and the right one were reachable the
  same way, by reading the data instead of the documentation, and one of them was reached only
  after it broke. Rejected: hardcoding the job row ranges, which is correct today and wrong the
  first time a job is added.

- **(2026-08-15) Every window reaches every other from its title bar, and the buttons that did
  the same job in the library's content area are gone.** Rationale: the blueprint puts cross-links
  in the title bar because that is where the host puts its own controls and therefore where a
  player already looks. Having them in both places was two answers to one question. The link to
  the release notes carries the unread state, re-evaluated every frame, because after the notes
  appear once by themselves it is the only signal that there is anything to come back to.

  One honest limit: a locked bar has no title bar, so its two links disappear with it. That is
  what locking is for, and both places stay reachable from the right-click menu.

- **(2026-08-15) A gearset carries the same right-click menu wherever it appears.** Rationale:
  marking a favourite decides what the bar holds, and reaching that through a window is a detour
  from the thing the player is already pointing at. The same menu on a bar tile and on a library
  row, so learning it once is enough.

- **(2026-08-15) A gearset missing a piece is no longer refused. This supersedes the entry of
  2026-08-13 that made incompleteness a blocking reason.** Rationale: the first session with the
  game running showed what the game itself does in that case. It does not refuse. It opens a
  dialog naming the missing piece, offers a suitable substitute already in the armoury, and lets
  the player answer. Blocking the switch therefore took away something the player has in the
  game's own window, which is the worst kind of helpfulness: a restriction invented by a tool
  that exists to remove friction.

  Nothing here touches that dialog. The plugin calls the same function the game's own gearset
  window calls; the dialog appears because the game put it there, and the player answers it. That
  keeps this on the allowed side of the platform's restriction about operating game windows,
  which the earlier caution was not needed for in the first place.

  The set is still marked: the item level is drawn in the warning colour, and the tooltip and the
  detail panel say that a piece is missing and that the game will offer a replacement. The key
  behind that sentence was renamed from `switch.blocked.incomplete` to `gearset.incomplete`, so
  that the name describes a state rather than a refusal and nobody reintroduces the block by
  reading it. Rejected: keeping the block behind a setting, which would have made a wrong default
  configurable rather than fixing it.

- **(2026-08-15) The bar's default icon size is 30 rather than 40, with a migration.** Rationale:
  forty was chosen before the bar had been seen in a real client, where it sits noticeably larger
  than the game's own hotbars. The layout version goes to 2 and the step corrects the stored value
  only where it still stands at exactly the old default, which is the test for whether anybody
  ever expressed an opinion about it. A slider that was moved is a decision and stays the player's.
  This is the first real use of the migration mechanism, which existed with one version and no
  steps until now.

- **(2026-08-13) Item level is read from the gearset entry, not computed from its equipment.**
  Rationale: `GearsetEntry.ItemLevel` exists and is the number the game itself shows in the gearset
  list, established by reflection. The alternative was reading fourteen equipment slots per set and
  averaging, which was expected to be the most expensive read in the plugin and would have needed a
  cache with its own invalidation. It turned out not to be necessary at all.
