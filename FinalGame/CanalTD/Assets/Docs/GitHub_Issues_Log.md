# CanalTD Coursework Issue Log (Scoring-Oriented)

> Note: I could not find a local folder/path named `FinalGame/template` in this workspace.  
> This document follows the structure you asked for (`Goal / Tasks / Design Decisions / Outcome / Status`) and is grounded in current project files.

---

## Issue 1 — Initial Game Concept, Rule Definition, and Resource Research

### Goal
Define the game concept, core loop, rule boundaries, and asset direction before heavy implementation.

### Tasks
- Define the game as a 2D turn-based strategy tower defense prototype.
- Establish player goals:
  - Protect own castle.
  - Redirect monster flow toward opponents.
  - Gain advantage using cards and gate control.
- Define core systems:
  - Canal / water path map.
  - Gate-based direction control.
  - Castle HP base system.
  - Card draw / select / play / discard flow.
- Define vertical-slice scope first (not full balance).
- Define early card set ideas (Redirect/Lock/Steal/Boost/Trap etc.).
- Research and select visual assets for prototype:
  - Use Tiny Swords as the main style reference.
  - Keep assets compatible with Unity 2D Sprite + Tilemap workflow.
  - Keep asset workload realistic for coursework prototype.

### Resource Research
- Selected Tiny Swords packs as primary style source:
  - `Tiny Swords (Free Pack)`
  - `Tiny Swords (Update 010)`
- Imported and organized resources under project art folders.
- Recorded resource usage at:
  - `CanalTD/Assets/Docs/ResourceReferences.md`

### Design Decisions
- Cards are the core strategic action layer.
- Gates are the core map interaction mechanic.
- Castle HP is the base loss condition anchor.
- First loop priority: Draw card -> Select card -> Play card -> Target gate -> Resolve.

### Outcome
Core design direction was fixed before system expansion, and asset sourcing decisions were documented for coursework traceability.

### Status
Done

---

## Issue 2 — Map Structure and Layered Tilemap Setup

### Goal
Build the playable board foundation with layered Tilemaps and clear spatial structure.

### Tasks
- Create map grid with layered tilemaps:
  - Water / Ground / Path / Shadow / Decoration
- Maintain bottom area as UI reserve zone.
- Keep map readable and symmetric for fair prototype testing.

### Design Decisions
- Tilemaps used for static board rendering.
- Interactive systems (gate/card/enemy) separated as GameObjects.

### Evidence
- Scene: `CanalTD/Assets/Scenes/GameScene.unity`
- Map object names found in scene:
  - `MapGrid`, `Water`, `Ground`, `Path`, `Shadow`

### Outcome
A stable map foundation exists for subsequent gate routing, enemy movement, and card-target interactions.

### Status
Done

---

## Issue 3 — Gate Objects and Click-Based Gate State Toggle

### Goal
Create interactable gate objects (not tilemap-only visuals) and enable open/close state switching.

### Tasks
- Build independent gate GameObjects under `Gates` root.
- Add clickable gate behavior using collider hit detection.
- Add gate visual state switching with frame animation.

### Design Decisions
- Gate remains object-based to support runtime targeting and effects.
- Gate change is currently coupled to card-triggered targeting mode.

### Evidence
- `CanalTD/Assets/Scripts/Map/GateFrameAnimation.cs`
- `CanalTD/Assets/Scripts/Map/doorChange.cs`
- `CanalTD/Assets/Scenes/GameScene.unity` includes `Gates`

### Outcome
Gate interaction loop is functional (click -> change state), enabling first strategic redirection interaction.

### Status
Done

---

## Issue 4 — Waypoint Path Foundation (No Complex Pathfinding)

### Goal
Implement simple, controllable waypoint routes for enemy movement.

### Tasks
- Create waypoint path data container.
- Support path IDs and ordered waypoint lists.
- Draw gizmo lines for editor readability.

### Design Decisions
- Use explicit waypoints instead of A* at this stage.
- Keep branch extension hooks for future gate-based routing.

### Evidence
- `CanalTD/Assets/Scripts/Map/WaypointPath.cs`

### Outcome
Enemy routes can be authored and tested quickly in Unity without heavy pathfinding architecture.

### Status
Done

---

## Issue 5 — Enemy Movement and Spawner Prototype

### Goal
Run a playable enemy loop: spawn -> move along waypoints -> reach path end.

### Tasks
- Implement enemy movement by waypoint stepping.
- Implement spawner with interval and count.
- Support four basic enemy type configs:
  - NormalEnemy / FastEnemy / TankEnemy / BossEnemy

### Design Decisions
- Enemy stats passed at spawn time.
- First stage focuses on motion and timing, not full combat yet.

