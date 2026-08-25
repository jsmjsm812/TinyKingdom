# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**위치 아워 (Witch Hour)** — a 2D pixel-art roguelite tower defense for mobile (portrait), built in Unity 6 / C# / UGUI, as a job-hunting portfolio piece (Unity client programmer + game planner tracks).

Full design reference lives in `Docs/GDD.md` — read it before implementing any gameplay system. It has the exact numbers (stats, costs, wave tables, save schema) that code should match.

## Current status

Project just created (Unity 6, 2D URP template). No gameplay code written yet. Following the 3-week roadmap in `Docs/GDD.md` — currently **Week 1: battle prototype**.

## Commands

No CLI build/test pipeline yet — this is early-stage Unity dev, verified via the Editor:
- Open the project in Unity Hub → Unity 6 Editor → Play Mode to test.
- No automated test suite exists yet. If you add one, use Unity Test Framework (`Assets/Tests/`) and document the run command here.

## Architecture

```
Assets/
  01_Scripts/
    Data/       ScriptableObject class definitions (GuardianData, InvaderData, WaveData, ZoneData...)
    Field/      Grid/slot system, lane, placement
    Combat/     Auto-attack, projectiles, damage resolution
    Shop/       Summon shop (4 slots + reroll), tab UI
    Merge/      Auto-merge logic (star-up on 3-of-a-kind)
    Core/       Game/run state, wave spawner, save/load, currency
    UI/         Non-shop UI (result screen, guardian codex, settings)
  02_Data/
    Guardians/  GuardianData .asset instances (one per 수호자, 10 total)
    Invaders/   InvaderData .asset instances (one per 침입자, 4 total)
  03_Art/Sprites, 03_Art/Fonts, 04_Audio/SFX, 04_Audio/BGM, 05_Prefabs, 06_Scenes, 07_Settings
Docs/
  GDD.md        condensed design reference (stats, costs, wave/zone tables, save schema)
```

Top-level `Assets/` folders are numbered (`01_Scripts`, `02_Data`, ...) to control sort order in the Unity Project window — keep new top-level folders consistent with this scheme (pick the next free number, or slot into the existing sequence) rather than adding unnumbered ones.

**Data-driven design is the whole point of this project** (both as an architecture choice and as a portfolio talking point): every 수호자/침입자/웨이브/구역 is a ScriptableObject asset, not a hardcoded class or switch statement. Adding a new guardian should mean "create one .asset in the Inspector," never "write a new C# class."

Key systems and how they connect:
- **Shop → Roster → Field**: 소환 상점 (4 always-visible slots, buy or reroll) produces a guardian into 소환 명부 (an unplaced-roster list, capped at 8 with scroll) → player drags into a field slot → it auto-attacks. Shop and roster are **tabs**, never shown simultaneously (mockup reference).
- **Merge is automatic**, not player-triggered: whenever 3 copies of the same name *and* same star level exist (bench or field, doesn't matter), they collapse into 1 copy at star+1. There is no "강화"/leveling system — merging is the only way a guardian gets stronger. Don't add a level-up mechanic; it was deliberately removed (see GDD history if curious why).
- **Rarity (★) vs star (성) are independent axes.** Rarity is fixed at summon time and never changes. Star only changes via merge. Keep these as separate fields on the runtime unit instance, not conflated.
- Run state (마나결정, field, roster, merge progress) resets every 출전; only `unlockedSpirits`, `zoneCleared`, `honorSeals` persist across runs (see save schema in GDD.md).

## Conventions

- Korean is fine in code comments/asset names where it aids clarity (this is a Korean-market portfolio project) — but public API/class/method names should stay in English (`GuardianData`, `TryPurchaseSlot()`), matching normal C#/Unity convention.
- No defense stat, no rubber-band difficulty — damage is always `공격력` directly, no subtraction term. Keep it that way; it's an intentional simplicity choice documented in GDD.md.
- Prefer ScriptableObject + Inspector-tunable fields over hardcoded constants for anything that's a design number (costs, probabilities, stat values) — designers/you will rebalance these constantly.
