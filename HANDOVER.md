# Handover — where this session left off

> Read [`CLAUDE.md`](CLAUDE.md) first for the rules; this doc is the **state snapshot**.
> Update this file whenever you hand off again — it's meant to stay current, not be a diary.

**As of:** the "two-stage chain routing" commit on `main`,
repo https://github.com/s-kroonen/My-Dutch-Bike
**Milestone:** M1 — the bike vertical slice (`docs/PROJECT_MOLEN_PLAN.md` §12). The bike is now
**fully hand-buildable and fully removable**, and the **chain is a real routed drivetrain part** —
the core M1 loop works end to end.

## Tooling change this session: MCP for Unity is live

- The `UnityMCP` server (CoplayDev/unity-mcp, launched via `uvx` from `~/.claude.json`) was failing
  to start: its config passed `uvx --offline`, and the dependency tree (fastmcp→mcp→pydantic) wasn't
  fully in the uv cache, so it crashed on every launch. **Fix: removed `--offline`** from the server
  args in `~/.claude.json`. It now starts and drives the open Editor.
- With the Editor open + bridge running, you can read scene/console state and run edit- or play-mode
  C# via MCP tools (`execute_code`, `manage_editor`, `manage_scene`, `manage_camera` screenshots, etc).
  This is how everything below was verified without a human clicking — a big upgrade over batchmode.
- **Do NOT run `M1SmokeTest.Run` via MCP `execute_code`** — it calls `EditorApplication.Exit`, which
  would kill the user's open Editor. Verify by driving `BikeAssembly` in play mode instead (build via
  `TryInstallPart` + `SetFastenerTightness`, assert `IsFullyAssembledAndSecured()`), then stop play.

## What's done (M1)

1. **Full interaction loop works** (`PlayerInteractor`): pick up loose parts (E), **place on sockets**,
   **tighten/loosen** fasteners (LMB/RMB), **remove installed parts** (E), mount/dismount.
   - Placement and tightening use a forgiving **aim cone** (not a precise ray hit), because the socket
     (8cm) and fastener (3cm) trigger colliders are small and often embedded in/occluded by part meshes.
     `BikeAssembly.FindAimedSocket` / `FindAimedFastener`. Cones: place 25°, fastener 8°.
   - **Blink/highlight feedback:** open sockets that accept a held part blink (targeted one brightest);
     the fastener under the crosshair pulses white over its red→green tightness colour.
   - **The original "can't place" bug** was `PickUp` disabling only `GetComponent<Collider>()` — but
     `LoosePart` colliders live on a child "Visual", so the held part's collider stayed on and the ray
     kept hitting what you held. Now disables all child colliders.
2. **Part removal** (`BikeAssembly.CanRemove`/`TryRemovePart` + `InstalledPart` marker): a part comes
   off once **all its fasteners are loose** and **nothing is installed on it** (strip in reverse). The
   removed part drops straight into your hands. Frame never removes. Verified: all 11 parts strip off.
3. **Bike is fully buildable → `complete=True`.** The blocker was the chain depending on the rear wheel
   being *secured* (tightened), so "attach all, then tighten" left the chain uninstallable. The
   dependency check in `CanInstall` was relaxed to **"installed", not "secured"** (matches the authoring
   comment). Completeness still requires everything tightened to ≥90% (`PartState.IsSecured`).
   - The **"Incomplete" prompt now names what's left** (`FirstIncompleteRequirement`), e.g.
     `Incomplete — next: Seat (tighten)`.
4. **Bike geometry reworked** (`BikeBomAuthoring`), regenerated fresh:
   - **Frame** is now a see-through **diamond of thin tubes** (head/top/down/seat tubes + chain/seat
     stays), not a slab — `CreateBikeFramePrefab` + `AddTube`. Bike is laid out along **+Z** (fork/front
     wheel +Z, rear wheel −Z) so it faces forward instead of sideways (the old "everything's 90° off").
   - **Wheels** = silver rigid disc (rim); **tires** = black **hollow torus rings** (procedural mesh via
     `CreateTorusMesh`/`CreateTorusPrefab`, double-sided material) so you see the wheel through them.
   - **Handlebar** = a raised **T** (`CreateHandlebarPrefab`): tall stem + wide bar, sitting **above the
     seat** (bar Y≈1.40 vs seat Y≈1.22).
   - **Fasteners repositioned** proud of their parts and spread apart (axle nuts on hub sides, headset
     nuts front/back of head tube, seat clamp under the seat, brake bolts on the face) so they're
     visible and individually aim-able. Bike raised on a **taller repair stand** so wheels clear ground.
5. **Player movement** (`FirstPersonController`): crouch (hold **LeftCtrl**, with headroom check),
   **sprint** (hold **LeftShift**, not while crouched), **jump** (**Space** when grounded).
