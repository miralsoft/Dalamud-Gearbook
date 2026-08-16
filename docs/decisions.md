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

- **(2026-08-15) The view switcher uses the game's own role symbols, read out of the client rather
  than remembered.** Rationale: the symbol font the switcher borrowed from the host was legible but
  foreign. A shield, a raised fist and a wizard's hat are somebody else's idea of what a tank, a
  melee job and a caster look like, and beside the game's own job icons on the same bar they read
  as a second product's icons pasted onto this one. The game draws these roles itself, all day, in
  windows the player already knows.

  The numbers are 62581 for tank, 62582 for healer, 62584 for melee damage, 62586 for physical
  ranged damage, 62587 for magical ranged damage, 62588 for the hand and 62589 for the land, with
  62576, the three role colours stacked, for the combat category. They were established by reading
  the icon files out of the installed client with Lumina and looking at them, the same method that
  settled the job table earlier this day, not from memory.

  The ordering is the part worth recording, because that is where such a table goes wrong quietly.
  The group is marked by the game itself: 62570 is a label reading "ROLE BASE" and 62580 one
  reading "ROLE FRAMED". Within it, 62585 turns out to be the pictures 62586 and 62587 drawn on top
  of one another, which is only sensible if it is the general case covering both. That fixes the
  whole sequence as running from general to specific, and with it the two entries the plugin
  deliberately leaves unused: 62583, damage of any kind, and 62585 itself, ranged damage of either
  kind. This plugin separates physical from magical everywhere else, so neither has anything here
  to label.

  Rejected: guessing the numbers, which the earlier open point had already refused for the right
  reason. A wrongly guessed picture is worse than a plain one because it looks deliberate, and
  nobody re-examines a symbol that appears to have been chosen.

  Also rejected: borrowing a game symbol for the favourites, the everything and the tag views.
  The game has stars and grids, but each already means something else in it, and a player who
  knows what a symbol means there reads it as that here too. Those three keep a plain glyph, which
  says less and misleads nobody. The mixed styles inside one strip are the price, and the strip is
  already divided into groups by separators, so the seam falls where a seam belongs.

  The mapping lives in the core library rather than in the plugin, because an icon number is a
  number and needs no platform assembly, which keeps it under test. The tests cannot tell whether
  a number draws the right picture, only the client can; what they hold is that no two roles share
  a symbol and that the combat category borrows none, which is the failure a copied line produces.

- **(2026-08-15) Gearbook can sort the game's own gearset list, on request and never on its own.**
  Rationale: a player who has built up thirty gearsets over years has a list whose order is
  whatever history left behind, and the game offers no way to sort it except dragging entries one
  at a time. The function behind that dragging, `RaptureGearsetModule.ReassignGearsetId`, is
  callable, and using it puts the list into the order Gearbook already shows.

  It sits on the allowed side of Dalamud's automation boundary, and the boundary was read before
  the design rather than after it. What is forbidden is interacting with the game **automatically**;
  this runs only when a player presses a button, performs the same operation that player could
  perform by hand, and asks the server for nothing it would not accept from normal play. There is
  no sort on login, none after a gearset is created, and no setting to add one. That absence is
  the feature, not an omission: a list that rearranges itself is exactly what the boundary is
  about.

  It gets its own gate, beside the one that equips, for the reason the rule gives: the line has to
  be reviewable by reading one file per kind of thing this plugin does to the game. This is the
  more valuable of the two. An equip is undone by equipping something else; an order built over
  years cannot be typed back in.

  The part worth recording is what is **not** known. The game's reordering call is used without a
  written guarantee of what it does to the entries between the two ends of a move: it might swap
  them, or lift one out and let the rest close up. Rather than guess, the arrangement fills the
  list from the front, which leaves every settled position untouched under **both** meanings, and
  the tests apply the moves under both to prove it. Every move is then read back out of the game
  before the next is decided, and the first surprise ends the run. A wrong assumption applied once
  and caught is a move the player can drag back; applied thirty times it is a list nobody
  recognises.

  The order as it stood is kept before every run, so the previous arrangement can be put back. It
  is stored as job and name rather than as slot numbers, because slots are precisely what the
  sorting changes and a backup written in slots would describe only the list it was taken from.

  Rejected: sorting from the library window with a single button and no preview, which is what was
  asked for and is one keystroke away from an accident on data with no undo. The window shows what
  will move before anything moves, and the same button is still one press.

  Rejected: offering the game's own order as one of the choices. Sorting a list into the order it
  is already in is not an option, it is the absence of one.

