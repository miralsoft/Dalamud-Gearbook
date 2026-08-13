# Open points: Gearbook

Purpose: open questions and unresolved items. Distinct from `todos.md`, which is planned work.
These still need a decision or a verification. Each entry names who it waits on and since when, so
an item cannot sit unanswered without that being visible.

## Waiting on a running game, not on a decision

- **(open since 2026-08-13) What does the second parameter of `EquipGearset(int, byte)` expect
  when a gearset is linked to a glamour plate?** The entry carries `GlamourSetLink`, and there is a
  value that means "no plate", but which one is not established. Reflection gives the signature and
  not the semantics, so this is checked in the game before it is written into the adapter. Guessing
  it wrong either applies the wrong glamour or ignores a link the player set up.

- **(open since 2026-08-13) Is `GearsetEntry.Id` zero-based or one-based?** The game's list shows
  numbers from 1. Whether the field matches that or is offset by one decides whether the number
  shown in the library window is the number the player sees.

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