6. **Player no longer collides with parts** — `Player` (layer 8) and `Part` (layer 9) layers created,
   that pair disabled in the physics collision matrix (`ProjectSettings/DynamicsManager.asset`). Loose +
   installed parts go on `Part`, the player rig on `Player`. Raycasts ignore the matrix, so
   pickup/place/tighten still work; you just stop climbing dropped parts.
7. **Loose-part physics** — cylinder parts (wheel/tire/crankset) get a **convex mesh collider** instead
   of the default rolling capsule, plus `angularDamping = 4` on loose/removed rigidbodies, so a dropped
   wheel/tire falls flat and settles instead of rolling forever.
8. **Chain routing** (`ChainRoute` + `BikeBomAuthoring` sprockets) — the chain is no longer a placeholder.
   Install it on the `crankset.chain` socket, then **route it in two ordered stages** by holding LMB:
   front chainring first, then rear cog (gated behind front). It reuses the fastener hold-to-progress
   mechanic (two `isChainRoute` fasteners `chain_front`/`chain_rear` with a `prerequisiteFastenerId`), so
   the chain counts as secured only when both stages are 100% — which feeds completeness/removal for free.
   `ChainRoute` draws a `LineRenderer` loop wrapping both sprockets (front wrap = stage 1, spans + rear
   wrap = stage 2). The crankset has a front chainring; the **rear cog stack is built from the archetype's
   `FrameMechanics.rearGearCount`** — city = internal hub (**1 cog**), race = derailleur cassette (**7 cogs**).
   Ordering/label live on `FastenerSlot` (generic), so prompts read "route/unroute chain (front/rear sprocket)".

## Verified (via MCP, in play mode)

- All 11 parts install + tighten → `complete=True`, zero console errors.
- All 11 parts strip off in reverse (fasteners reachable) → only the frame remains.
- Colliders: Wheel/Tire/Crankset are convex MeshColliders; Frame is 6 box-collider tubes.
- Handlebar bar sits above the seat. Screenshots confirmed the frame/wheel/tire/handlebar look.
- **Chain (city bike, play mode):** all 11 parts install; routing the rear before the front is blocked
  (stays 0); front-then-rear reaches 100%; rear cog count = 1 (city hub); `complete=True`; a side-on
  screenshot shows the chain wrapping both sprockets. **Race not driven in play** (7-cog cassette shares
  the exact same `ChainRoute`/gear-count path; `rearGearCount=7` confirmed in the mechanics asset).
- **Not yet human-playtested for *feel*** — cone sizes (25°/8°), blink readability, crouch/sprint/jump
  feel, frame proportions, and now the **chain routing feel** (aiming at the front/rear sprocket points,
  how long the two holds take). Logic + visual are proven; hands-on tuning is the open question.

## Known rough edges / not yet done

- **Part-failure mechanics not started** — the user explicitly wants these *next* after the chain:
  a **chain-link failure / chain falling off**, a **flat tire**, a **broken spoke**. The chain is now
  structured to support the first of these (routing state + visual are in `ChainRoute`). Nothing decays
  or fails yet.
- **Derailleur shifting not built** — race bikes render a 7-cog cassette, but you can't shift across it;
  the chain routes onto the stack as one unit. Shifting is a later mechanic (gear count is data-driven now).
- **No save/load** (plan §6.3, highest-risk system) — `AssemblyState` is designed to serialize to JSON;
  not started. Note the chain's routing state is just its two fastener tightness values, so it serialises
  the same as any bolt.
- **No wear/condition decay** — `PartState.condition` exists, nothing decrements it.
- **No PSX render pipeline** — plain URP defaults; intentional for M1.
- **Legacy Input Manager**, not the new Input System — deliberate (WASD/mouse/E/Q/LMB/RMB/Ctrl/Shift/Space).
- **Cosmetic:** City/RaceTrack still share ground/road material names (`GroundMat`/`RoadMat`), so both
  scenes get whichever tint was authored last. Harmless, low priority.
- RaceTrack was regenerated with the same authoring path but only City was play-tested this session;
  it should be identical (shared frame sockets/parts).

## Suggested next steps

1. **Part-failure mechanics** (user's stated next focus) — chain-link failure / chain off, flat tire,
   broken spoke. Build on `PartState.condition` (exists, unused) + the chain routing state.
2. **Human playtest + tune** the interaction feel (cone sizes, blink, crouch/sprint/jump, and the new
   chain-routing holds — how it feels to aim at the front then rear sprocket).
3. Then pick the next system: **save/load** (de-risks the hardest piece early) vs. more M1 polish vs.
   starting the PSX art pass. Ask the user.
4. Eventually M2 (the windmill) once M1 feels solid.

## Machine-specific things NOT to assume carry over

- `~/.claude.json`'s `UnityMCP` server args were edited (dropped `--offline`) on **this** machine —
  another machine's MCP config may differ.
- Layers `Player`/`Part` and the collision-matrix change live in committed `ProjectSettings`
  (`TagManager.asset`, `DynamicsManager.asset`) — those DO travel with the repo.
- Same commit-signing / `gh`-availability caveats as before still apply (see `CLAUDE.md`).
