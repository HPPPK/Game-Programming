# Kanban and Issue Evidence

This document summarises how GitHub Issues, the `Game-Programming` Project/Kanban board, and the main milestone support the process evidence for the individual CanalTD coursework submission.

Useful links:

- GitHub Project board: https://github.com/users/HPPPK/projects/2
- Closed development issues: https://github.com/HPPPK/Game-Programming/issues?q=is%3Aissue%20is%3Aclosed
- Open issues / remaining limitations: https://github.com/HPPPK/Game-Programming/issues?q=is%3Aissue%20is%3Aopen
- Main milestone: https://github.com/HPPPK/Game-Programming/milestone/2

The repository may show `Issues 0` in the top navigation because there are currently no open issues. This does not mean issues were unused. Completed work was closed after implementation, which is normal task management and useful process evidence.

The milestone name `Initial game design` is historical. It contains both design and implementation tasks, including gameplay systems, UI, tutorial work, audio, and online-mode tasks. The root `README.md` and the other final documentation files should be treated as the authoritative final submission entry point.

## Evidence table

| Area | Example issue(s) | What the issue proves | Related system / file / scene | Assessment value |
| --- | --- | --- | --- | --- |
| Enemy routing and map strategy | [Strategic Enemy Route Assignment Algorithm #29](https://github.com/HPPPK/Game-Programming/issues/29) | The issue defines the need for enemies to choose routes intelligently, collect valid paths, prefer longer routes, rebalance after gate changes, and avoid blocked gates. | `Assets/Scripts/Enemy/EnemyPathAssignmentManager.cs`, `Assets/Scripts/Path`, `GameScene` | Shows planning for core gameplay intelligence, strategic depth, iteration, and acceptance criteria. |
| Card system | [Tutorial Part 3: Card System and Card Demonstrations #95](https://github.com/HPPPK/Game-Programming/issues/95) | The closed issue title and milestone entry show a tracked task for card-system tutorial demonstrations. | `Assets/Scripts/Card`, `Assets/Scripts/Tutorial`, `GuideScene` | Shows planning and completion evidence for a major interaction system and player onboarding. |
| Tower system | [Audio System Integration and Sound Effects Implementation #87](https://github.com/HPPPK/Game-Programming/issues/87), [Strategic Enemy Route Assignment Algorithm #29](https://github.com/HPPPK/Game-Programming/issues/29) | Tower-specific issue detail is not separately claimed here; tower behavior is evidenced through final system files and related combat/audio/routing integration tasks. | `Assets/Scripts/Tower`, `GameScene` | Shows tower defense is part of the integrated vertical slice while avoiding invented issue details. |
| Tutorial enemy-wave section | [Tutorial Part 5: Enemy Wave System #97](https://github.com/HPPPK/Game-Programming/issues/97) | The issue defines tutorial goals for explaining enemy waves, automatic tower attacks, blocked player actions during waves, and tutorial wave completion. | `Assets/Scripts/Tutorial/TutorialManager.cs`, `Assets/Scripts/Tutorial/TutorialEnemyDemoSpawner.cs`, `GuideScene` | Shows onboarding, accessibility, player guidance, and task completion evidence. |
| Tutorial turn-flow section | [Tutorial Part 4: End Turn System #96](https://github.com/HPPPK/Game-Programming/issues/96) | The closed issue title and milestone entry show a tracked task for teaching the End Turn system in the tutorial sequence. | `Assets/Scripts/Tutorial`, `Assets/Scripts/Core/TurnManager.cs`, `GuideScene` | Shows process evidence for turn-structure explanation and tutorial iteration. |
| Tutorial / onboarding | [Step by step instruction guidance #92](https://github.com/HPPPK/Game-Programming/issues/92), [Tutorial Part 3 #95](https://github.com/HPPPK/Game-Programming/issues/95), [Tutorial Part 4 #96](https://github.com/HPPPK/Game-Programming/issues/96), [Tutorial Part 5 #97](https://github.com/HPPPK/Game-Programming/issues/97) | The issue set shows tutorial and guide work was tracked through several focused tasks. | `Assets/Scripts/Tutorial`, `GuideScene` | Shows feedback response, accessibility, and onboarding process evidence. |
| Player identity, HUD, and scoring | [Player Information, Color, HUD, and Score #93](https://github.com/HPPPK/Game-Programming/issues/93) | The issue defines tutorial goals for player colors, castle ownership, HP, gold, card count, score, score as win condition, highlights, messages, and skip behavior. | `Assets/Scripts/UI`, `Assets/Scripts/Tutorial`, `GuideScene`, `GameScene` | Shows attention to clarity, controls, feedback, player goal communication, and assessability. |
| UI / turn marker | [Turn Marker Animation - User Indication #90](https://github.com/HPPPK/Game-Programming/issues/90), [Player Information, Color, HUD, and Score #93](https://github.com/HPPPK/Game-Programming/issues/93) | The issue titles show tracked work for active-player indication and player information clarity. | `Assets/Scripts/UI`, `GameScene`, `GuideScene` | Shows iteration based on readability and feedback needs. |
| Audio feedback and settings | [Audio System Integration and Sound Effects Implementation #87](https://github.com/HPPPK/Game-Programming/issues/87) | The issue records a centralized `AudioManager`, background music, UI sounds, gameplay sounds, persistent settings, PlayerPrefs, and scene compatibility goals. | `Assets/Scripts/UI/AudioManager.cs`, `BootstrapScene`, `HomeScene`, `ModeSelectScene`, `GameScene`, `ResultScene` | Shows polish, feedback, accessibility, integration planning, and acceptance criteria for audio behavior. |
| Online scene transition bug | [Online Match Start Does Not Synchronize Scene Transition #89](https://github.com/HPPPK/Game-Programming/issues/89) | The issue title shows tracked online scene-transition bug work. | `Assets/Scripts/Networking`, `ModeSelectScene`, `GameScene` | Shows bug-fix and iteration evidence for the online extension. |
| Online turn and lifecycle work | [Online Turn Authority & Player Lifecycle Goal #91](https://github.com/HPPPK/Game-Programming/issues/91) | The issue records online player roster, slot assignment, turn ownership, End Turn sync, player leave handling, host migration, and online exit-flow goals. | `Assets/Scripts/Networking`, `ModeSelectScene`, `GameScene` | Shows advanced extension planning and bug-fix/professional workflow without claiming online mode is the primary assessed path. |
| Mode selection and room flow | [Game Mode Selection & Online/AI Room System #62](https://github.com/HPPPK/Game-Programming/issues/62) | The issue title records task tracking for game-mode selection and online/AI room setup. | `Assets/Scripts/UI/ModeSelectSceneManager.cs`, `Assets/Scripts/Networking`, `ModeSelectScene` | Shows planning evidence for menu flow, mode entry, and extension scope. |
| Home screen and main menu | [Home Scene UI & Main Menu System #61](https://github.com/HPPPK/Game-Programming/issues/61) | The issue title records task tracking for the home scene and main menu system. | `Assets/Scripts/UI/HomeSceneManager.cs`, `HomeScene` | Shows planning evidence for first-screen usability, navigation, and professional presentation. |

## How markers should read this evidence

- Issues are process evidence for planning, implementation, iteration, and completion.
- The Project/Kanban board supports professionalism by showing task movement through planning, progress, and completion states.
- Closed issues should be read as completed tasks, not as missing or unused issue tracking.
- Testing still requires `FinalGame/TESTING_LOG.md`; issue closure does not by itself prove every Unity runtime path passed.

## How this supports the marking criteria

- Planning: issues break large systems into specific tasks and acceptance criteria.
- Progress tracking: the Project/Kanban board records movement through planning, progress, and completion states.
- Testing and validation: several issues include acceptance criteria such as no console errors, tutorial completion, correct synchronization goals, and scene compatibility. These are process criteria, not a claim that every runtime test has passed.
- Bug fixing and iteration: online lifecycle work includes bug/enhancement tracking, and closed issues show completed development tasks.
- Professionalism: closed issues, milestone completion, and final documentation give markers a direct route from process evidence to the submitted Unity systems.
