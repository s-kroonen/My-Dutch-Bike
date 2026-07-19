# My Summer Bike — Master Plan

> **Working title:** *My Summer Bike* — homage to *My Summer Car*.
> **Title note `[TBD: final title]`:** Fine as a working/hobby title; if you go commercial, consider a more distinct name — it closely echoes a trademarked title and could invite friction.
> **Companion docs:** `ART_DIRECTION_SPEC.md` · `PART_SYSTEM_DESIGN.md` · `REPO_SETUP_GUIDE.md`.
> **Document type:** Living design + decision record. Single source of truth.
> **Status:** v1.0 — Pre-production. Nothing here is final until tagged `[DECIDED]`.
> **Last updated:** 2026-07-19

---

## 0. How to use this document

This file is written for **both humans and AI agents** to read and extend.

**Status tags** — every decision carries one:

| Tag | Meaning |
|---|---|
| `[DECIDED]` | Locked. Change only via a new ADR (§13). |
| `[PROPOSED]` | Recommended default, awaiting confirmation. |
| `[OPEN]` | Needs a decision. Listed in §14. |
| `[TBD]` | Will be filled in later; not blocking. |

**Conventions**
- Keep prose short. Prefer bullet points and tables.
- Any non-trivial technical decision gets an **ADR** in §13 (see template).
- When you change a decision, update the section *and* add/update its ADR *and* bump the changelog (§15).
- Agents: do not delete `[DECIDED]` content; supersede it with a new ADR and mark the old one `Superseded`.

---

## 1. Vision & Pillars

**One-liner:** A first-person build-and-maintain simulator set in the rural Netherlands around the year 2000, where you assemble and keep alive classic Dutch machines — starting with a wind-powered bread mill and a bike — steeped in old-style Dutch farming and culture.

**Premise `[DECIDED]`:** You inherit your grandparents' farm. They started two projects and never finished them — a wind-powered bread mill and a bike. Your job is to finish and run them, make the farm pay, and expand. The half-built inheritance is the in-world reason objects arrive as parts to assemble.

**Setting `[DECIDED]`:** Rural **West-Brabant**, ~2000 — the polder-edge farmland and small villages around Bergen op Zoom / Steenbergen / Dinteloord (evoking small places like De Heen and Stampersgat). Flat-ish farmland, small-village Brabant character, Dutch signage / radio / food of the era. Real names are inspiration, not a 1:1 map.

**Design pillars** (in priority order):
1. **Style above all.** Nailing the *My Summer Car* aesthetic and feel is the primary success criterion. Everything serves this. `[DECIDED]`
2. **Tangible tinkering.** The core joy is assembling, maintaining, and repairing physical objects part by part. `[DECIDED]`
3. **Authentic Dutch, circa 2000.** Setting, objects, culture, and mood are specifically Dutch and specifically that era. `[DECIDED]`
4. **A base that expands.** Ship a small, excellent core; grow it via clean DLC/updates. `[DECIDED]`

**Tone & audience:** Grungy, adult, MSC-style — drinking, crude humour, real danger and consequences. Aimed at a mature audience (expect an 18+ / PEGI 18 / Steam Mature rating). `[DECIDED]`

**Explicitly not goals:** shipping fast, minimizing difficulty of development, or matching any deadline. Quality and style win over speed. `[DECIDED]`

---

## 2. Reference & Inspiration

**Primary reference:** *My Summer Car* (Amistech Games, Unity).

**What we take (the feel):**
- Low-poly geometry + hand-painted / retro textures; grungy, lived-in rural world.
- Deep, physical part-by-part assembly and maintenance; things wear, break, and must be fixed.
- First-person, immersive, slightly janky-charming physics. `[DECIDED]`
- Diegetic, low-UI presentation.

**What we change:**
- Setting: rural Netherlands ~2000, not 1990s Finland.
- Objects: a windmill (bread mill) and a bike instead of a rally car.
- Culture: Dutch food, radio, signage, landscape, humour.

