# CLAUDE.md — instructions for any Claude Code instance working on this repo

> Project: **My Dutch Bike** (working title in the docs: *My Summer Bike*) — a first-person,
> PSX-styled build-and-maintain sim. Full plan: [`docs/PROJECT_MOLEN_PLAN.md`](docs/PROJECT_MOLEN_PLAN.md).
> Current state snapshot: [`HANDOVER.md`](HANDOVER.md) — **read that first** if you're picking
> this up mid-stream; it has the "what's done / what's next" that this file doesn't.

## Engine & tooling

- **Unity 6000.5.4f1** (see `ProjectSettings/ProjectVersion.txt` for the exact version this
  project currently targets — always trust that file over this doc if they disagree).
  Install via Unity Hub if missing.
- **Git + Git LFS.** Run `git lfs install` once per machine before cloning/checking out.
- No Node/npm/etc. — this is a pure Unity project, nothing to `npm install`.

## Critical rule: never run Unity batchmode while the Editor is open

Unity refuses a second instance on the same project. If a batchmode command runs while
the Editor GUI has this project open, it hangs and then fails with:
```
Aborting batchmode due to fatal error: another Unity instance is running with this project open.
```
This has happened twice already (see `HANDOVER.md` history). **A hook in
`.claude/settings.json` (`.claude/hooks/check-unity-not-running.sh`) already blocks this
automatically** — if a Bash command containing `Unity.exe` runs while `Get-Process Unity`
finds a running instance, the hook denies the tool call before it runs. If you ever need to
bypass it deliberately, close the Editor window instead of fighting the hook.

## Verifying changes headlessly

There is no GUI automation available to Claude Code in this environment — you cannot click
through the Unity Editor. Verification instead happens via Unity **batchmode** + a scripted
edit-mode check (`Assets/Editor/M1SmokeTest.cs`) that programmatically installs every part
and tightens every fastener on both greybox scenes, then asserts the bike reports complete.
Run it after any change to `Assets/Code/**` or the authoring scripts:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.4f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "<repo root>" -executeMethod M1SmokeTest.Run \
  -logFile "<some log path>"
echo "EXIT CODE: $?"
```

Notes:
- **Always check `Get-Process Unity` first** (the hook does this for you now, but know why).
- The apparent exit code of a backgrounded shell wrapper is **not** Unity's real exit code —
  capture it explicitly (`echo "EXIT CODE: $?"` after the Unity invocation) and read that,
  don't trust the wrapper's own exit status.
- Grep the log for `M1SmokeTest: ALL PASS` / `error CS` / `Exception`, don't just check exit 0.
- `M1SmokeTest.Run` calls `BikeBomAuthoring.Run()` and `GreyboxSceneAuthoring.Run()` first, so
  one invocation regenerates data + scenes + validates, all idempotently.
- The authoring scripts (`Assets/Editor/BikeBomAuthoring.cs`, `GreyboxSceneAuthoring.cs`,
  `ProjectBootstrap.cs`) **skip anything that already exists on disk**. If you change part
  definitions, prefab geometry, or scene layout logic, **delete the affected `.unity` file(s)
  (keep the `.meta`)** before rerunning, or your change won't take effect.
- After any Unity/package upgrade, rerun the smoke test before touching anything else —
  that's the fastest way to confirm nothing broke (see `HANDOVER.md` for the last one).

## Git conventions

- **Never add a `Co-Authored-By: Claude` trailer to commits in this repo** — explicit user
  preference, overriding Claude Code's default commit workflow. A hook
  (`.claude/hooks/check-no-claude-coauthor.sh`) blocks any Bash command containing that
  string as a defense-in-depth backstop, but don't rely on the hook — just don't write it.
- Prefer small, descriptive commits. Look at `git log` for the tone/format already in use.
- If `git commit` hangs with no error: this environment has been seen with
  `commit.gpgsign=true` + SSH-format signing (`gpg.format=ssh`) and no reachable ssh-agent —
  the signing step blocks waiting for a passphrase prompt that never appears in a
  non-interactive shell. Check `Get-Service ssh-agent` (Windows) / `ssh-add -l` before
  assuming something else is wrong. Don't bypass signing (`--no-gpg-sign`) to work around
  it — get the agent running instead, or ask the user to.
- Don't force-push, don't `--no-verify`, don't rewrite history on `main` — standard rules,
  restated because this is a solo hobby repo where it'd be easy to assume otherwise.

## Repo structure quick-reference

Full layout is in [`docs/REPO_SETUP_GUIDE.md`](docs/REPO_SETUP_GUIDE.md). The parts that
matter day to day:

| Path | What |
|---|---|
| `docs/PROJECT_MOLEN_PLAN.md` | the master plan + ADR log — **the source of truth for scope decisions** |
| `docs/PART_SYSTEM_DESIGN.md` | the part/fastener system design |
| `docs/DEMO_GUIDE.md` | how to open the project and play the current build, plus controls |
| `Assets/Code/Parts/` | authored data model: `PartDefinition`, `SocketDefinition`, `FastenerSlot`, `FrameMechanics`, plain-data `AssemblyState` |
| `Assets/Code/Bike/` | `BikeAssembly` (install/tighten logic), `SocketMarker`, `FastenerPoint`, `ChainRoute` (two-stage chain routing + rear cog stack), `BikeRideController`, `BikeDebugOverlay`, `DebugVisualUtility` |
| `Assets/Code/Interaction/` | `PlayerInteractor` (raycast pickup/place/tighten/mount + crosshair/cursor lock), `LoosePart` |
| `Assets/Code/Player/` | `FirstPersonController` |
| `Assets/Editor/` | idempotent authoring scripts + `M1SmokeTest.cs` — see above |
| `Assets/Data/Parts/`, `Assets/Data/Mechanics/` | generated `PartDefinition`/`FrameMechanics` assets (don't hand-edit; edit the authoring script and regenerate) |
| `Assets/Scenes/` | `Bootstrap.unity` (empty proof-of-life), `City.unity`, `RaceTrack.unity` — all generated, don't hand-edit without regenerating from the authoring scripts |

## Decision process

Non-trivial scope or architecture decisions get an **ADR** in `docs/PROJECT_MOLEN_PLAN.md`
§13 (template there). Don't quietly expand scope (e.g. new bike archetypes, new systems)
without adding one — see ADR-0005 for the pattern (multi-bike-type + city/race environments).

## What NOT to assume

- Don't assume any particular machine has `jq`, `gh` (GitHub CLI), or Node globally — none
  were guaranteed present when this was built. The hook scripts deliberately avoid `jq`.
- Don't assume the Unity Editor is closed — always check before batchmode (see above).
- Don't assume `.claude/` memory files from a previous machine exist here — that's exactly
  why this file and `HANDOVER.md` exist. Keep them updated as the source of truth in-repo.
