# Demo Guide — running the M1 bike vertical slice in Unity

> **What exists right now:** the M1 bike vertical slice (`PROJECT_MOLEN_PLAN.md` §12) —
> a data-driven part/fastener system (`PART_SYSTEM_DESIGN.md`), two frame archetypes
> (city + race, ADR-0005), and two greybox test scenes. All geometry is placeholder
> primitives (cubes/cylinders) — no real art yet, per the PSX pipeline that comes later
> (`ART_DIRECTION_SPEC.md`).
>
> **Verification note:** this was built and validated headlessly (Unity batchmode +
> a scripted edit-mode check that installs every part and tightens every fastener,
> then asserts the bike reports complete). Nobody has clicked through the actual
> Play-mode controls in the Editor yet — do that first before trusting the feel of it.

## 1. Prerequisites
- **Unity Hub**, with **Unity 6000.0.41f1** (or 6000.0.36f1) installed via Hub.
- **Git** + **Git LFS** (`git lfs install`, once per machine).

## 2. Get the project
```
git clone https://github.com/s-kroonen/My-Dutch-Bike.git
cd My-Dutch-Bike
```
(If you already have the folder locally — e.g. this machine — nothing to do here.)

## 3. Open it in Unity
1. Open **Unity Hub** → **Add** → **Add project from disk** → select the
   `My-Dutch-Bike` (a.k.a. `My Dutch Bike`) folder.
2. Set its editor version to **6000.0.41f1** if Hub asks.
3. Open the project. **First open will take a minute or two** — Unity is
   importing all assets and compiling scripts for the first time. Subsequent
   opens are fast.

## 4. The three scenes
| Scene | What |
|---|---|
| `Assets/Scenes/Bootstrap.unity` | empty proof-of-life scene (ground/light/camera only) |
| `Assets/Scenes/City.unity` | city-bike BOM scattered around a stand, on a small street loop |
| `Assets/Scenes/RaceTrack.unity` | race-bike BOM scattered around a stand, on a bigger track loop |

Open `City.unity` or `RaceTrack.unity` and press **Play**.

## 5. Controls
| Input | Action |
|---|---|
| WASD | walk |
| Mouse | look |
| Aim + **E** | pick up a loose part / place a held part on a highlighted socket / mount the bike once it's fully built |
| **Q** | drop the currently held part |
| Aim at a bolt + **hold Left Mouse** | tighten it |
| Aim at a bolt + **hold Right Mouse** | loosen it |
| While riding: WASD | throttle + steer; **E** dismounts |

There's no on-screen prompt yet (diegetic-only per the art spec §7) — you're aiming with
the crosshair-less screen center. If nothing happens on **E**, you're not close enough or
not looking directly at the part/socket/bolt (interact range is 3 m, raycast from camera center).

## 6. What "done" looks like
Each scene starts with the frame already mounted on a stand and every other part
(fork, wheels, tires, crankset, chain, handlebar, seat, brakes, pedals) lying loose
nearby, bolts undone. Pick each one up, place it on its matching socket, then tighten
every bolt on it. Once **every** part is installed and every bolt is tight, the bike
becomes mountable — aim at it and press E to ride. City and race frames should feel
different (`Assets/Data/Mechanics/*.asset`): city is slower/more stable, race is
faster/twitchier.

If a part won't seat: it's either aimed at the wrong socket (categories must match,
e.g. a tire only fits a wheel's rim socket) or a prerequisite part isn't installed yet
(the Console logs the reason — e.g. "missing dependency: bike.wheel_rear" for the chain).

## 7. Confirm URP is actually active
`Edit → Project Settings → Graphics` → **Scriptable Render Pipeline Settings**
should show `MyDutchBike_URP` (not "None" / built-in). This asset lives at
`Assets/Settings/MyDutchBike_URP.asset`, with its renderer at
`Assets/Settings/MyDutchBike_Renderer.asset`.

## 8. Where things live
| Path | What |
|---|---|
| `Assets/Code/Parts/` | authored data: `PartDefinition`, `SocketDefinition`, `FastenerSlot`, `FrameMechanics`, and the plain-data `AssemblyState` (the eventual save payload) |
| `Assets/Code/Bike/` | `BikeAssembly` (install/tighten logic + spawning), `SocketMarker`, `FastenerPoint`, `BikeRideController` |
| `Assets/Code/Interaction/` | `PlayerInteractor` (raycast pickup/place/tighten/mount), `LoosePart` |
| `Assets/Code/Player/` | `FirstPersonController` |
| `Assets/Data/Parts/` | the 12 `PartDefinition` assets (2 frames + 10 shared parts) |
| `Assets/Data/Mechanics/` | per-archetype handling data (top speed, accel, turn rate, stability) |
| `Assets/Prefabs/Parts/` | placeholder primitive meshes for every part |
| `Assets/Editor/BikeBomAuthoring.cs` | generates the part defs + placeholder prefabs (menu: **Tools → Project Bootstrap → Author Bike BOM**) — idempotent |
| `Assets/Editor/GreyboxSceneAuthoring.cs` | generates the City/RaceTrack scenes (menu: **Tools → Project Bootstrap → Author Greybox Scenes**) — idempotent |
| `Assets/Editor/M1SmokeTest.cs` | scripted install-everything-and-check check (menu: **Tools → Project Bootstrap → Run M1 Smoke Test**) |
| `docs/` | the design docs — start with `PROJECT_MOLEN_PLAN.md` |

## 9. What's next
This is a first pass, not a polished slice — expect rough edges (raycast precision on
small bolts, no save/load yet, no wear/condition decay, no diegetic prompts, placeholder
geometry). Natural next steps: playtest in the Editor and tune interaction ranges/socket
sizes, add wear over ride distance, then move toward M2 (the windmill).