**⚠ Legal note `[DECIDED]`:** *My Summer Car* assets (models, textures, sounds, code) are **proprietary IP** and **must not be extracted or reused**. We replicate the *style* only, with our own or properly-licensed assets. See §5.4.

---

## 3. Scope

### 3.1 Base game (v1.0) — the foundation
The minimum that proves the concept and feels complete:
- **Two buildable/maintainable objects:** the **bike** (first vertical slice — simpler) and the **wind-powered bread mill / windmill** (flagship). `[DECIDED]`
- **Multiple bike archetypes, each a distinct frame + mechanics** — not just one bike. v1.0 ships at least a **city bike** (upright frame, fenders, rack, comfort geometry) and a **race bike** (drop-bar frame, lightweight, no fenders, stiffer/faster handling). Both run on the same Part & Fastener system (`PART_SYSTEM_DESIGN.md`) as different `PartDefinition` sets — new frame = new data, not new code. Every bike, regardless of archetype, is fully customizable part-by-part (frame, tires, brakes, pedals, wheels, drivetrain, handlebar, seat …). `[DECIDED]` — see ADR-0005.
- **Two riding environments to exercise the bikes:** the **city** (village streets/roads, already part of the farm region) and a **race track** (a dedicated loop for testing race-bike handling/performance). Both are v1.0 scope, not DLC. `[DECIDED]` — see ADR-0005.
- A **small region**: the inherited family farm + a nearby village + connecting roads (doubles as the "city" riding environment above). `[DECIDED]`
- Dual core loop: produce/sell for money + acquire → assemble → operate → wear/repair → maintain. `[DECIDED]`
- Full survival/immersion layer (§4.4). `[DECIDED]`
- Save/load. `[DECIDED]`

