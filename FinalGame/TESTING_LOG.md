# CanalTD Testing Log

## Purpose

This document records testing coverage for the CanalTD playable vertical slice: scene flow, controls, rules, UI feedback, stability, known limitations, and changes made after testing or feedback. Unity runtime checks still require manual verification in the Unity Editor unless a result is explicitly filled in later.

## Primary assessment route

`BootstrapScene` -> `HomeScene` -> `ModeSelectScene` -> Local / AI mode -> `GameScene` -> `ResultScene`

Local Mode and AI Mode are the main assessment routes. Online Mode is treated as an implemented extension unless broader multiplayer validation is completed separately.

## Current verification status

- Unity batchmode compile check was attempted locally on 2026-06-08 using Unity `2022.3.62f3`.
- The batchmode check did not reach project compilation because Unity LicensingClient IPC timed out.
- This result is an environment/licensing blocker, not evidence that C# compilation passed or failed.
- Local and AI gameplay route checks were reported as manually verified by the student in Unity Editor on 2026-06-08.
- Online Mode remains documented as an implemented extension and is not claimed as fully stable multiplayer gameplay.

## Testing summary table

| ID | Area | Test case | Expected result | Manual result | Status | Evidence / related issue or file | Change made after testing |
|---|---|---|---|---|---|---|---|
| T01 | Unity setup | Unity project opens from `FinalGame/CanalTD` in Unity `2022.3.62f3`. | Project opens without missing project configuration. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. Codex batchmode attempt was separately blocked by Unity LicensingClient IPC timeout before project compilation. | Pass | `FinalGame/CanalTD`, `ProjectSettings/ProjectVersion.txt`, ignored local log `unity-batch-compile.log` | No project change required |
| T02 | Scene flow | `BootstrapScene` loads the normal scene flow. | Startup reaches the expected menu flow. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scenes/BootstrapScene.unity` | No change required |
| T03 | Home UI | `HomeScene` buttons navigate correctly. | Buttons route to mode selection, guide, settings, or exit behavior as configured. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scenes/HomeScene.unity`, issue [#61](https://github.com/HPPPK/Game-Programming/issues/61) | No change required |
| T04 | Local mode | `ModeSelectScene` enters Local Mode. | Local player setup loads the main game route. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scenes/ModeSelectScene.unity`, `Assets/Scripts/UI/ModeSelectSceneManager.cs` | No change required |
| T05 | AI mode | `ModeSelectScene` enters AI Mode. | AI setup loads the main game route with AI-controlled slots. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Core/AIPrototypeTurnManager.cs`, issue [#62](https://github.com/HPPPK/Game-Programming/issues/62) | No change required |
| T06 | Guide | `GuideScene` explains the basic rules and controls. | Player can read or step through onboarding guidance. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scenes/GuideScene.unity`, issues [#92](https://github.com/HPPPK/Game-Programming/issues/92)-[#97](https://github.com/HPPPK/Game-Programming/issues/97) | Changed after feedback: guide/onboarding route added |
| T07 | Cards | Player can draw a card once per turn. | Draw action updates hand state and respects turn limits. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Card/CardDrawManager.cs` | No change required |
| T08 | Turn/AP rules | AP / turn rules restrict invalid actions. | Invalid action is blocked or warned without corrupting turn state. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Core/TurnManager.cs` | No change required |
| T09 | Targeting | Card targeting confirm/cancel flow works. | Confirm applies selected target; cancel exits targeting cleanly. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Card`, issue [#95](https://github.com/HPPPK/Game-Programming/issues/95) | No change required |
| T10 | Gate/path | Gate/path interaction affects enemy pressure or path choice. | Gate/path changes affect reachable route selection. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Path`, `Assets/Scripts/Enemy/EnemyPathAssignmentManager.cs`, issue [#29](https://github.com/HPPPK/Game-Programming/issues/29) | No change required |
| T11 | Tower build | Tower build area opens build/purchase interaction. | Build interaction opens and allows valid tower choices. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Tower/BuildTowerManager.cs` | No change required |
| T12 | Land ownership | Owned land rules prevent invalid building. | Building is blocked on invalid/unowned land. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Tower` | No change required |
| T13 | Resources | Player resources decrease after land/tower purchases. | Gold/resource display and internal state update after purchase. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Core/PlayerResource.cs`, `Assets/Scripts/UI` | No change required |
| T14 | Tower actions | Upgrade/sell flow gives readable feedback. | Valid upgrade/sell actions update tower/resource state and feedback. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Tower/BuildTowerManager.cs` | No change required |
| T15 | Enemy waves | Enemy waves spawn and move along valid paths. | Enemies appear and follow reachable route data. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Enemy/WaveManager.cs`, issue [#97](https://github.com/HPPPK/Game-Programming/issues/97) | No change required |
| T16 | Tower combat | Towers attack enemies where appropriate. | Towers target enemies in range and apply damage. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Tower/CannonTower.cs`, `Assets/Scripts/Enemy/EnemyHealth.cs` | No change required |
| T17 | Result state | Castle/base/score/result state updates when enemies reach targets. | Castle health, score, and result state change as expected. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Core/CastleBase.cs`, `Assets/Scripts/UI/ResultSceneManager.cs` | No change required |
| T18 | Turn UI | Current player / turn indicator is readable. | Active player is visible during turn flow. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/UI/CurrentTurnIndicatorManager.cs`, issue [#90](https://github.com/HPPPK/Game-Programming/issues/90) | Changed after feedback: clearer turn indicator |
| T19 | Invalid feedback | Invalid action warning/toast appears. | Player receives readable feedback for blocked actions. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/UI/GlobalUIManager.cs`, `Assets/Scripts/UI/ModeSelectSceneManager.cs` | No change required |
| T20 | Results | `ResultScene` displays final ranking/result. | Match result appears with readable ranking/result state. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scenes/ResultScene.unity`, `Assets/Scripts/UI/ResultSceneManager.cs` | No change required |
| T21 | AI Easy | AI Easy turn logic check. | Easy AI takes simple valid turns. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Core/AIPrototypeTurnManager.cs` | No change required |
| T22 | AI Medium | AI Medium turn logic check. | Medium AI takes valid turns with stronger choices. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Core/AIPrototypeTurnManager.cs` | No change required |
| T23 | AI Hard | AI Hard turn logic check. | Hard AI takes valid turns with higher pressure. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | `Assets/Scripts/Core/AIPrototypeTurnManager.cs` | No change required |
| T24 | Online extension | Online room flow is treated as an extension if not fully validated. | Online is documented in the README and not required as the core marking route. | Passed as a scope/documentation check on 2026-06-08. Online remains an implemented extension and is not claimed as fully stable multiplayer gameplay. | Pass | `README.md`, issues [#62](https://github.com/HPPPK/Game-Programming/issues/62), [#89](https://github.com/HPPPK/Game-Programming/issues/89), [#91](https://github.com/HPPPK/Game-Programming/issues/91) | Local/AI route prioritised as stable assessment path |
| T25 | Console | Local/AI assessment route has no console-breaking errors after manual check. | No console-breaking errors appear during the checked route. | Passed in Unity Editor on 2026-06-08, based on student-reported manual verification. | Pass | Unity Editor console | No change required |

## Bugs and improvements

| Problem found | Evidence / issue | Change made | Why it improved playability | Current status |
|---|---|---|---|---|
| Online scene transition needed synchronization work. | [Online Match Start Does Not Synchronize Scene Transition #89](https://github.com/HPPPK/Game-Programming/issues/89) | Online mode documented as extension; related networking scripts retained. | Avoids making online mode the primary assessment route without broader validation. | Online remains extension; broader multiplayer validation still required |
| Active player / turn indication needed clearer feedback. | [Turn Marker Animation - User Indication #90](https://github.com/HPPPK/Game-Programming/issues/90) | Current-turn and marker UI documented as feedback improvements. | Helps players identify whose turn is active. | Student manual Local/AI verification recorded |
| Guide needed step-by-step onboarding. | [Step by step instruction guidance #92](https://github.com/HPPPK/Game-Programming/issues/92), tutorial issues [#95](https://github.com/HPPPK/Game-Programming/issues/95)-[#97](https://github.com/HPPPK/Game-Programming/issues/97) | Guide scene and tutorial flow documented as the onboarding path. | Makes controls and rules easier for first-time players. | Student manual verification recorded |
| Player information and score needed to be readable. | [Player Information, Color, HUD, and Score #93](https://github.com/HPPPK/Game-Programming/issues/93) | HUD/status systems documented as assessment-facing feedback. | Supports player goal clarity and result understanding. | Student manual Local/AI verification recorded |
| Audio feedback needed centralized integration. | [Audio System Integration and Sound Effects Implementation #87](https://github.com/HPPPK/Game-Programming/issues/87) | AudioManager and audio source references documented. | Supports interaction feedback and presentation polish. | Student manual Local/AI verification recorded |

## How to complete this log before final submission

1. Open `FinalGame/CanalTD` in Unity `2022.3.62f3`.
2. Run the Local Mode route:
   `BootstrapScene` -> `HomeScene` -> `ModeSelectScene` -> Local mode -> `GameScene` -> `ResultScene`.
3. Run the AI Mode route:
   `BootstrapScene` -> `HomeScene` -> `ModeSelectScene` -> AI mode -> `GameScene` -> `ResultScene`.
4. Fill `Manual result` with the real outcome observed in Unity.
5. Set each status to `Pass`, `Partial`, or `Fail` based only on the observed result.
6. Record any change made after testing in the final column.
7. Do not mark tests as passed without checking them in Unity.

## Remaining limitations

- Online multiplayer edge cases require broader validation.
- Balance and tutorial pacing may need further polish after more playtesting.
- Unity batchmode compilation was attempted but blocked by Unity licensing IPC before compilation.
- Local and AI route checks are recorded above based on student-reported Unity Editor manual verification.
