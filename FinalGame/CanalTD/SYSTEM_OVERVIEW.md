# CanalTD Unity System Overview

This document helps markers locate the main Unity systems used by the CanalTD final vertical slice. It describes student-authored systems under `FinalGame/CanalTD/Assets/Scripts` and avoids third-party package code.

## Scene flow

- `BootstrapScene`: standard entry scene and shared startup managers.
- `HomeScene`: main menu / first navigation point.
- `ModeSelectScene`: Local, AI, Guide, and Online mode selection.
- `GuideScene`: onboarding and tutorial route.
- `GameScene`: main playable scene for Local and AI assessment routes.
- `ResultScene`: end-of-match ranking/result display.

Primary assessment route:

`BootstrapScene` -> `HomeScene` -> `ModeSelectScene` -> Local / AI mode -> `GameScene` -> `ResultScene`

## Main systems

| System | Main scripts | Scene / GameObject role | Inputs | Outputs / effects | Assessment notes |
|---|---|---|---|---|---|
| Turn / AP management | `Core/TurnManager.cs`, `Core/GamePhaseManager.cs`, `Core/ITurnSource.cs`, `Core/TurnSourceResolver.cs` | Configured in the Unity scene / Inspector. | Player actions, AP costs, turn-end actions, online authority state where relevant. | Turn changes, AP state, draw/play/discard/gate restrictions, UI update triggers. | Core rule structure for the playable vertical slice. |
| Player resources | `Core/PlayerResource.cs`, `Core/PlayerManager.cs`, `Core/PlayerType.cs`, `Core/PlayerHand.cs` | Configured in the Unity scene / Inspector. | Purchases, player setup, card-hand state, score/health/resource changes. | Resource updates, player state, hand state, score/identity data. | Supports player goal, costs, ownership, and feedback. |
| Card draw / play / targeting | `Card/CardDrawManager.cs`, `Card/CardInstanceSelectable.cs`, `Card/CardHoverUI.cs`, `Card/TileTargetingManager.cs`, `Card/GateTargetingManager.cs`, `Card/TowerTargetingManager.cs`, `Card/PlayerTargetingManager.cs`, `Card/ShockTrapTargetingManager.cs` | Card UI and targeting systems configured in scenes. | Card selections, target selections, confirm/cancel buttons, turn/AP permissions. | Hand updates, card effects, targeting highlights, warnings, card-driven map/player/tower effects. | Key interaction system for meaningful player decisions. |
| Gate / path / canal control | `Map/GateActionType.cs`, `Map/doorChange.cs`, `Map/GateFrameAnimation.cs`, `Map/WaypointPath.cs`, `Path/PathNode.cs`, `Path/PathEdge.cs`, `Path/PathGraphState.cs`, `Path/Pathfinder.cs`, `Path/GatePathBlocker.cs`, `Path/CastleEndNode.cs` | Path/gate objects configured in the Unity scene / Inspector. | Gate/card actions, path graph state, blocked routes, enemy destination needs. | Path availability changes, route recalculation support, visual gate feedback. | Supports the unique canal-pressure mechanic. |
| Enemy spawning / routing | `Enemy/WaveManager.cs`, `Enemy/EnemySpawner.cs`, `Enemy/EnemyPathAssignmentManager.cs`, `Enemy/EnemyMover.cs`, `Enemy/EnemyHealth.cs`, `Enemy/WaveEnemyEntry.cs`, `Enemy/EnemyStats.cs`, `Enemy/EnemyType.cs`, `Enemy/EnemyTargetSelector.cs`, `Enemy/EnemySlowEffect.cs` | Enemy wave and prefab systems configured in scenes. | Wave data, path graph data, enemy stats, gate/path state, tower damage. | Enemy spawning, movement, health changes, route destination assignment, castle pressure. | Main challenge loop and strategic pressure system. |
| Tower build / upgrade / sell | `Tower/BuildTowerManager.cs`, `Tower/TowerBuildArea.cs`, `Tower/CannonTower.cs`, `Tower/TowerProjectile.cs`, `Tower/TowerStats.cs`, `Tower/TowerType.cs`, `Tower/TargetPriority.cs` | Build areas and tower objects configured in the Unity scene / Inspector. | Player clicks, build area state, resources, ownership, upgrade/sell commands. | Tower placement, upgrades, selling, resource changes, projectile/damage effects. | Shows resource management and defense interaction. |
| UI feedback / status panels | `UI/GlobalUIManager.cs`, `UI/CurrentTurnIndicatorManager.cs`, `UI/PlayerStatusPanelUI.cs`, `UI/APDisplayUI.cs`, `UI/RoomSlotUI.cs`, `UI/ModeSelectSceneManager.cs`, `UI/HomeSceneManager.cs`, `UI/SettingsPanelController.cs`, `UI/ManualUIButton.cs`, `UI/UIButtonSoundBinder.cs`, `UI/GlobalCursorManager.cs`, `UI/CursorVisualFollower.cs` | UI canvases and scene managers configured in scenes. | Button clicks, gameplay state, turn state, player data, invalid actions. | Labels, status panels, warnings/toasts, cursor feedback, navigation. | Supports readability, controls, and assessment clarity. |
| AI mode | `Core/AIPrototypeTurnManager.cs`, `Core/AIDifficulty.cs` | Configured in the Unity scene / Inspector. | Difficulty choice, game state, available cards/actions/resources. | AI turn choices and automated interaction. | Makes the primary assessment route usable without several human players. |
| Online mode extension | `Networking/PhotonPunRoomLobbyManager.cs`, `Networking/PhotonOnlineGameSceneManager.cs`, `Networking/PhotonOnlineCardSyncManager.cs`, `Networking/PhotonOnlineBuildSyncManager.cs`, `Networking/OnlineTurnPermissionManager.cs`, `Networking/OnlineCardSyncModels.cs`, `Networking/OnlineBuildSyncData.cs`, `Networking/PhotonLobbyPropertyKeys.cs` | Photon room/lobby and online scene managers configured in scenes. | Photon room/player state, ready state, network events, online turn authority. | Lobby flow, synchronized scene-loading attempts, online turn/card/build sync payloads. | Implemented extension; broader validation required before treating it as the stable route. |
| Tutorial / Guide flow | `Tutorial/TutorialManager.cs`, `Tutorial/TutorialData.cs`, `Tutorial/TutorialActionGate.cs`, `Tutorial/TutorialMessageController.cs`, `Tutorial/TutorialHighlightController.cs`, `Tutorial/TutorialEnemyDemoSpawner.cs` | Guide scene tutorial objects configured in the Unity scene / Inspector. | Tutorial steps, player actions, skip/continue inputs, demonstration triggers. | Step messages, highlights, restricted actions, tutorial enemy demonstrations. | Supports onboarding and accessibility. |
| Audio feedback | `UI/AudioManager.cs`, `Core/UIButtonSound.cs`, `UI/UIButtonSoundBinder.cs` | Shared audio manager and UI button sound binding configured in scenes. | Button clicks, card/tower/enemy/result events, settings values. | Music and SFX playback, volume/pitch feedback. | Improves interaction feedback and presentation polish. |
| Result / ranking screen | `UI/ResultSceneManager.cs`, `UI/ResultRowUI.cs`, `Core/GameResultData.cs` | Result scene UI configured in the Unity scene / Inspector. | Final player state, score, castle/result data. | Ranking/result rows and end-of-match feedback. | Supports completion condition and outcome clarity. |

## Manual verification note

Unity Editor compilation and runtime testing must be manually verified. This overview maps systems to files and scenes; it does not claim every runtime path has passed testing.

## Support and debug utility note

Some UI helper scripts are retained for development support, such as manual button or raycast probing utilities. They are not listed as main gameplay systems and should not be treated as final feature claims.
