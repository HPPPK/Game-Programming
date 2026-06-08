# CanalTD Development Log

## Overview

This log summarises the main development process for the CanalTD individual coursework project. It records broad development periods, major milestones, problems encountered, changes made after feedback/testing, and links to GitHub Issues / Kanban evidence.

Exact development dates are not reconstructed here. The GitHub commit history, closed issues, and Project board provide the detailed chronological record.

## Development timeline

The periods below are broad development stages. Exact dates should be read from the commit history, closed issues, and Project board rather than reconstructed in this document.

| Date or period | Goal | Work completed | Evidence / issue / file | Problem encountered | Change made | Result |
|---|---|---|---|---|---|---|
| Early concept stage | Define a clear game idea and rules direction. | Developed the CanalTD concept around canal/path control, indirect competition, cards, towers, and enemy pressure. | [prototype.md](./prototype.md), [FINAL_GAME_DESIGN.md](./FINAL_GAME_DESIGN.md) | Early scope could become too broad if every multiplayer idea was treated as core. | Final design separates primary Local/AI route from Online extension. | Clearer final vertical-slice scope. |
| Core gameplay implementation | Build playable turn, card, tower, enemy, path, and result systems. | Implemented core student-authored systems under `FinalGame/CanalTD/Assets/Scripts`. | [SYSTEM_OVERVIEW.md](./CanalTD/SYSTEM_OVERVIEW.md) | Multiple systems needed to work together inside scenes, not only as separate scripts. | Scene flow and system overview document the integration points. | Main gameplay systems are easier for markers to inspect. |
| Enemy routing / map strategy | Make enemy pressure respond to route and gate state. | Developed route assignment and path support. | [Strategic Enemy Route Assignment Algorithm #29](https://github.com/HPPPK/Game-Programming/issues/29), `Assets/Scripts/Enemy/EnemyPathAssignmentManager.cs`, `Assets/Scripts/Path` | Fixed lanes would weaken the indirect strategy idea. | Enemy routing was designed around valid paths and pressure distribution. | Stronger core mechanic and clearer strategic identity. |
| Home/menu flow | Provide a clear first-screen route. | Added/maintained Home scene and main menu navigation. | [Home Scene UI & Main Menu System #61](https://github.com/HPPPK/Game-Programming/issues/61), `Assets/Scenes/HomeScene.unity` | Markers need a clear way to enter the game. | README and inner project guide identify the scene flow. | Easier assessment navigation. |
| Mode selection / AI / Online room work | Support Local, AI, Guide, and Online entry points. | Built mode selection and room setup support. | [Game Mode Selection & Online/AI Room System #62](https://github.com/HPPPK/Game-Programming/issues/62), `Assets/Scenes/ModeSelectScene.unity` | Online mode increased validation risk. | Local/AI route prioritised; Online documented as extension. | More realistic final submission scope. |
| Audio feedback | Add sound and music feedback. | Integrated central audio manager and mapped sourced audio. | [Audio System Integration and Sound Effects Implementation #87](https://github.com/HPPPK/Game-Programming/issues/87), [ResourceReferences.md](./CanalTD/Assets/Docs/ResourceReferences.md) | Audio sources needed clearer traceability. | Resource references now map current audio files to source links. | Stronger credits and feedback documentation. |
| Online extension work | Improve online scene and lifecycle behavior. | Added Photon-related room, card, build, and turn-sync support. | [Online Match Start Does Not Synchronize Scene Transition #89](https://github.com/HPPPK/Game-Programming/issues/89), [Online Turn Authority & Player Lifecycle Goal #91](https://github.com/HPPPK/Game-Programming/issues/91), [ONLINE_STATUS.md](./CanalTD/ONLINE_STATUS.md) | Full multiplayer edge-case validation is harder than Local/AI verification. | Online mode documented as implemented extension, not core marking route. | Honest scope and reduced final-assessment risk. |
| UI / tutorial polish | Improve clarity, onboarding, and feedback. | Added clearer turn indication, HUD/player information, Guide scene, and tutorial sections. | [Turn Marker Animation - User Indication #90](https://github.com/HPPPK/Game-Programming/issues/90), [Step by step instruction guidance #92](https://github.com/HPPPK/Game-Programming/issues/92), [Player Information, Color, HUD, and Score #93](https://github.com/HPPPK/Game-Programming/issues/93), [Tutorial Part 3 #95](https://github.com/HPPPK/Game-Programming/issues/95), [Tutorial Part 4 #96](https://github.com/HPPPK/Game-Programming/issues/96), [Tutorial Part 5 #97](https://github.com/HPPPK/Game-Programming/issues/97) | Players needed clearer explanation of whose turn it was and how systems worked. | Added guide/onboarding and stronger current-turn feedback. | Better accessibility and assessability. |
| Final documentation and submission preparation | Make the repository professional and assessment-facing. | Added final design, testing log, Kanban evidence, system overview, online status, resource references, and checklist. | `README.md`, [TESTING_LOG.md](./TESTING_LOG.md), [KANBAN_AND_ISSUE_EVIDENCE.md](./KANBAN_AND_ISSUE_EVIDENCE.md), [SUBMISSION_CHECKLIST.md](./SUBMISSION_CHECKLIST.md) | Documentation needed to avoid overclaiming runtime stability. | Used cautious manual-verification language where Unity was not run. | Clearer, more honest final submission evidence. |

## Major milestones

- Core concept and prototype documented.
- Local/AI vertical slice prioritised for final assessment.
- Enemy routing and path-pressure system developed.
- Card, turn, AP, tower, enemy, UI, tutorial, and result systems integrated.
- Online mode implemented as an extension with separate status documentation.
- GitHub Issues, Project/Kanban board, and milestone evidence linked for process review.

## Key problems and fixes

- Turn clarity was a usability issue, so clearer turn indication and status feedback were added.
- New players needed onboarding, so Guide scene and tutorial flow were added.
- Online mode carried validation risk, so it was documented as an extension rather than the core assessment route.
- Audio and resource references needed clearer traceability, so current audio files were mapped to source links.
- Historical design documents could be mistaken for final scope, so a final design document was created and historical files were labelled.

## What changed after feedback/testing

- Clearer turn indicators were added after feedback that active-player state was hard to read.
- Guide scene / onboarding improvements were added after feedback that the rules needed a beginner-friendly explanation.
- UI readability improvements were made through status panels, labels, warnings/toasts, and result feedback.
- Local/AI route was prioritised as the stable assessment path.
- Online mode was treated as an extension because broader validation is still required.

## GitHub Issues / Kanban evidence

GitHub Issues were used as task tracking and completion evidence. Closed issues represent completed development tasks, not missing process evidence. The Project/Kanban board supports planning, progress, and completion evidence.

- Kanban evidence summary: [KANBAN_AND_ISSUE_EVIDENCE.md](./KANBAN_AND_ISSUE_EVIDENCE.md)
- Closed issues: https://github.com/HPPPK/Game-Programming/issues?q=is%3Aissue%20is%3Aclosed
- Project board: https://github.com/users/HPPPK/projects/2
- Main milestone: https://github.com/HPPPK/Game-Programming/milestone/2

## Final state before submission

- Final assessed Unity project: `FinalGame/CanalTD`
- Primary assessment route: `BootstrapScene` -> `HomeScene` -> `ModeSelectScene` -> Local / AI mode -> `GameScene` -> `ResultScene`
- Online Mode: implemented extension requiring broader validation
- Runtime verification status: Unity Editor compilation and runtime testing must be manually verified

## Remaining limitations

- Online multiplayer edge cases require broader validation.
- Balance and tutorial pacing may need further polish after more playtesting.
- Manual test results in `FinalGame/TESTING_LOG.md` need to be completed in Unity before final submission.
- Unity Editor compilation and runtime testing must be manually verified and recorded before claiming runtime pass results.
