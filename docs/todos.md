# To-dos: Gearbook

Purpose: the to-do list, grouped per area. Planned work. Open questions live in `open-points.md`.

Everything for `0.1.0` that can be done without the game running is done. What is left needs
either a running client or a decision.

## In the game, before anything else

- [ ] Load the staging build (`build.ps1` prints the path) and confirm it loads at all
- [ ] Confirm a gearset actually changes from the bar, from the library and from the command
- [ ] Answer the four questions in `open-points.md` that need a running game
- [ ] Check the bar in a cutscene and in combat, with the hiding settings both ways
- [ ] Check the library against a character with many gearsets, including several sharing a job
      and a name, which is what the reconciler was built for
- [ ] Confirm the notes window appears once after an update and not on a first installation
- [ ] Check the layout in German, the longer of the two shipped languages

## Before the first release

- [ ] The icon, exactly 512 by 512 and square, judged at the size it is actually drawn at, in
      colour and in the greyed-out state
- [ ] Measure per-frame cost with Dalamud's own plugin statistics window rather than estimating
- [ ] The crash-safety audit over every unsafe block, pointer dereference, pinning block and
      game call reachable from an interface callback, as a checklist pass over grep hits, with
      the outcome recorded in `status.md`
- [ ] Decide on branch protection and record the answer either way (R-16)
- [ ] Push, and confirm both workflows go green on the server
- [ ] Follow `release.md` end to end

## Later, decided and deferred

- [ ] The Eorzea Arsenal best-in-slot integration. This side is built and inert; the other side
      does not offer the gate yet. The contract and the feedback on it are in `open-points.md`,
      marked as belonging to that repository.
- [ ] Globally bound keyboard shortcuts
- [ ] Drag and drop for the bar order, which is currently arranged from the library window
