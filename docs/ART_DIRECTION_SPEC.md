# My Summer Bike — Art-Direction Spec

> Companion to `PROJECT_MOLEN_PLAN.md` §5. This is the **styling bible** — the numbers every asset obeys so a hybrid CC0 + custom library reads as one cohesive game.
> **Status:** v0.1 — starting values. Tune during the M1 bike vertical slice, then freeze.
> Status tags: `[DECIDED]` locked · `[START]` starting value, expect to tune · `[TBD]` to define.

---

## 1. Look in one sentence
A **full PSX/PS1 aesthetic** — wobbling vertices, warped affine textures, low-res dithered image — wrapped around **My Summer Car's grimy, hand-built rural subject matter**, lit as **muted, overcast West-Brabant**. More retro than MSC; same lived-in grime. `[DECIDED]`

---

## 2. Rendering (the PSX look)

| Property | Value | Notes |
|---|---|---|
| Internal render resolution | **~360p height** (e.g. 640×360), point-upscaled to window | `[START]` — lower (320×240) = harder retro |
| Color depth | **15–16-bit** feel via ordered **Bayer 4×4 dithering** | post-process step |
| Texture filtering | **Point (nearest)**, no bilinear | on all in-world textures |
| Mipmaps | Off (or nearest, no trilinear) | keeps the shimmer |
| Perspective correction | **Off — affine texture mapping** | the "warp"; in shader |
| Vertex snapping | Snap to a **low grid** in clip space, grid ≈ render height | the "wobble"; expose `SnapResolution` |
| Lighting | **Vertex lighting only**, no per-pixel; bake where possible | low-res lightmaps ok |
| Shadows | Minimal / blob or low-res; avoid crisp realtime | overcast = soft anyway |
| Fog | **Linear fog** to hide draw distance | start ~20 m, end ~80 m `[START]` |
| Anti-aliasing | **None** | AA fights the aesthetic |
| Post FX | Dither + color-reduce; optional slight noise/vignette | keep subtle |

**Unity implementation `[START]`:** URP on Unity 6. Do the low-res look via a **low render-scale / render-to-RenderTexture then point-upscale**. Custom shaders handle **vertex snap + affine mapping**; a full-screen pass does **dither + color reduction**. PSX shader kits exist on the Asset Store (or build custom — the two shader tricks are short). Log the choice as an ADR when picked.

---

## 3. Palette — muted overcast Dutch `[START]`

Desaturated, low-contrast, cool-neutral. Starting swatches (tune in-engine):

| Role | Hex | Use |
|---|---|---|
| Overcast sky / fog | `#AEB4B8` | skybox, fog color |
| Cool grey-blue | `#8A939A` | shadows, distant |
| Muted green | `#6B7355` | grass, foliage |
| Deep muted green | `#4E5742` | trees, darks |
| Field brown | `#7A6A52` | soil, wood |
| Dark earth | `#5C4D3A` | mud, timber |
| Muted brick red | `#8A5A4A` | brickwork, roofs |
| Off-white / plaster | `#D8D4C8` | walls, mill body |
| Warm accent (rare) | `#C9A24B` | grain, warm lights — use sparingly |

Rule: keep saturation low; let the **rare warm accent** (grain, a lit window) pop against the grey. `[DECIDED: low-sat, cool, sparse warm accents]`

---

## 4. Poly budgets `[START]` (triangles)

| Class | Budget | Examples |
|---|---|---|
| Small prop | 50–300 | tools, bottles, bolts |
| Bike/mill part | 50–500 | wheel, gear, sail-arm |
| Assembled hero object | 2k–6k total | full bike, full mill |
| Character | 800–1,500 | player hands, NPCs |
| Building | 300–1,500 | farmhouse, shed |
| On-screen scene total | < ~80–120k | keep it light |

Low counts are *stylistic*, not just performance — sharp facets are part of the look.

---

## 5. Textures `[START]`
- Resolution: props **64–128**, hero assets up to **256**, terrain atlases **256–512**. Powers of two.
- Style: hand-painted-ish, baked-in dirt/wear and fake shadow. **No normal maps.** One albedo per material where possible.
- Trim sheets / atlases to keep material (draw-call) count low.
- Slight grime pass on everything — nothing factory-clean.

## 6. Lighting & camera `[START]`
- **Overcast key:** soft directional, low intensity, cool tint; high ambient, low contrast; no harsh sun.
- Time-of-day + seasons (plan §4.7) shift color temp and light angle; keep all seasons within the muted palette.
- **First-person FOV ~65–70.** Head-bob subtle. Near/far planes tuned so fog does the far-hiding.

## 7. HUD & type `[START]`
- **Minimal, mostly diegetic** (like MSC — read state from the world, not a HUD). Only essential prompts.
- Font: a **pixel/bitmap font**; a second period-Dutch signage face for world text (shop signs, packaging). `[TBD: pick fonts]`
- All UI text via localization string tables (Dutch + English), never baked into textures where avoidable.

## 8. Audio direction (brief) `[TBD]`
Compressed, slightly lo-fi; period Dutch **radio** vibe; mechanical foley (ratchets, chain, sails, engine); rural ambience (wind, birds, distant traffic). Mono-leaning for retro feel.

---

## 9. PSX-conform checklist (every asset passes this before entering the game)
- [ ] Poly count within its class budget (§4)
- [ ] Textures ≤ class max, power-of-two, **point-filtered**, no normal map (§5)
- [ ] Materials minimized (atlas/trim where possible)
- [ ] Colors sit within the muted palette (§3)
- [ ] Uses the project PSX shader (snap + affine) (§2)
- [ ] Grime/wear pass applied — nothing pristine
- [ ] (CC0 assets) license logged in `CREDITS.md`; retextured to spec, not raw-dropped

---

## 10. Open art items `[TBD]`
- Final render resolution + snap grid (decide by eye on the bike)
- Fog distances per scene
- Font choices
- Audio palette
- Whether to add subtle CRT/scanline option (probably off by default)