### Evidence
- `CanalTD/Assets/Scripts/Enemy/EnemyMover.cs`
- `CanalTD/Assets/Scripts/Enemy/EnemySpawner.cs`

### Outcome
Vertical-slice movement core is in place and can be extended to base damage, score, and wave logic.

### Status
Done

---

## Issue 6 — Card UI Foundations (Deck / Draw / Select / Discard / Play)

### Goal
Build the card-hand interaction flow in UI.

### Tasks
- Prepare card prefabs and hand slots.
- Implement deck build and shuffle.
- Implement Draw button behavior.
- Implement single-card selection/deselection.
- Implement Discard and Play actions.
- Add warning text feedback for invalid actions.

### Design Decisions
- Deck managed as prefab references and runtime instances.
- Selection is instance-level with clear visual offset.

### Evidence
- Prefabs:
  - `CanalTD/Assets/Prefabs/Card/*.prefab`
- Scripts:
  - `CanalTD/Assets/Scripts/Card/CardDrawManager.cs`
  - `CanalTD/Assets/Scripts/Card/CardInstanceSelectable.cs`
  - `CanalTD/Assets/Scripts/Card/CardHoverUI.cs`
- Scene UI hooks (buttons):
  - `DrawCardButton`, `DiscardButton`, `PlayCardButton` in `GameScene.unity`

### Outcome
Card interaction baseline is playable and supports future concrete card effect logic.

### Status
Done

---

## Issue 7 — Redirect Gate Card Targeting Mode + Visual Feedback

### Goal
Create the first strategic card interaction: entering gate-target mode and applying gate action.

### Tasks
- Enter targeting mode from card/tool flow.
- Dim non-target layers and highlight gates.
- Exit targeting mode after gate interaction resolves.

### Design Decisions
- Use map/sprite color dimming instead of fullscreen overlay for clearer readability.

### Evidence
- `CanalTD/Assets/Scripts/Card/GateTargetingManager.cs`
- `CanalTD/Assets/Scripts/Core/CursorToolManager.cs`
- `CanalTD/Assets/Scripts/Map/GateFrameAnimation.cs`

### Outcome
First complete interaction chain exists: play action -> targeting mode -> click gate -> resolve -> exit mode.

### Status
Done

---

## Issue 8 — Castle HP Visualization

### Goal
Provide visible base-health feedback for players.

### Tasks
- Add castle health data component.
- Add health bar fill scaling.
- Add hover HP text display.

### Design Decisions
- Keep damage trigger testable via key for rapid iteration.
- UI updates every frame for immediate debugging clarity.

### Evidence
- `CanalTD/Assets/Scripts/Core/CastleBase.cs`
- `CanalTD/Assets/Scripts/UI/CastleHealthBar.cs`

### Outcome
Castle state is now visible and testable, supporting later score/lose-state integration.

### Status
Done

---

## Issue 9 — Rule-Constraint Integration (AP / Turn / Land / Tower)

### Goal
Implement the remaining systems required by v3 ruleset and coursework vertical slice grading.

### Tasks
- Add AP system (2 AP per turn).
- Add per-turn limits:
  - max 1 card use
  - max 1 gate change
- Add player turn order and enemy phase transition.
- Add land ownership and tower slot permission logic.
- Add score/gold progression hooks from enemy results.

### Evidence (Current Gap)
- No dedicated scripts yet for Turn/AP/Land/Tower managers under `CanalTD/Assets/Scripts/`.

### Outcome
This issue defines the next grading-critical implementation block.

### Status
In Progress

---

## Issue 10 — Coursework Vertical Slice Milestone

### Goal
Deliver a small but complete playable loop for assessment.

### Scope Target
- 2-4 placeholder players
- 2 enemy entrances
- 4 core gates
- waypoint enemy routes
- card draw/select/play
- Redirect Gate interaction
- castle HP feedback

### Acceptance Criteria
- Game can run from one scene without manual scene edits during play.
- Core interaction loop is demonstrable in a short video/live demo.
- All systems above are traceable to issues, scripts, and scene objects.

### Outcome
Milestone definition ready; implementation already partially achieved.

### Status
In Progress

---

# Suggested GitHub Labels
- `scope:design`
- `scope:map`
- `scope:gate`
- `scope:enemy`
- `scope:card`
- `scope:ui`
- `scope:core-rules`
- `status:done`
- `status:in-progress`
- `coursework-evidence`

# Suggested Milestones
- `M1 Concept + Resource Research`
- `M2 Map + Gate + Enemy Loop`
- `M3 Card Interaction + HP Feedback`
- `M4 Rule Constraints + Vertical Slice`
