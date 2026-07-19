# My Summer Bike — Part & Fastener System Design

> Companion to `PROJECT_MOLEN_PLAN.md` §4.2 / §6.3. This is the **core system** — MSC-deep assembly (parts, install order, bolts with tightness, per-part wear) that also drives the save format and generalizes to every buildable object (bike, mill, scooter, boat).
> **Status:** v0.2 — design. Prototype on the bike (M1), generalized across city/race frame archetypes per ADR-0005.
> Design goals: **data-driven** (no code per object), **fully serializable** (drives saves), **co-op-aware** (authoritative, clean state).

---

## 1. Principles
1. **Objects are data, not code.** A bike or mill = a set of part/socket/fastener definitions + meshes. New objects and DLC add data, not systems.
2. **State is plain and serializable.** Runtime state is POCO (ids, floats, bools) — no scene references — so it saves to JSON and networks later without rework.
3. **The world places, the definitions describe.** Definitions (immutable, authored) vs instances (mutable, per-save).

---

## 2. Authored data (ScriptableObjects — immutable)

**PartDefinition**
| Field | Type | Meaning |
|---|---|---|
| `id` | string | stable unique id (e.g. `bike.frame`) |
| `displayName` | localized | shown to player |
| `mesh` / `prefab` | ref | visual (Addressable) |
| `mass` | float | physics |
| `category` | enum | frame / wheel / drivetrain / … |
| `sockets` | Socket[] | attach points this part *provides* |
| `installsOn` | socketType | where this part *goes* |
| `dependencies` | partId[] | must be installed first |
| `fasteners` | FastenerSlot[] | bolts/nuts securing it |
| `wearProfile` | ref | how it degrades |

**SocketDefinition** — `id`, `localTransform`, `acceptedCategory`.
**FastenerSlot** — `id`, `type` (bolt/nut/screw), `localTransform`, `torqueTarget`, `toolRequired`.

## 3. Runtime state (serializable — per save)

**AssemblyState** (one per object)
```
objectId
parts:      [ { partDefId, installed, onSocketId, condition } ]   // condition 0..1
fasteners:  [ { fastenerSlotId, present, tightness } ]            // tightness 0..1
```
That's the entire save payload for an object — ids + floats + bools. The world save is a list of AssemblyStates + player/world/time state.

## 4. Rules
- A part installs on a socket only if its `dependencies` are installed and the socket is free & category-matched.
- A part is **secured** only when its fasteners are `present` and `tightness ≥ threshold`.
- **Loose/missing fasteners** → part rattles, loses function, or detaches under stress (MSC-style consequence).
- **Install order** is enforced by dependencies (can't mount the chain before the crank).
- **Wear** reduces `condition` over use/time (plan §4.7); low condition degrades function; repair = service or replace part.

## 5. Interaction flow (first-person)
```
raycast → hover part/bolt/tool
  pick up part      → highlights valid sockets → place → snaps (uninstalled, unsecured)
  grab tool (wrench)→ target bolt → tighten/loosen (ratchet action) → tightness rises/falls
  torque feedback   → sound + wobble stops when secured
  wrong order / no tool → blocked with a diegetic hint
```
Mirror MSC feel: physical, slightly fiddly, satisfying click when a bolt seats.

## 6. Serialization → the save system
- `AssemblyState` serializes to JSON directly (no Unity refs). Whole game save = `{ world, time, player, needs, assemblies[] }`.
- **Versioned schema** (`saveVersion`) — never silently break saves (plan §10.2). Migrate on load.
- Because state is decoupled from scene objects, load = spawn prefabs from definitions, then apply state.

## 7. Generalization & DLC
- Bike, mill, scooter, boat = just new **PartDefinition/Socket/Fastener** sets + meshes.
- DLC ships these as **Addressable content packs** (plan §6.6) — zero core-code changes.
- **Co-op-aware:** authoritative, serializable state (plan §6.7) means networking layers on later cleanly.

## 8. Example — bike bill of materials (M1 target) `[START]`

**Frame archetypes drive mechanics, not just looks (ADR-0005, plan §3.1).** Each archetype is a `bike.frame.<archetype>` PartDefinition with its own sockets (head-tube angle, chainstay length, mount points) and its own tuning data (weight, stiffness, gearing range, top-speed/handling params). Every other part below **installs identically on any frame archetype** via matching socket types — a `bike.tire.*` or `bike.brake.*` part doesn't care whether it's socketed to `bike.frame.city` or `bike.frame.race`. This is what makes "swap tires/brakes/pedals on any bike" and "add a new frame archetype later" both just data changes.

M1 ships two archetypes: `bike.frame.city` (upright geometry, fender/rack mounts, relaxed handling) and `bike.frame.race` (drop-bar geometry, no fender mounts, light/stiff/fast).

| Part (`id`) | Installs on | Depends on | Fasteners | Notes |
|---|---|---|---|---|
| `bike.frame.city` / `bike.frame.race` | world/stand | — | — | archetype root; only one frame installed per bike instance |
| `bike.fork` | frame.head | frame | 2× headset nut | |
| `bike.wheel_front` | fork.dropout | fork | 1× axle nut ×2 | |
| `bike.wheel_rear` | frame.dropout | frame | 1× axle nut ×2 | |
| `bike.tire_front` / `bike.tire_rear` | wheel.rim | wheel_front / wheel_rear | — (bead seat) | swappable per wheel — width/tread/compound vary by category (e.g. `tire.city`, `tire.race_slick`) |
| `bike.crankset` | frame.bb | frame | 2× crank bolt | |
| `bike.chain` | crankset+rear | crankset, wheel_rear | — (tension) | |
| `bike.handlebar` | fork.stem | fork | 1× stem bolt | shape (upright vs drop) matches frame archetype convention, not hard-locked to it |
| `bike.seat` | frame.post | frame | 1× seat clamp | |
| `bike.brakes` | fork + frame | fork, frame | 2× caliper bolt | swappable category (`brake.rim`, `brake.disc`) independent of frame archetype |
| `bike.pedals` | crankset | crankset | 2× (threaded) | swappable category (`pedal.platform`, `pedal.clipless`) |

Enough parts to prove order, dependencies, bolts, tightness, and wear — without the mill's complexity — while proving the **frame-archetype generalization** end to end. This BOM *is* the M1 vertical slice.

## 9. Later tooling `[TBD]`
- A Unity **editor tool** to author sockets/fasteners visually on a mesh.
- Debug overlay showing each part's condition + tightness.

## 10. Open items `[TBD]`
- Tightness threshold + torque model (simple vs realistic)
- Wear rates + failure behavior per category
- How tools are acquired/upgraded (ties to economy, plan §4.3)