### 3.2 Planned DLC / expansions (post-1.0)
Ordered rough priority — all `[DECIDED]` as *intended*, sequencing `[TBD]`:
- **Further bike archetypes** (e.g. cargo bike, BMX, kids' bike) — cheapest DLC of all, since v1.0 already generalizes the bike to multiple frame archetypes on the shared part system.
- **Classic scooter** (e.g. brommer / Puch-style) — closest to the bike system, good first non-bike DLC.
- **Boat** — adds water physics.
- **Other cities/regions** — adds world content + travel.
- **Co-op multiplayer** — a major post-1.0 feature (not content DLC). Not built for v1.0, but core systems are designed *co-op-aware* now (see §6.7) so it's feasible later.
- Future/unknown expansions: reserve architecture for them (§6.6).

### 3.3 Out of scope for v1.0 `[DECIDED]`
- Networked co-op — *deferred, not abandoned*: excluded from v1.0 to protect scope, but architected for (§6.7).
- Console/mobile ports.
- Procedural world generation.

---

## 4. Core Gameplay

### 4.1 Core loop `[DECIDED]`
Two interlocking loops:
- **Production / economy loop:** grow/harvest grain → mill into flour → bake/sell bread → earn money → buy parts, tools, and expansions.
- **Tinkering loop:** buy/find parts → transport → assemble (part by part) → operate → parts wear & fail → diagnose → repair/maintain → upgrade.
The mill is the economic engine; money from it funds everything else (including bike parts and expansion).

### 4.2 Assembly & maintenance model `[DECIDED]` — MSC-deep
Full granular assembly: individual parts with correct **install order/dependencies**, **fasteners** (bolts/nuts) with tightness/torque state, and **per-part wear**. This is the hardest, most central system — it drives the interaction system, the save format, and the whole "tinkering" feel.
The part/fastener system must model: part identity + attach sockets; fastener state (present, tightness); install-order dependencies; per-part condition/wear; and full serialization for saves. Prototype it on the **bike** first (fewest parts) before the windmill. Exactly which parts/bolts exist per object: `[TBD]`.

### 4.3 Progression & economy `[DECIDED]` (details `[TBD]`)
The **windmill's production loop is the main money-maker**: grain → flour → bread, sold for money. Money buys bike/mill parts, tools, more equipment, and expansions. Likely role for the **bike**: transport/deliveries within the world (to confirm). Exact prices, sale points, and grain-sourcing (grow your own vs buy grain): `[TBD]`. Dutch-flavoured economy.

### 4.4 Survival / immersion layer `[DECIDED]`
Full MSC-style life-sim: needs (hunger, thirst, fatigue), consequences, and permadeath — adapted to Dutch culture (food, drink, daily habits). This is a core part of the feel, not an optional layer. It raises the importance of the save system (§4.5) and world simulation (§6.3). Detailed needs/consequence tuning: `[TBD]`.

### 4.5 Save system `[DECIDED]`
**Diegetic in-world save points** (like MSC — e.g. a specific spot on the farm), not save-anywhere. Must serialize the full world + every object's part/fastener state + survival + time/season state. Design early — it constrains the part system (§6.3). Exact save-point location(s) and count: `[TBD]`.

### 4.6 Goal structure `[DECIDED]`
**Open sandbox with optional objectives.** The spine goal, from the premise, is to **finish and run the grandparents' half-built mill and bike** and make the farm pay. Around that, the core is open-ended build/maintain/produce, with optional objectives/tasks (and a loose narrative) guiding players who want direction without forcing it. Objective content is data-driven (fits §6.6 so it can extend via updates/DLC).

### 4.7 Time & seasons `[DECIDED]`
Day/night cycle **and** seasons. Seasons drive the farming cycle (sow → grow → harvest grain) and visual variation; day/night drives survival rhythm (sleep, fatigue), shop hours, and activity. Time also advances wear and needs. Exact time scale (how long a day/season lasts) and whether time can be skipped by sleeping: `[TBD]`.

---

## 5. Art Direction — **THE priority** (see Pillar 1)

> This is the section that decides whether the game reads as "MSC-like." Style consistency comes from a **locked spec that every asset obeys**, not from which packs we download.

### 5.1 Style spec `[DECIDED: direction]` / exact numbers `[TBD]`
**Overall look: a full PSX / PS1 retro aesthetic applied to MSC-style grimy subject matter** — i.e. *more* aggressively retro than MSC itself (MSC keeps stable geometry; we go further). Rendering rules:
- **Vertex snapping / jitter** (the PS1 "wobble") and **affine, non-perspective-correct texture mapping** (the warp). `[DECIDED]`
- **Low-res, point-filtered textures**, limited palette, visible **dithering**, low internal render resolution upscaled. `[DECIDED]`
- **Vertex lighting**, no/minimal normal maps, baked where possible, **fog for draw distance**. `[DECIDED]`
- **Palette / mood: muted, overcast Dutch realism** — greys, greens, browns, flat overcast light. `[DECIDED]`
- Low-poly geometry; dirt/wear expressed through textures.

Still to nail down (`[TBD]`): exact poly budgets per class (prop / part / building / character), texture resolutions + texel density, vertex-snap strength, fog distances, camera FOV, HUD/font.

### 5.2 Audio direction `[TBD]`
Diegetic sound, period Dutch radio vibe, mechanical foley, ambient rural NL.

### 5.3 Asset pipeline `[DECIDED: hybrid]`
- **Model: hybrid** — CC0 assets as the base/background layer; **custom "hero" assets** (the mill, the bike, key props) built in-house. `[DECIDED]`
- **DCC tool:** Blender (free, CC0-friendly, strong Unity export). `[PROPOSED]`
- **PSX-conform step (mandatory):** every asset — CC0 *or* custom — is normalized to the §5.1 spec (low-res textures, point filtering, PSX shader, poly budget) before it enters the game. **No raw CC0 drop-ins.** This is what keeps a hybrid library visually consistent. `[DECIDED]`
- Naming conventions, import settings, folder structure: `[TBD]`.

### 5.4 Asset sources (all commercial-safe if license respected)
| Source | License | Best for |
|---|---|---|
| Kenney (kenney.nl) | CC0 | Broad low-poly props, kits |
| Quaternius | CC0 | Stylistically consistent low-poly packs |
| Poly Haven | CC0 | Textures, HDRIs, some models |
| Poly Pizza | CC0 / CC-BY | Huge low-poly model search |
| ambientCG | CC0 | PBR/retro textures |
| Sketchfab (CC0 filter) | CC0 | Variety (check each model) |
| itch.io (CC0 tag) | mixed | Style-specific packs |

**License hygiene `[DECIDED]`:** prefer **CC0**. If CC-BY is used, log it in `CREDITS.md`. Never ship CC-BY-NC (non-commercial) or GPL art in a commercial build. Verify license per-asset before shipping.

---

## 6. Technical Architecture

### 6.1 Engine — **ADR-0001** `[DECIDED: Unity 6]`
See §13 ADR-0001 for full rationale. Unity chosen: MSC lineage, C# fit, largest low-poly ecosystem, free Personal tier.

### 6.2 Language / scripting
C# (Unity). `[DECIDED]`

### 6.3 Key systems to design early (priority order)
1. **Part/fastener system** — data-driven; each part = data + mesh + state (sockets, fasteners, tightness, install-order, wear). The foundation everything else depends on (§4.2).
2. **Save/serialization** — must capture full world + every object's part-state + survival state. Highest-risk piece.
3. **Interaction system** (pick up / place / bolt / tighten / hold).
4. **Wear & repair model.**
5. **Survival/needs simulation** (§4.4).
6. **Time & season simulation** (§4.7) — drives farming, needs, and wear.
7. **World structure** (single scene vs streamed).

### 6.4 Performance targets `[TBD]`
Target hardware, resolution, framerate. Low-poly makes this easy; still set numbers.

### 6.5 Target platform `[DECIDED]`
**PC / Windows first**, via itch.io (then optionally Steam). Linux/Deck/console out of scope for v1.0 — clean, co-op-aware code keeps a Linux build feasible later.

### 6.6 DLC / modding architecture `[PROPOSED]` — decide early, it's expensive to retrofit
Build all content as **data-driven, additively-loaded packages from day one** (e.g. Unity **Addressables**), so a scooter/boat/city DLC is a content bundle that never touches core code. Consider light mod support for community content later.

### 6.7 Co-op-aware design `[DECIDED]` — see ADR-0003
Co-op is deferred past v1.0, but we avoid decisions that make netcode impossible later:
- Keep the **simulation authoritative and serializable**, and separate **simulation from input and presentation** (this also helps the save system).
- Don't scatter game state across unsynced singletons; route it through the part/save systems.
- No networking code now (it would balloon scope). Unity **Netcode for GameObjects** can layer on later if state stays clean.

### 6.8 Presentation & localization `[DECIDED]`
- **First-person** camera (§2).
- **Localization from day one — Dutch + English.** All player-facing text goes through string tables (Unity Localization package), never hard-coded, so more languages can be added later. Dutch is the authentic voice; English for reach.

---

## 7. Tools & Environment `[PROPOSED]`

| Need | Tool |
|---|---|
| Engine | Unity 6 (see ADR-0001) |
| 3D modelling | Blender |
| Textures | Blender / Krita / GIMP (all free) |
| Audio | Audacity / free DAW; Freesound (CC) |
| Version control | Git + Git LFS on **GitHub** (§8) |
| Project mgmt | **GitHub Projects + Issues** |
| Comms | Discord |
| CI (later) | GitHub Actions + build automation |

---

## 8. Version Control & Collaboration

### 8.1 System — **ADR-0002** `[DECIDED: Git + Git LFS on GitHub]`
**Chosen: Git + Git LFS, hosted on GitHub.** Rationale / alternative below.
- **Option A — Git + Git LFS** (chosen):
  - Track `Assets/`, `Packages/`, `ProjectSettings/`. Ignore `Library/`, `Temp/`, `Logs/`, `Build(s)/`, `obj/`, `UserSettings/` (use the standard Unity `.gitignore`).
  - **Keep `.meta` files.** Set Editor to **Force Text** asset serialization + **Visible Meta Files** for merge-friendliness.
  - **Git LFS** for binaries (`.png .psd .fbx .blend .wav .ogg .tga` …) via `.gitattributes`.
  - Configure Unity **Smart Merge (UnityYAMLMerge)** for scene/prefab conflicts.
- **Option B — Unity Version Control** (recommended if artists/non-coders are on the team): better with large binaries and locking; cloud seats becoming free in 2026. Less universal than Git.

### 8.2 Branching model `[PROPOSED]`
- `main` = always buildable. Feature branches → Pull Request → review → merge.
- Protect `main`. At least one review per PR.
- **Scene-locking discipline:** avoid two people editing the same scene/prefab simultaneously (binary merges are painful). Split work by feature/scene/prefab ownership.

### 8.3 Repo structure `[DECIDED]`
```
/ (repo root)
  Assets/
    Art/            # models, textures, materials (LFS)
    Audio/          # sfx, music (LFS)
    Code/           # C# scripts (systems, gameplay)
    Prefabs/        # objects, parts
    Scenes/         # world, greybox
    Settings/       # render pipeline, input, localization
    Data/           # ScriptableObjects: parts, recipes, objectives
  Packages/
  ProjectSettings/
  docs/             # this plan + ADRs + art-direction spec
  CREDITS.md        # CC0 / CC-BY asset attributions
  .gitignore  .gitattributes   # Unity template + Git LFS rules
```

### 8.4 Working in a group `[TBD]`
Ownership map (who owns art / core systems / world / audio), cadence, definition of "done." Fill after team is known (§9).

---

## 9. Team & Roles `[OPEN]` — see §14 Q5
- Who's in the group and rough skills (code / 3D art / audio / design)?
- Role assignments and a light working agreement.

---

## 10. Release & Live-ops

### 10.1 Versioning `[PROPOSED]`
Semantic-ish: `MAJOR.MINOR.PATCH`.
- MAJOR = big milestone / breaking save changes.
- MINOR = content updates / features.
- PATCH = bug fixes.
- Tag every release in git; keep a public changelog.

### 10.2 Update process `[TBD]`
Cadence, release branch, testing gate, save-compatibility policy (never silently break saves).

### 10.3 DLC delivery `[PROPOSED]`
Ships as an additive content package (§6.6). **Intent: free now, commercial-ready later** `[DECIDED]` — early content is free, but we architect DLC to support optional paid delivery later without rework.

### 10.4 Distribution `[PROPOSED]`
Free release on itch.io first (easiest, dev-friendly); keep Steam open as a later option. Because we may go commercial, treat licensing/IP as commercial-grade from day one (CC0-first assets, §5.4) so nothing blocks a future paid release. If revenue ever nears Unity's $200k Personal threshold, revisit the license tier.

---

## 11. Quality — Bug Reporting & QA

### 11.1 Bug tracking `[DECIDED]`
- **GitHub Issues** with a **bug-report template**: build version, repro steps, expected vs actual, severity, attached save + log + system info.
- Later: in-game "Report a bug" that bundles log + current save + system specs.

### 11.2 Severity levels `[PROPOSED]`
`S1 crash/save-loss` · `S2 blocks progression` · `S3 wrong behaviour` · `S4 cosmetic`.

### 11.3 Triage & testing `[TBD]`
Triage cadence; who owns it; playtest rounds before each release; regression checklist for the part/save systems.

---

## 12. Roadmap / Milestones `[PROPOSED]`

| Milestone | Goal | Exit criteria |
|---|---|---|
| **M0 Pre-production** | This doc + art spec + engine locked + tech spike | ADR-0001 decided; §5.1 spec drafted; project skeleton in VC |
| **M1 Vertical slice** | **Bike(s)** fully assemble (bolt-level)→operate→wear→repair, across **city + race frame archetypes**, ridden in a greybox **city street loop** and a greybox **race track loop**, in final art style | Deep part/fastener + save systems proven; both frame archetypes handle distinctly; core loop *fun* and *on-style* end-to-end |
| **M2 Flagship** | The **windmill** built on the proven systems | Second object working; systems generalized |
| **M3 Base game (v1.0)** | World, both objects, save, chosen immersion layer, polish | Shippable base game / demo |
| **M4+ DLC** | Scooter → boat → cities | Each as a clean additive package |

Rationale: **bike before windmill** — it's the simpler object, so it de-risks the core systems and pipeline before the flagship.

**Current target `[DECIDED]`:** M1 — the **bike vertical slice**.

---

## 13. Decision Log (ADRs)

> **ADR template**
> ```
> ### ADR-XXXX: <title>
> Status: Proposed | Accepted | Superseded by ADR-YYYY
> Date: YYYY-MM-DD
> Context: <why a decision is needed>
> Decision: <what we chose>
> Consequences: <trade-offs, follow-ups>
> ```

### ADR-0001: Game engine
**Status:** Accepted (2026-07-19)
**Date:** 2026-07-19
**Context:** Need an engine matching a low-poly, physics-heavy, first-person build-sim in the MSC style, buildable by a small group, with an accessible art pipeline.
**Decision:** **Unity 6.**
**Consequences:**
- ➕ Same lineage as MSC; C# matches team skills; largest low-poly asset ecosystem; Unity Personal free under $200k revenue; splash optional on Unity 6; possible free Education license via Avans; free cloud Version Control seats arriving 2026.
- ➖ Proprietary (not open-source); paid Pro tier only becomes relevant above the revenue threshold.
- **Alternative considered — Godot 4.6:** free/MIT, Jolt physics default, Python-like scripting. Fallback if license-freedom or full open-source is a priority. **Unreal rejected:** C++/photoreal pipeline fights the retro low-poly look.

### ADR-0002: Version control
**Status:** Accepted (2026-07-19) — **Git + Git LFS, hosted on GitHub** (§8.1). Revisit only if non-coder artists join who'd prefer Unity Version Control's file-locking model.

### ADR-0003: Multiplayer strategy
**Status:** Accepted (2026-07-19)
**Context:** Co-op is wanted eventually but not for v1.0; netcode is expensive and risky if retrofitted onto messy state.
**Decision:** Ship v1.0 single-player, but design core systems **co-op-aware** from the start (authoritative, serializable, sim/input/presentation separated). Defer all actual networking to post-1.0.
**Consequences:** Small discipline cost now (clean state ownership — which also improves the save system) in exchange for keeping co-op feasible later without a rewrite.

### ADR-0004: Rendering aesthetic
**Status:** Accepted (2026-07-19)
**Context:** Style is the top pillar; team chose a full PSX/PS1 look (more retro than MSC).
**Decision:** Implement a PSX-style render path — vertex snapping/jitter, affine texture mapping, low-res point-filtered textures, dithering, vertex lighting, fog — applied uniformly to all assets via the pipeline (§5.3).
**Consequences:** Strong cohesive identity + cheap performance + hides low-poly seams; requires a PSX shader/render setup (Asset Store kits exist, or build custom) and a conform step for every asset. Locks the game to a stylized (not photoreal) look — intended.

### ADR-0005: Multiple bike archetypes + city/race environments as v1.0 scope
**Status:** Accepted (2026-07-19)
**Date:** 2026-07-19
**Context:** The bike was originally scoped as a single vehicle (§3.1) with further variants (scooter) deferred to DLC. We want v1.0 to already showcase deep, varied customization — multiple frames with different mechanics (city vs race), not just one bike — and a place to actually feel the difference (a race track alongside the city/farm roads).
**Decision:** Ship **v1.0** with at least two bike archetypes — **city** (upright, fenders, rack, comfort) and **race** (drop-bar, light, stiff, fast) — both built from the same Part & Fastener system (`PART_SYSTEM_DESIGN.md`) as data, not new code. Add a **race track** greybox environment alongside the existing **city/village streets**, so both archetypes have a proper environment to prove their mechanics differ. This absorbs what was previously a "scooter DLC" idea one step earlier: the *system* now supports multiple frame archetypes from day one; the scooter (and further archetypes: cargo bike, BMX, kids' bike) becomes an even cheaper future DLC (§3.2).
**Consequences:**
- ➕ Forces the part/fastener system (§6.3) to be frame-archetype-generic from the start, which was the plan anyway (§7 of `PART_SYSTEM_DESIGN.md`) — no rework, just earlier validation.
- ➕ Race track environment is reusable later (time trials, an objective/minigame, future vehicle DLC testing ground).
- ➖ M1 scope grows: two frames + two greybox environments instead of one bike + one farmyard. Slightly larger vertical slice, but de-risks generalization earlier rather than after the windmill.
- Mechanics differences (handling, weight, gearing, top speed) between city/race frames: `[TBD]`, tune during M1.

*(add new ADRs below as decisions are made)*

---

## 14. Open Questions (answer these to lock the goal)

**Resolved:** Q1 → **Unity** · Q2 → **MSC-deep assembly** · Q3 → **Full MSC-style survival** · Q4 → **SP for v1.0, co-op-aware for later** · Q6 → **Free now, commercial-ready** · Q7 → **Small region, West-Brabant** · Q8 → **Hybrid (CC0 base + custom hero), full PSX look**

| # | Question | Why it matters | Default |
|---|---|---|---|
| Q5 | **Team:** how many people, and what skills (code/art/audio)? | Drives VC choice, roles, ownership | TBD (parked) |

---

## 15. Glossary & Changelog

**Glossary:** *ADR* = Architecture Decision Record. *CC0* = public-domain license, commercial-safe, no attribution. *DCC* = Digital Content Creation tool (e.g. Blender). *Vertical slice* = one small piece of the game built to final quality to prove the whole.

**Changelog:**
- `v1.0 (2026-07-19)` — Locked (ADR-0005): v1.0 ships **multiple bike archetypes** (city + race, distinct frames/mechanics, same part system) and a **race track** environment alongside the city/village streets, moving this up from a deferred DLC idea. M1 broadened accordingly (§12).
- `v0.9 (2026-07-19)` — Working title = "My Summer Bike"; M1 (bike vertical slice) confirmed as first target. Plan is now decision-complete; remaining items are detail specs (handled in companion docs) + the parked team question.
- `v0.8 (2026-07-19)` — Locked: PC/Windows-first (ADR platform); Git + Git LFS on GitHub + GitHub Projects/Issues (ADR-0002 accepted); diegetic save points; finalized repo structure.
- `v0.7 (2026-07-19)` — Locked: first-person; day/night + seasons time sim (drives farming); localized Dutch + English from day one.
- `v0.6 (2026-07-19)` — Locked art direction: hybrid pipeline (CC0 base + custom hero), full PSX/PS1 rendering (ADR-0004), muted overcast Dutch palette. Resolved Q8.
- `v0.5 (2026-07-19)` — Locked: world = small region, West-Brabant (Bergen op Zoom/Steenbergen/Dinteloord vibe); premise = inherit grandparents' farm, finish their half-built mill & bike. Resolved Q7.
- `v0.4 (2026-07-19)` — Locked: farming = full production loop + main economy (mill-driven); goal structure = sandbox + optional objectives; tone = grungy/adult (mature rating). Defined the dual core loop.
- `v0.3 (2026-07-19)` — Locked: MSC-deep assembly (Q2); SP for v1.0 + co-op-aware architecture (Q4, ADR-0003). Elevated part/fastener + save systems as core.
- `v0.2 (2026-07-19)` — Locked: engine = Unity (ADR-0001 accepted); survival = full MSC-style; intent = free now, commercial-ready. Resolved Q1/Q3/Q6.
- `v0.1 (2026-07-19)` — Initial scaffold created from kickoff discussion.