- **(2026-08-15) The bar no longer has a sort of its own; it follows the library's.** Rationale:
  there were two controls answering the same question, "in what order", and they could disagree.
  They did: a bar sitting on "by job" while the library was grouped by role looked to the owner
  like sorting that simply did not work, and no amount of reading the code would have found a bug,
  because there was none. The second control was the bug.

  What is left on the bar is the one question the library cannot answer: whether the arrangement
  the player dragged into shape still wins. That is a genuinely different question, it applies only
  to the favourites, and it is now a switch rather than an entry hidden at the top of a list of
  orders.

  Layout version 6 carries the old value across. A bar on the arrangement keeps it. A bar with an
  explicit order loses the control but not the order: it is written into the library's setting,
  though only where the library is still sitting on the value it shipped with. A library order
  somebody set deliberately in the window built for the question is an answer, and the bar's
  retired setting does not get to overwrite it.

  Rejected: keeping both and documenting the precedence. Two controls with a rule about which wins
  is the arrangement that produced this in the first place.

- **(2026-08-15) Tooltips wrap at a width in letters, not at the window's edge.** Rationale: the
  shared text helper wraps at the content region's right edge, which asks the window how wide it
  is. A tooltip on its first frame has no answer yet, so every line broke against a provisional
  width and the tooltip appeared for one frame as a tall narrow column before settling. From the
  outside it read as a second dialog flashing past, which is exactly how it was reported.

  The fix is to say the width in letters, once, around the whole tooltip, and to have the pieces
  inside inherit it. The wrapping helper keeps its window-relative behaviour, because in a window
  that already has a width that is the right answer and the one every settings caption needs.

- **(2026-08-16) Crafting and gathering are drawn as an anvil and a pickaxe, not as the last two
  entries of the role block.** Rationale: the role block ends with a pair that the ordering says
  is the hand and the land, and they were used first for that reason. They draw four metal discs
  and three nuggets on a green ground, and nobody reading the bar could tell what either meant.
  A symbol that has to be explained has already failed at the only thing it does.

  The owner said so twice, which is what moved this. The second time came with pictures of what
  the game supposedly uses, a purple tile with an anvil and a gold one with a fish, and those sent
  the search somewhere useful even though it came back empty.

  What the search established, and it is worth keeping because it closes the question rather than
  postponing it: every icon between 0 and 79999 in a plausible size was measured by the colour
  just inside its frame, looking for those two tiles. They are not in the game's icon folder. The
  game also marks each of its icon groups with a label tile it draws itself, `CLASS JOB`,
  `GTR TYPE`, `ROLE FRAMED`, `GEAR SET` and a dozen more, and there is no group for job
  categories among them. The window textures for the gearset list, the character sheet and the
  crafting log were opened and looked at as well. The tiles in those pictures are almost certainly
  a website's own artwork: the small job symbols beside them on the same page are the game's, the
  large ones are on colours the game uses nowhere.

  So the choice was between the game's pictures that exist. 62109 and 62116 are the blacksmith's
  anvil and the miner's pickaxe, framed and on the dark ground the crafting and gathering jobs all
  use, which is the same frame and size as everything else in the menu. Strictly this is a job's
  picture standing in for a whole category, which is the borrowing this project refused for the
  favourites star. It loses here to a plainer point: an anvil reads as making things and a pickaxe
  as digging them up, to anybody, immediately, and the star had a working alternative while these
  two had none.

  Rejected: keeping the role-block pair and explaining it in a tooltip. A tooltip that has to say
  what a picture means is the picture admitting it does not work.

