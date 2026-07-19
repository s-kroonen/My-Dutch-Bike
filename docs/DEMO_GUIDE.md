# Demo Guide — running the current skeleton in Unity

> What exists right now: an empty Unity 6 / URP project with merge-friendly
> settings and one bootstrap scene (ground + light + camera). No bike, no
> gameplay yet — this just proves the engine/repo pipeline works end to end.
> The real M1 target (bike vertical slice) is tracked in `PROJECT_MOLEN_PLAN.md` §12.

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

## 4. See it run
1. In the **Project** window, open `Assets/Scenes/Bootstrap.unity` (double-click).
   It should already be the active scene, and it's the only scene registered
   in Build Settings.
2. Press **Play** (▶ at the top).
3. Expected: a flat grey ground plane, seen from a slightly elevated angle,
   under a plain directional light. Nothing moves — there's no gameplay yet.
   This confirms the project opens, compiles, and renders correctly.
4. Press Play again to stop.

## 5. Confirm URP is actually active
`Edit → Project Settings → Graphics` → **Scriptable Render Pipeline Settings**
should show `MyDutchBike_URP` (not "None" / built-in). This asset lives at
`Assets/Settings/MyDutchBike_URP.asset`, with its renderer at
`Assets/Settings/MyDutchBike_Renderer.asset`.

## 6. Where things live
| Path | What |
|---|---|
| `Assets/Scenes/Bootstrap.unity` | the one demo scene |
| `Assets/Settings/` | URP pipeline + renderer assets |
| `Assets/Editor/ProjectBootstrap.cs` | one-time setup script (menu: **Tools → Project Bootstrap → Run Once**) — safe to re-run, it skips anything that already exists |
| `Assets/Art`, `Audio`, `Code`, `Prefabs`, `Data` | empty for now, per `docs/REPO_SETUP_GUIDE.md` §8.3 layout |
| `docs/` | the design docs — start with `PROJECT_MOLEN_PLAN.md` |

## 7. What's next
This skeleton is the starting point for **M1: the bike vertical slice**
(`PROJECT_MOLEN_PLAN.md` §12) — implementing the part/fastener system
(`PART_SYSTEM_DESIGN.md`) on a city-bike and race-bike frame, in a greybox
city-street loop and a greybox race-track loop.
