# CanalTD Unity Project Guide

This file is the **inner project guide** for the Unity folder.  
The **official assessment-facing README** is the repository root README:

- [../../README.md](../../README.md)

If someone opens the Unity project folder directly on GitHub, this file should help them understand where to look next.

## What this folder contains

`CanalTD` is the main Unity project for this final game submission.

## Key folders

- `Assets/Scenes`
  - Main scenes including Bootstrap, Home, Mode Select, Guide, Game, and Result
- `Assets/Scripts`
  - Gameplay, AI, pathfinding, UI, tutorial, and networking scripts
- `Assets/Docs`
  - Supporting reference and documentation files
- `ProjectSettings`
  - Unity project configuration
- `Packages`
  - Unity package dependencies

## Recommended reading order

1. Official game README:
   - [../../README.md](../../README.md)
2. Contribution and peer-feedback notes:
   - [./CONTRIBUTIONS_AND_FEEDBACK.md](./CONTRIBUTIONS_AND_FEEDBACK.md)
3. Development log:
   - [../DEVELOPMENT_LOG.md](../DEVELOPMENT_LOG.md)
4. Resource / asset / AI references:
   - [./Assets/Docs/ResourceReferences.md](./Assets/Docs/ResourceReferences.md)
5. Early concept document:
   - [../prototype.md](../prototype.md)

## Quick project facts

- Game title: `CanalTD`
- Unity version: `2022.3.62f3`
- Recommended entry scene: `Assets/Scenes/BootstrapScene.unity`
- Project type: turn-based multiplayer strategy / tower-defense vertical slice

## Final scene flow

The final assessment-facing flow starts from `BootstrapScene`, then uses `HomeScene` and `ModeSelectScene` to enter `GuideScene` or the main `GameScene`, with match outcomes shown through `ResultScene`.

## Why this structure is useful

- The root README explains the game as an assessable coursework submission.
- This inner README explains the Unity project layout.
- Contribution, development, and resource files are separated so markers can find evidence quickly.