- **(2026-08-16) The search for a crafting and gathering role symbol is closed: there is none, and
  the earlier identification of 62588 and 62589 was wrong.** The owner asked twice more for the
  originals and suggested looking at what other plugins had done, which is what settled it.

  Three findings, each independent of the others:

  **The game's own group labels.** Every icon group is marked with a black tile the game draws
  itself. All 132 of them between 60000 and 79999 were found by measuring for a nearly black
  square holding a few per cent of pale pixels, and then read: `SYSTEM MISC`, `CLASS JOB`,
  `CLASS JOB FRAMED`, `NAME CLASS JOB`, `QUEST CLASS`, `QUEST JOB`, `GTR TYPE`, `LEVE KIND`,
  `ROLE BASE`, `ROLE FRAMED`, `GEAR SET`, `ACHIEVEMENT` and a hundred more. There is no group for
  job categories. That is not an absence of evidence, it is the map being complete.

  **The colour of the two entries at the end of the role block.** 62588 sits on 49, 78, 39 and the
  healer's own ground on 49, 77, 38: the same green, pixel for pixel. The crafting and gathering
  jobs use the dark ground, 62574. Whatever 62588 and 62589 are, they are not the hand and the
  land, and this project's earlier note claiming they were has been corrected rather than left to
  be believed by the next reader. No plugin on GitHub uses either number.

  **What DelvUI does.** Asked for the role icon of a job, it returns 62581 for a tank, 62582 for a
  healer, 62583 for damage, and optionally 62584, 62586 and 62587 for melee, physical ranged and
  magical ranged. That is this project's mapping exactly, arrived at separately, which is a
  cross-check worth more than either derivation alone. Asked for a crafter or a gatherer, it
  returns that job's own icon. The mature plugin hit the same dead end and took the same way out.

  So the anvil and the pickaxe stay, and they are no longer a compromise made in ignorance. They
  are what is left when the thing being looked for has been shown not to exist.

  Recorded because the tempting summary of this session is "we could not find it", and that is
  not what happened. It was found not to be there, which is a different and stronger statement,
  and it means nobody needs to look again.

- **(2026-08-16) The crafting and gathering symbols are assembled at drawing time from two of the
  game's own pictures.** Rationale: the game keeps its role tiles in two halves, an empty framed
  colour from the `ROLE BASE` group and a silver tool drawn on nothing from the `CLASS JOB` group,
  and ships the pairs it wanted. It never made a pair for these two categories. So this one is
  made at drawing time out of the same halves: the dark ground with an anvil over it, and the
  earth-coloured ground with a pickaxe.

  That answers the objection the owner actually raised, which was not that the anvil was ugly but
  that it was a **job's** symbol standing where a **category's** belonged. On its own the anvil is
  the blacksmith. On a ground no single job carries, it is the level above one, and it stops
  looking like a gearset tile that wandered into the menu.

  The grounds deliberately avoid blue, green and red. Those belong to the tank, the healer and
  damage, and on any of them these two would read as a fourth and fifth role; the tricolour is
  already combat. That leaves exactly two grounds for exactly two categories, which is luck rather
  than design, and is worth writing down before somebody adds a third category and finds the
  cupboard bare.

  Rejected: embedding the two pictures the owner found as files in the plugin. Technically it is
  half an hour of work. But this repository is public and the plugin ships through an index, so a
  bundled picture is redistributed with every release, and neither of those two is ours. That is
  the reason the whole ecosystem addresses game art by number and loads it from the player's own
  installation, and it is not a rule worth breaking for a menu icon. The owner chose the assembled
  route once the trade was on the table.

