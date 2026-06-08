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
2. Final game design:
   - [../FINAL_GAME_DESIGN.md](../FINAL_GAME_DESIGN.md)
3. Testing log:
   - [../TESTING_LOG.md](../TESTING_LOG.md)
4. Contribution and peer-feedback notes:
   - [./CONTRIBUTIONS_AND_FEEDBACK.md](./CONTRIBUTIONS_AND_FEEDBACK.md)
5. Development log:
   - [../DEVELOPMENT_LOG.md](../DEVELOPMENT_LOG.md)
6. Resource / asset / AI references:
   - [./Assets/Docs/ResourceReferences.md](./Assets/Docs/ResourceReferences.md)
7. Early concept document:
   - [../prototype.md](../prototype.md)

## Quick project facts

- Game title: `CanalTD`
- Unity version: `2022.3.62f3`
- Recommended entry scene: `Assets/Scenes/BootstrapScene.unity`
- Project type: turn-based multiplayer strategy / tower-defense vertical slice

## Final playable scene flow

`BootstrapScene` -> `HomeScene` -> `ModeSelectScene` -> Local / AI mode -> `GameScene` -> `ResultScene`

`GuideScene` is the onboarding/help route for learning the rules and controls.

## Primary assessment modes

- Local Mode: primary assessment route.
- AI Mode: primary assessment route when human opponents are not available.
- Online Mode: implemented extension; broader multiplayer validation is required before treating it as the stable marking route.

## Important documentation

- Root README: [../../README.md](../../README.md)
- Final game design: [../FINAL_GAME_DESIGN.md](../FINAL_GAME_DESIGN.md)
- Testing log: [../TESTING_LOG.md](../TESTING_LOG.md)
- Kanban and issue evidence: [../KANBAN_AND_ISSUE_EVIDENCE.md](../KANBAN_AND_ISSUE_EVIDENCE.md)
- Development log: [../DEVELOPMENT_LOG.md](../DEVELOPMENT_LOG.md)
- Submission checklist: [../SUBMISSION_CHECKLIST.md](../SUBMISSION_CHECKLIST.md)
- Contribution and feedback notes: [./CONTRIBUTIONS_AND_FEEDBACK.md](./CONTRIBUTIONS_AND_FEEDBACK.md)
- Unity system overview: [./SYSTEM_OVERVIEW.md](./SYSTEM_OVERVIEW.md)
- Online mode status: [./ONLINE_STATUS.md](./ONLINE_STATUS.md)
- Resource references: [./Assets/Docs/ResourceReferences.md](./Assets/Docs/ResourceReferences.md)

## Prototype and draft files

- `GameScene_AIPrototype.unity` is a prototype/AI development scene and is not the primary final route.
- `GameScene.unity.bak_before_layout_edit` is a scene-layout backup and is not used for final assessment.
- `Assets/Docs/v3.docx` is treated as a draft/report-like document. The assessment-facing Markdown documents linked above are the final documentation entry points.
- UI debug/probe helper scripts such as `ManualUIButtonDebug.cs`, `UIClickProbe.cs`, `UIRaycastCleaner.cs`, and `UIRaycastDebugger.cs` are development support utilities, not main gameplay systems.

## Why this structure is useful

- The root README explains the game as an assessable coursework submission.
- This inner README explains the Unity project layout.
- Contribution, development, and resource files are separated so markers can find evidence quickly.
