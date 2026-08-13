# Release procedure: Gearbook

Purpose: the chain from the act that starts a release to the moment somebody can install it,
written to be followed rather than remembered (D-01). The steps that get forgotten are the ones at
the end, after the part that felt like the work.

Nothing here is done automatically or on a hunch. A tag is pushed only on the owner's explicit
instruction.

## Before the very first release

Three things must be true, and none of them needs an extra file in this repository:

1. The repository is public. It is.
2. It has at least one published release, neither draft nor pre-release.
3. The packaged archive is attached to that release under the asset name the aggregate index looks
   for.

Then two things are added **in the index repository** (`miralsoft/Dalamud-Plugins`), by whoever
maintains it, not from here (M-18): the entry in its list, **and** the descriptive section in its
README linking back to this repository. Both. A release only existing users can discover is an
update, not a release.

## Cutting a release

1. **Decide the version.** Semantic versioning. Work in progress collects under `Unreleased` in
   `CHANGELOG.md` and the number is set once, when the change is finished, and only after asking
   (C-13). A version is what other projects align to, so a bump per working step sends "review
   this" for a state nobody could have targeted.

2. **Review the declared foundation version** in `project.md` (M-17). Read the foundation changelog
   from the declared version onward, then either raise it and do the work, or leave it and record
   why in `decisions.md`. Raising it includes re-copying whatever was copied out of the foundation
   and updating its provenance line (M-19). Releasing is the moment somebody is already looking, so
   this check costs a minute and cannot be forgotten by drifting past it.

3. **Raise the version in one place**, `Directory.Build.props`. Every other occurrence is derived
   from it or checked against it. Several hand-kept copies is exactly how a manifest and an
   assembly come to disagree.

4. **Write the three documents.** They describe the same release for three different readers and
   none is generated from another (D-12):
   - `CHANGELOG.md` for whoever works on the plugin;
   - the embedded release notes in `Gearbook.Core/News/Resources/`, one file per language, for
     whoever plays with it;
   - the description in the plugin index, for somebody deciding whether to install at all.

   The tests fail the build if the notes and the changelog disagree about which versions exist, or
   if the newest entry does not name the version being built.

5. **Run the gates locally**: `./build.ps1`. It runs the same checks CI runs. If it passes here and
   fails there, that difference is a defect in the script.

6. **Merge to `main`.** From the first public release onward this happens through a pull request
   with the required check, and a release change is never opened as a draft: on GitHub a draft pull
   request does not fire the events required checks listen for, so the check never runs and the
   request stays blocked with nothing to fix (D-09).

7. **Push the tag, after the merge.** The release workflow builds from a clean checkout, where the
   developer-tools file does not exist, checks the tag against the version actually built, and
   refuses to publish if they disagree. The version is not derived from the tag: the tag is the one
   thing written by hand at the end, so a mismatch has to fail loudly rather than publish whatever
   the tag happened to say.

8. **Confirm by inspecting the result, not by a green workflow** (D-07). A green run is the
   automation's statement about itself, and the failures that matter here are the ones where every
   step succeeded and the outcome is still wrong. Check all four:
   - both release assets are present, the packaged archive and the single-entry index file;
   - the manifest **inside the archive** names the new version, because that is the copy the channel
     and the installer read, not the one in the repository;
   - the entry in the aggregate index has appeared;
   - the public address serves it.

## The two silent delays

Between a published release and a player seeing it, two waits sit in the chain. Both are normal.
Undocumented, a normal wait reads as a fault and sends somebody hunting one that does not exist.

- **The aggregate index rebuilds on a schedule.** Its interval is defined in
  `miralsoft/Dalamud-Plugins` and is not restated here, because a copy of somebody else's schedule
  goes stale silently. Look it up there the first time this procedure is run and record the figure
  in this section.
- **The public address is cached.** Same: the duration belongs to whoever serves it.

The index build can be set off by hand to skip the first wait. That is allowed even though the
index is another repository, because it reaches the same state on its own schedule either way and
the trigger only moves the moment (M-18). What is **not** allowed from here is supplying it with
anything: the entry, its description and the icon address stay with whoever owns that repository. A
dispatch that carries inputs is a change wearing a trigger's costume.

## If a release has to be withdrawn

It stays recorded as withdrawn rather than being deleted, in the changelog and everywhere else it
was announced. The same principle the rule numbering uses: retired, not removed. Removing an entry
from the aggregate index withdraws the plugin from every existing user, so it is a deliberate act
and never a reaction to a failure.