- **(2026-08-16) The version goes to 1.0.0, and the declared foundation version stays at 2.0.0.**

  **The version.** Major rather than `0.2.0` for two reasons. The interface a player learns is
  settled: the bar, the switcher, the library, one order shared between them, one gesture per
  meaning. And the release removes something a global rule requires, which is a breaking change in
  the only sense that matters here even though no caller breaks. A version other people install is
  worth saying out loud, and `0.x` says the opposite.

  **The foundation review (M-17).** Required whenever a release is cut, and done rather than
  assumed: the clone was pulled, `VERSION` still reads 2.0.0, and the changelog's `Unreleased`
  section is empty. Nothing has been added since this project declared its version, so there is
  nothing to review, nothing to re-copy under M-19, and the declaration stays where it is. Written
  down because "there was nothing new" and "nobody looked" are indistinguishable a year later, and
  only one of them is a review.

- **(2026-08-16) The bar can carry shortcuts to Artisan and to Ice's Cosmic Exploration, off by
  default.** Rationale: the owner opens both from macros while crafting, and the bar is already on
  screen at that moment. Dalamud publishes the installed plugins and lets one open another's main
  window through `IExposedPlugin.OpenMainUi`, so this needs no text command typed on anybody's
  behalf, nothing sent to the game, and no agreement with either plugin's authors. The other
  plugin decides what its own button does.

  A shortcut appears only when all three of installed, loaded and "has a main window" hold. A
  plugin that is installed but switched off would give a button that does nothing, which is worse
  than no button.

  **Where** they appear is the part worth stating. Artisan in the crafting views, the cosmic one in
  crafting and gathering and only while the player is standing in that content. A shortcut to a
  crafting plugin among a row of tanks is a tile in the way rather than to hand.

  **The cosmic zones are recognised from the game's own data, not from a list of four numbers and
  not by asking the other plugin.** `TerritoryIntendedUse` is 60 for Sinus Ardorum, Phaenna, Oizys
  and Auxesia, and for nothing else in the game. Reading that mark means a zone added to the
  content in a later patch is recognised with nothing changing here. The owner suggested asking
  Ice, which was the natural thought and was rejected for two reasons that both matter: the answer
  is wanted before deciding whether Ice is even loaded, and an undocumented gate into another
  plugin breaks when that plugin changes something it never promised.

  **No icons.** Dalamud publishes which plugins are installed but not their pictures; the installer
  fetches those from the web, and this plugin does not reach the network at all. One of the two
  ships an icon file inside its own install folder and the other does not, so taking that route
  would have given one shortcut a picture and the other a placeholder, and would have meant reading
  another plugin's files by a path that changes with its version. Two plain glyphs instead.

  Off by default. Somebody who installs a gearset switcher did not ask for buttons to other
  people's plugins on it, and a bar that grows a tile because an unrelated plugin was installed is
  a bar that changed without anybody deciding anything.

- **(2026-08-16) `main` is protected, and the protection includes administrators.** R-16 requires
  every change to reach `main` through a pull request from the first public release onward, and
  that release is now out.

  The settings: pull requests required, `Build` and `Content checks` required and required to be
  up to date with the branch, linear history, no force pushes, no branch deletion, conversation
  resolution required, and **administrators are not exempt**.

  Zero approving reviews are required, which looks like a hole and is not one. This is a
  single-maintainer project; requiring an approval would mean nobody could ever merge, so the
  choice is between zero approvals and no protection at all. What the protection is actually
  buying here is the checks: nothing reaches `main` without the build, the tests, the format gate
  and the content checks passing on the merged result.

  Including administrators is the part worth defending, because excluding them is the usual
  default and would have been easier. Excluded, the rule would read "every change goes through a
  pull request, except the ones by the only person who commits", which is not the rule. The
  escape hatch for a genuinely stuck required check is to edit the protection deliberately, which
  is a visible act rather than a habit.
