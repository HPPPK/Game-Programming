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
- Gameplay route tests still require manual Unity Editor verification and should not be marked `Pass` until actually checked.

## Testing summary table

| ID | Area | Test case | Expected result | Manual result | Status | Evidence / related issue or file | Change made after testing |
|---|---|---|---|---|---|---|---|
| T01 | Unity setup | Unity project opens from `FinalGame/CanalTD` in Unity `2022.3.62f3`. | Project opens without missing project configuration. | Batchmode attempt on 2026-06-08 was blocked by Unity LicensingClient IPC timeout before project compilation. Manual Editor verification still required. | Partial | `FinalGame/CanalTD`, `ProjectSettings/ProjectVersion.txt`, ignored local log `unity-batch-compile.log` | No project change made; complete this check manually in Unity Editor |
| T02 | Scene flow | `BootstrapScene` loads the normal scene flow. | Startup reaches the expected menu flow. | Manual verification required | Needs manual verification | `Assets/Scenes/BootstrapScene.unity` | To be completed after manual verification |
| T03 | Home UI | `HomeScene` buttons navigate correctly. | Buttons route to mode selection, guide, settings, or exit behavior as configured. | Manual verification required | Needs manual verification | `Assets/Scenes/HomeScene.unity`, issue [#61](https://github.com/HPPPK/Game-Programming/issues/61) | To be completed after manual verification |
| T04 | Local mode | `ModeSelectScene` enters Local Mode. | Local player setup loads the main game route. | Manual verification required | Needs manual verification | `Assets/Scenes/ModeSelectScene.unity`, `Assets/Scripts/UI/ModeSelectSceneManager.cs` | To be completed after manual verification |
| T05 | AI mode | `ModeSelectScene` enters AI Mode. | AI setup loads the main game route with AI-controlled slots. | Manual verification required | Needs manual verification | `Assets/Scripts/Core/AIPrototypeTurnManager.cs`, issue [#62](https://github.com/HPPPK/Game-Programming/issues/62) | To be completed after manual verification |
| T06 | Guide | `GuideScene` explains the basic rules and controls. | Player can read or step through onboarding guidance. | Manual verification required | Needs manual verification | `Assets/Scenes/GuideScene.unity`, issues [#92](https://github.com/HPPPK/Game-Programming/issues/92)-[#97](https://github.com/HPPPK/Game-Programming/issues/97) | Changed after feedback: guide/onboarding route added |
| T07 | Cards | Player can draw a card once per turn. | Draw action updates hand state and respects turn limits. | Manual verification required | Needs manual verification | `Assets/Scripts/Card/CardDrawManager.cs` | To be completed after manual verification |
| T08 | Turn/AP rules | AP / turn rules restrict invalid actions. | Invalid action is blocked or warned without corrupting turn state. | Manual verification required | Needs manual verification | `Assets/Scripts/Core/TurnManager.cs` | To be completed after manual verification |
| T09 | Targeting | Card targeting confirm/cancel flow works. | Confirm applies selected target; cancel exits targeting cleanly. | Manual verification required | Needs manual verification | `Assets/Scripts/Card`, issue [#95](https://github.com/HPPPK/Game-Programming/issues/95) | To be completed after manual verification |
| T10 | Gate/path | Gate/path interaction affects enemy pressure or path choice. | Gate/path changes affect reachable route selection. | Manual verification required | Needs manual verification | `Assets/Scripts/Path`, `Assets/Scripts/Enemy/EnemyPathAssignmentManager.cs`, issue [#29](https://github.com/HPPPK/Game-Programming/issues/29) | To be completed after manual verification |
| T11 | Tower build | Tower build area opens build/purchase interaction. | Build interaction opens and allows valid tower choices. | Manual verification required | Needs manual verification | `Assets/Scripts/Tower/BuildTowerManager.cs` | To be completed after manual verification |
| T12 | Land ownership | Owned land rules prevent invalid building. | Building is blocked on invalid/unowned land. | Manual verification required | Needs manual verification | `Assets/Scripts/Tower` | To be completed after manual verification |
| T13 | Resources | Player resources decrease after land/tower purchases. | Gold/resource display and internal state update after purchase. | Manual verification required | Needs manual verification | `Assets/Scripts/Core/PlayerResource.cs`, `Assets/Scripts/UI` | To be completed after manual verification |
| T14 | Tower actions | Upgrade/sell flow gives readable feedback. | Valid upgrade/sell actions update tower/resource state and feedback. | Manual verification required | Needs manual verification | `Assets/Scripts/Tower/BuildTowerManager.cs` | To be completed after manual verification |
| T15 | Enemy waves | Enemy waves spawn and move along valid paths. | Enemies appear and follow reachable route data. | Manual verification required | Needs manual verification | `Assets/Scripts/Enemy/WaveManager.cs`, issue [#97](https://github.com/HPPPK/Game-Programming/issues/97) | To be completed after manual verification |
| T16 | Tower combat | Towers attack enemies where appropriate. | Towers target enemies in range and apply damage. | Manual verification required | Needs manual verification | `Assets/Scripts/Tower/CannonTower.cs`, `Assets/Scripts/Enemy/EnemyHealth.cs` | To be completed after manual verification |
| T17 | Result state | Castle/base/score/result state updates when enemies reach targets. | Castle health, score, and result state change as expected. | Manual verification required | Needs manual verification | `Assets/Scripts/Core/CastleBase.cs`, `Assets/Scripts/UI/ResultSceneManager.cs` | To be completed after manual verification |
| T18 | Turn UI | Current player / turn indicator is readable. | Active player is visible during turn flow. | Manual verification required | Needs manual verification | `Assets/Scripts/UI/CurrentTurnIndicatorManager.cs`, issue [#90](https://github.com/HPPPK/Game-Programming/issues/90) | Changed after feedback: clearer turn indicator |
| T19 | Invalid feedback | Invalid action warning/toast appears. | Player receives readable feedback for blocked actions. | Manual verification required | Needs manual verification | `Assets/Scripts/UI/GlobalUIManager.cs`, `Assets/Scripts/UI/ModeSelectSceneManager.cs` | To be completed after manual verification |
| T20 | Results | `ResultScene` displays final ranking/result. | Match result appears with readable ranking/result state. | Manual verification required | Needs manual verification | `Assets/Scenes/ResultScene.unity`, `Assets/Scripts/UI/ResultSceneManager.cs` | To be completed after manual verification |
| T21 | AI Easy | AI Easy turn logic check. | Easy AI takes simple valid turns. | Manual verification required | Needs manual verification | `Assets/Scripts/Core/AIPrototypeTurnManager.cs` | To be completed after manual verification |
| T22 | AI Medium | AI Medium turn logic check. | Medium AI takes valid turns with stronger choices. | Manual verification required | Needs manual verification | `Assets/Scripts/Core/AIPrototypeTurnManager.cs` | To be completed after manual verification |
| T23 | AI Hard | AI Hard turn logic check. | Hard AI takes valid turns with higher pressure. | Manual verification required | Needs manual verification | `Assets/Scripts/Core/AIPrototypeTurnManager.cs` | To be completed after manual verification |
| T24 | Online extension | Online room flow is treated as an extension if not fully validated. | Online is documented separately and not required as the core marking route. | Manual verification required | Partial | `FinalGame/CanalTD/ONLINE_STATUS.md`, issues [#62](https://github.com/HPPPK/Game-Programming/issues/62), [#89](https://github.com/HPPPK/Game-Programming/issues/89), [#91](https://github.com/HPPPK/Game-Programming/issues/91) | Local/AI route prioritised as stable assessment path |
| T25 | Console | Local/AI assessment route has no console-breaking errors after manual check. | No console-breaking errors appear during the checked route. | Manual verification required | Needs manual verification | Unity Editor console | To be completed after manual verification |

## Bugs and improvements

| Problem found | Evidence / issue | Change made | Why it improved playability | Current status |
|---|---|---|---|---|
| Online scene transition needed synchronization work. | [Online Match Start Does Not Synchronize Scene Transition #89](https://github.com/HPPPK/Game-Programming/issues/89) | Online mode documented as extension; related networking scripts retained. | Avoids making online mode the primary assessment route without broader validation. | Manual verification required |
| Active player / turn indication needed clearer feedback. | [Turn Marker Animation - User Indication #90](https://github.com/HPPPK/Game-Programming/issues/90) | Current-turn and marker UI documented as feedback improvements. | Helps players identify whose turn is active. | Manual verification required |
| Guide needed step-by-step onboarding. | [Step by step instruction guidance #92](https://github.com/HPPPK/Game-Programming/issues/92), tutorial issues [#95](https://github.com/HPPPK/Game-Programming/issues/95)-[#97](https://github.com/HPPPK/Game-Programming/issues/97) | Guide scene and tutorial flow documented as the onboarding path. | Makes controls and rules easier for first-time players. | Manual verification required |
| Player information and score needed to be readable. | [Player Information, Color, HUD, and Score #93](https://github.com/HPPPK/Game-Programming/issues/93) | HUD/status systems documented as assessment-facing feedback. | Supports player goal clarity and result understanding. | Manual verification required |
| Audio feedback needed centralized integration. | [Audio System Integration and Sound Effects Implementation #87](https://github.com/HPPPK/Game-Programming/issues/87) | AudioManager and audio source references documented. | Supports interaction feedback and presentation polish. | Manual verification required |

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
- Tests marked `Manual verification required` must be completed in Unity before final submission.
- Unity batchmode compilation was attempted but blocked by Unity licensing IPC before compilation.
- Unity Editor compilation and runtime testing must be manually verified.
