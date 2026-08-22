# Project rules: Gearbook

Purpose: project-specific rules that extend or tighten the global rules.

> These rules may only add to or tighten the global rules. They may never weaken, contradict, or
> override them (M-01, M-04). A project rule that contradicts a global rule is invalid and must be
> removed.

Nothing here restates a global rule. Where a global rule already says something, this document is
silent about it on purpose: two full statements of the same fact drift apart, and the one nobody
is looking at is the one that goes wrong.

## Added prefixes

- `GB` for this project's own rules. The global prefixes are defined in `rules/06-numbering.md`.

## Rules

- **GB-01 Commit messages follow Conventional Commits.** A type, a colon, then the imperative
  summary: `feat:`, `fix:`, `docs:`, `chore:`, `test:`, `refactor:`, `build:`, `ci:`. This tightens
  R-11, which asks only for a short imperative summary. The reason is the changelog: three
  audiences read three documents about the same release (D-12), and a typed history is what makes
  writing the first of them a sorting job rather than a memory test.

- **GB-02 Everything persisted is per character.** Gearsets belong to a character, so favourites,
  tags, notes, views, the bar layout and the display settings do too, including the ones that look
  global such as the language choice and the filter level. The key is the character's own content
  id. No other character's identifier is read or stored, in any form.

  The reason for including the settings that look global: a player who runs a crafter on one
  character and a raider on another wants different filter levels, and a setting that follows them
  across characters is a setting they have to change twice a session. The cost is that a new
  character starts at the defaults, which is what P-04 requires those defaults to be good enough
  for anyway.

- **GB-03 The draw callback reads only this plugin's own snapshot.** Not game memory, not through
  a null-checked accessor, not "just this one pointer". Reads happen on the framework thread into
  an immutable snapshot, and a click records an intent rather than calling anything.

  This tightens the framework profile, which requires game functions to be called from the
  framework thread. That rule permits a read from the draw callback as long as it is guarded. This
  one does not, because the guard is the part that gets forgotten during a hot reload, when the
  window is open and the addon is being torn down underneath it.

- **GB-04 Nothing persisted identifies a gearset by its slot number alone.** The slot is the number
  the game shows and the game's own "change number" reassigns it. A saved record carries this
  plugin's own id, and the slot is only ever one input to recognising it again.

  Without this the failure is silent and unrecoverable from the player's side: reorder two sets,
  and the notes and favourites swap. It looks like data loss rather than like a bug, and by the time
  it is noticed the correct mapping is gone.

- **GB-05 Product text is German and English; development artefacts stay English.** German is the
  language the product texts are written in and English is the technical fallback, which is the
  language a missing key resolves to. C-02 permits a project to set the language of user-facing
  product content and forbids touching the language of development artefacts, so code, comments,
  documents, the changelog and commit messages remain English regardless.

- **GB-06 No layout reserves space for the BiS integration.** The badge occupies space only when
  there is data to put in it: no empty box, no placeholder, no message, no column that is present
  but blank. The majority of players will never install Eorzea Arsenal, and the common failure with
  an integration like this one is building the interface around the extra information, after which
  the plugin looks broken to everybody who does not have it.

- **GB-07 Between releases, features collect on a `release/<version>` branch rather than landing on
  `main` one at a time.** A feature still gets its own branch and its own pull request, but that
  request targets the release branch, not `main`. `main` only receives the release branch itself,
  as one pull request, when the version is actually cut.

  This extends R-16 rather than falling short of it (R-01's branching section permits a project
  branching strategy on that condition): every change still reaches `main` through a pull request
  with the required checks on the merged result, there is simply one more branch in front of it.
  Nothing about the release procedure in `release.md` changes; the release branch is what gets
  merged to `main` in its step 6.

  The release branch is not protected the way `main` is, so a pull request into it is not blocked by
  a failing check, only informed by one: both `ci.yml` and `content-checks.yml` already trigger on
  every push and every pull request regardless of branch, so the same gates run and report, without
  needing a change to either workflow.

  Rationale: several features are meant to ship together in the next version, and merging each one
  straight to `main` would mean either releasing after every single change or leaving `main` sitting
  ahead of the last tag for a stretch, both of which the owner would rather avoid. Renaming the
  branch if the version it targets turns out wrong (a patch growing into a minor, say) is one `git
  branch -m` and a force-push of a branch nobody else is tracking, so naming it early costs nothing
  worth guarding against.
