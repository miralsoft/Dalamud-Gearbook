# Open points: Gearbook

Purpose: open questions and unresolved items. Distinct from `todos.md`, which is planned work.
These still need a decision or a verification. Each entry names who it waits on and since when, so
an item cannot sit unanswered without that being visible.

## Answered by the first session with the game running (2026-08-15)

Kept rather than deleted, because what was checked is as useful to the next reader as what is
still open.

- **The plugin loads, the bar draws, and a gearset actually changes.** Observed switching to a
  black mage set and to a dark knight set from the bar, with the game's own log confirming both.
- **`EquipGearset` with a glamour plate argument of zero works** for a set that is not linked to
  a plate. The linked case is still open, below.
- **The slot numbering lines up.** The bar's order and item levels matched the game's own gearset
  list entry for entry, which it could not do if the index were off by one.
- **German reaches the interface**, including the settings window and its tabs.
- **The job icons are the game's own**, so the bar looks like the hotbar it replaces.

## Waiting on a running game, not on a decision

- **(open since 2026-08-13) What does the second parameter of `EquipGearset(int, byte)` expect
  when a gearset is linked to a glamour plate?** The entry carries `GlamourSetLink`, and there is a
  value that means "no plate", but which one is not established. Reflection gives the signature and
  not the semantics, so this is checked in the game before it is written into the adapter. Guessing
  it wrong either applies the wrong glamour or ignores a link the player set up.

- **(open since 2026-08-13) Is `GearsetEntry.Id` zero-based or one-based?** The game's list shows
  numbers from 1. Whether the field matches that or is offset by one decides whether the number
  shown in the library window is the number the player sees. The library currently adds one to
  the slot before displaying it, on the assumption that the field is zero-based; comparing the
  two lists side by side answers it in a second.

Both `JobClassifier` questions that sat here were closed on 2026-08-15 by reading the real job
table out of the installed client. One of the two constants was wrong and had already reached a
player. See the entries of that date in `decisions.md`.

- **(open since 2026-08-15) The role symbols on the bar's view switcher.** The game has its own
  role icons, the ones its character window draws beside Verteidiger, Heiler and the rest, and
  they would look more at home than the host's symbol font that is used instead. Their icon
  numbers could not be established from outside a running client, and a picture guessed wrong is
  worse than one that is merely plainer, because a wrong one looks deliberate. Establish them
  with the client in front of you and swap them in; the switcher already has one place where the
  symbol per view is decided.

## Waiting on the owner

- **(open since 2026-08-13) The icon.** Needed before the first release, not before the first
  commit. Exactly 512 by 512 and square, or Dalamud drops it in favour of the default without
  reporting anything. Dalamud stamps its own status badges over the bottom right corner, so nothing
  essential goes there. Judge a candidate by rendering it at the size it is actually drawn at, in
  colour and greyed out, before accepting it.

- **(open since 2026-08-13) Whether the repository gets branch protection.** R-16 requires every
  change to reach `main` through a pull request from the first public release onward. The
  repository is public, so protection can be configured. Until it is, the rule binds and nothing
  enforces it, and R-16 requires a project that cannot carry the protection to say so. This entry
  is that statement until the protection exists.

## Open work in other repositories

Decided here, done elsewhere. Recorded so it is not lost between repositories, and it stops here:
an agent changes exactly one repository, this one (M-18). Somebody working in the other repository
picks these up.

- **(open since 2026-08-13, `Dalamud-Eorzea-Arsenal`) The shape of the BiS call gate.** The original
  proposal was `EorzeaArsenal.GearsetBis.V1` as `Func<uint, string?>`, taking a gearset index and
  returning a small JSON string or null. Three pieces of feedback from this side, all of which the
  owner carries over; none of it is implemented anywhere yet.

  **One call instead of one per gearset.** Ninety gearsets means ninety IPC calls, and they happen
  at exactly the moment a window opens, which is the moment it is felt. Proposed shape, no
  arguments, one JSON string back:

  ```jsonc
  {
    "state": "ok",          // ok | noaccount | loading | nodata
    "entries": [
      { "id": 4, "matched": 14, "total": 16, "target": "Ultimate BiS" }
    ]
  }
  ```

  Still only numbers and short strings across the boundary, so no equipment information crosses it
  and the argument for the original design is preserved.

  **A state field, because `null` conflates four things:** no account, no BiS for that job, unknown
  gearset, not loaded yet. For a single tile all four correctly mean "no badge". For every tile at
  once they do not: without a state, an empty column cannot be told apart from an absent account,
  and a feature that switches itself off has to say so somewhere the player can find it.

  **A note in the contract that the slot number is not a durable key.** For a single call it is
  correct, both sides read the same module in the same moment. The caching obligation is what makes
  it dangerous, and handling that is this side's job, not Arsenal's. It belongs in the contract
  anyway so that a later version does not treat the index as an identity.

- **(open since 2026-08-15, `miralsoft-foundation-docs`) The server info bar rule needs an
  exception, or this project stays out of compliance with it.** `rules/frameworks/dalamud.md`
  requires every plugin to carry an entry in the server info bar or an icon at the minimap.
  Gearbook now carries neither, by the owner's decision, recorded in full in `decisions.md` on the
  same date.

  The rule was written for a plugin with no permanent presence, where an entry beside the clock is
  the only way back. It does not fit a plugin whose primary surface is a bar that is on screen all
  the time and whose every icon opens a menu leading everywhere else. A sentence covering that
  case would let a plugin of this shape comply honestly instead of deviating.

  Whoever writes it should also decide what the exception depends on, because the obvious
  candidate is fragile: "the plugin has a permanently visible surface" stops being true the moment
  a player switches that surface off, and a rule whose condition the player can turn off is a rule
  that quietly stops applying. This repository cannot make that change (M-18), and until it is
  made, the deviation stands and is named as one.

- **(open since 2026-08-13, `Dalamud-Plugins`) The index entry for Gearbook.** Before the first
  release this repository must be public with a published release carrying the packaged archive
  under the asset name the index looks for. Then two things are added in the index repository: the
  entry in its list, **and** the descriptive section in its README linking back here. Both. Without
  the second, the release is invisible to anybody who does not already know the plugin exists,
  which is the same as not having released it. Neither is written from here.

## Notes

- **(noted 2026-08-13) The gearset limit is 100 and this owner is at 33.** The library window shows
  the count for that reason. Worth remembering when designing anything that creates gearsets, which
  currently nothing does.
