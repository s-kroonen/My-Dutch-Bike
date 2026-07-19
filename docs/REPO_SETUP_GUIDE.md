# My Summer Bike — Repo Skeleton & Setup Guide

> Companion to `PROJECT_MOLEN_PLAN.md` §8. Copy-paste the files below to stand up the GitHub repo the way the plan specifies: **Unity 6 + Git + Git LFS on GitHub**, merge-friendly settings, and templates for ADRs and bug reports.
> Do this once, before real code. Move to code after.

---

## 1. Folder structure
```
my-summer-bike/
  Assets/
    Art/        Audio/     Code/      Prefabs/
    Scenes/     Settings/  Data/
  Packages/
  ProjectSettings/
  docs/
    PROJECT_MOLEN_PLAN.md
    ART_DIRECTION_SPEC.md
    PART_SYSTEM_DESIGN.md
    adr/ADR-template.md
  .github/
    ISSUE_TEMPLATE/bug_report.md
    pull_request_template.md
  README.md   CONTRIBUTING.md   CREDITS.md
  .gitignore  .gitattributes
```

## 2. One-time setup (in order)
1. **Install:** Unity 6 (LTS) + Unity Hub, Git, **Git LFS** (`git lfs install`).
2. **Create the Unity project** (URP template) named `my-summer-bike`.
3. **Editor settings for merge-friendliness** (Project Settings → Editor):
   - *Asset Serialization → Mode:* **Force Text**
   - *Version Control → Mode:* **Visible Meta Files**
4. **Add** `.gitignore`, `.gitattributes`, `README.md`, `CONTRIBUTING.md`, `CREDITS.md`, and the `.github/` + `docs/` files below.
5. **Init & first commit:**
   ```
   git init
   git lfs install
   git add .gitattributes && git commit -m "chore: git lfs + attributes"
   git add . && git commit -m "chore: initial Unity project skeleton"
   ```
6. **Create the GitHub repo**, then:
   ```
   git remote add origin git@github.com:<org>/my-summer-bike.git
   git push -u origin main
   ```
7. **Configure Smart Merge** (once per machine) so Unity resolves scene/prefab conflicts:
   set `merge.unityyamlmerge` in your global git config to Unity's `UnityYAMLMerge` tool (path under the Unity Editor `Tools/` folder).
8. **Protect `main`** on GitHub: require a pull request + ≥1 review before merge.
9. **Enable GitHub Projects + Issues** for tracking (plan §7, §11).

---

## 3. `.gitignore` (Unity)
```gitignore
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emoryCaptures/
[Rr]ecordings/
.vs/
.vsconfig
*.csproj
*.sln
*.userprefs
*.pidb
*.booproj
*.svd
.DS_Store
Assets/AssetStoreTools*
crashlytics-build.properties
sysinfo.txt
```
> Keep `Assets/`, `Packages/`, `ProjectSettings/` **tracked**. Never ignore `.meta` files.

## 4. `.gitattributes` (LFS + line endings + Unity merge)
```gitattributes
* text=auto

# Unity YAML — use Unity's Smart Merge, treat as text
*.unity   merge=unityyamlmerge eol=lf
*.prefab  merge=unityyamlmerge eol=lf
*.asset   merge=unityyamlmerge eol=lf
*.meta    merge=unityyamlmerge eol=lf
*.controller merge=unityyamlmerge eol=lf

# Binary assets → Git LFS
*.png  filter=lfs diff=lfs merge=lfs -text
*.jpg  filter=lfs diff=lfs merge=lfs -text
*.tga  filter=lfs diff=lfs merge=lfs -text
*.psd  filter=lfs diff=lfs merge=lfs -text
*.fbx  filter=lfs diff=lfs merge=lfs -text
*.blend filter=lfs diff=lfs merge=lfs -text
*.wav  filter=lfs diff=lfs merge=lfs -text
*.ogg  filter=lfs diff=lfs merge=lfs -text
*.mp3  filter=lfs diff=lfs merge=lfs -text
*.ttf  filter=lfs diff=lfs merge=lfs -text
*.otf  filter=lfs diff=lfs merge=lfs -text
```

## 5. `docs/adr/ADR-template.md`
```markdown
# ADR-XXXX: <title>
Status: Proposed | Accepted | Superseded by ADR-YYYY
Date: YYYY-MM-DD

## Context
<why a decision is needed>

## Decision
<what we chose>

## Consequences
<trade-offs, follow-ups>
```

## 6. `.github/ISSUE_TEMPLATE/bug_report.md`
```markdown
---
name: Bug report
about: Something broken or wrong
labels: bug
---

**Build version:**
**Severity:** S1 crash/save-loss | S2 blocks progression | S3 wrong behaviour | S4 cosmetic

**Steps to reproduce:**
1.

**Expected:**
**Actual:**

**Attachments:** save file · Player.log · system specs
```

## 7. `.github/pull_request_template.md`
```markdown
## What & why

## Testing done

## Checklist
- [ ] `main` stays buildable
- [ ] Didn't co-edit a scene/prefab someone else is in (see CONTRIBUTING)
- [ ] Assets pass the PSX-conform checklist (ART_DIRECTION_SPEC §9)
```

## 8. `CONTRIBUTING.md` — working agreement (essentials)
- **`main` is always buildable.** Work on feature branches → PR → ≥1 review → merge.
- **Scene/prefab discipline:** coordinate before two people edit the same scene or prefab; binary-ish merges are painful. Split work by feature/scene/prefab ownership.
- **Every non-trivial decision → an ADR** in `docs/adr/`.
- **Assets go through the PSX-conform checklist** before merge.
- **Log CC0/CC-BY assets** in `CREDITS.md`.

## 9. `README.md` (starter)
```markdown
# My Summer Bike
A first-person, PSX-styled build-and-maintain sim set in rural West-Brabant, ~2000.
Inherit the farm, finish your grandparents' bread mill and bike, make it pay.

See `docs/PROJECT_MOLEN_PLAN.md` for the full plan.

## Requirements
Unity 6 (LTS), Git, Git LFS.

## Setup
`git lfs install` before first clone/checkout. Open in Unity 6.
```

---

## 10. Next after the repo exists
Per the roadmap, M1 = the **bike vertical slice**: implement the Part & Fastener system (`PART_SYSTEM_DESIGN.md`) on the bike BOM, in the PSX look (`ART_DIRECTION_SPEC.md`), in a greybox farmyard — assemble → ride → wear → repair, end to end.
