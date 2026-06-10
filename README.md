# CanalTD

## Short description

CanalTD is a turn-based multiplayer strategy and tower-defense game where players control canal gates, use cards, build towers, and redirect enemy pressure toward opponents instead of attacking directly.

## One-sentence game idea

Players survive enemy waves by combining map control, card interaction, canal-flow control, and tower placement to manipulate where enemies travel.

## Player goal

Protect your own castle, manage enemy pressure better than the other players, and finish the match with the strongest score and survival outcome.

## Assessment readiness

- The game can be run from a clear Unity scene flow.
- The player goal is visible and understandable.
- The controls and turn structure are readable.
- The project includes a guide scene, turn indicators, feedback UI, and status panels.
- Documentation, contribution notes, and development history are linked from this README.

## Controls

- Left click: select cards, towers, UI buttons, and map targets.
- Card buttons: draw, discard, and play cards during the active player turn.
- Targeting confirm / cancel buttons: confirm or cancel card targeting actions.
- Tower build interactions: click a build area and choose a tower from the radial menu.
- Menu buttons: choose Local, AI, Guide, or Online mode from the front-end scenes.

## How to run the project

1. Open Unity Hub.
2. Add and open the project folder: `FinalGame/CanalTD`.
3. Load `Assets/Scenes/BootstrapScene.unity` for the normal startup flow.
4. Press **Play** in the Unity Editor.
5. Use the Home and Mode Select scenes to enter Local, AI, Guide, or Online play.

## Build / release

No downloadable release is currently provided. The assessed project should be opened in Unity `2022.3.62f3` from `FinalGame/CanalTD`.

## Main scenes

- `BootstrapScene`: standard project entry scene
- `HomeScene`: initial menu flow
- `ModeSelectScene`: mode selection and online lobby access
- `GuideScene`: onboarding and beginner guidance
- `GameScene`: main playable game scene
- `ResultScene`: end-of-match ranking and result display

## Unity version

- `2022.3.62f3`

## Current status

- Local mode is playable.
- AI mode includes three difficulty levels and playable turn logic.
- Core card, tower, enemy, path, and result systems are integrated.
- Online multiplayer uses Photon room-code inviting for 2-4 players. Players create or join a private room using a room code, ready up, and play with synchronized turns, building, cards, gates, waves, combat, and results.
- Tutorial / guide flow exists through the dedicated Guide scene.

## Final assessment route

For marking, the project can be assessed through Local, AI, or Online mode:

`BootstrapScene` -> `HomeScene` -> `ModeSelectScene` -> Local / AI / Online mode -> `GameScene` -> `ResultScene`

Online mode is an implemented Photon multiplayer route. Local and AI modes remain available as offline assessment routes, while Online mode can be demonstrated when Photon connectivity and compatible clients are available.

## Final multiplayer demo checklist

Use this as the live presentation flow if Online mode is shown:

1. Start from `BootstrapScene` and open the Guide Tutorial.
2. Run an AI match to show the main playable route.
3. Open Online mode from `ModeSelectScene`.
4. Host creates a private room and shares the room code.
5. 2-4 players join, set names, ready up, and host starts the match.
6. Demonstrate build synchronization through buy land, build tower, upgrade, and sell.
7. Demonstrate card synchronization with one player-interaction card and one map-control card.
8. Demonstrate gate synchronization with Lock Gate or Open Gate.
9. End all player turns and show the synchronized enemy wave.
10. Continue to victory/result flow where time allows.

## Vertical slice coverage

- Clear start through the Bootstrap, Home, and Mode Select scenes
- Clear player goal through castle defence, score pressure, and enemy routing
- Working interaction through card play, targeting, tower building, and turn progression
- Feedback through current-turn UI, status panels, targeting highlights, toasts, and result screens
- Challenge and decision-making through shared enemy pressure, path control, and tactical card use

## Future work

- Add automated regression coverage for multiplayer synchronization.
- Continue balancing AI behaviour, card pacing, and multiplayer clarity.
- Add more regression coverage and playtesting evidence.

## Known limitations

- Online mode is room-code invitation multiplayer only; random player pairing is not part of the final design.
- There is no reconnect, late-join recovery, spectator mode, ranked mode, database persistence, or dedicated server.
- Online room joining requires clients to use the same Photon AppId, fixed region, game version, and compatible build.
- Balance and tutorial pacing can be improved with more playtesting.

## Development tracking / Kanban evidence

I used GitHub Issues as the main task-tracking method for CanalTD development. Issues were used to break down design, implementation, tutorial, UI, audio, online-mode, and bug-fix work into reviewable tasks.

Most implementation tasks are now closed because the related work was completed before final submission. The GitHub tab may show `Issues 0`; this means there are currently zero open issues, not that issues were unused. Closed issues are positive process evidence because they show completed planning, progress, iteration, and bug-fix records.

The GitHub Project/Kanban board was used to move tasks through planning, progress, and completion states. The main milestone is named `Initial game design`, but it contains both design and implementation tasks; this README and the linked final documentation are the authoritative final submission entry points.

- GitHub Project board: https://github.com/users/HPPPK/projects/2
- Closed development issues: https://github.com/HPPPK/Game-Programming/issues?q=is%3Aissue%20is%3Aclosed
- Open remaining issues / limitations: https://github.com/HPPPK/Game-Programming/issues?q=is%3Aissue%20is%3Aopen
- Main milestone: https://github.com/HPPPK/Game-Programming/milestone/2

Known limitations are documented as future work above. If any limitation needs active tracking after submission, it should be represented by a separate open issue instead of reopening completed development tasks.

## Main Unity systems

| System | Main scripts / scenes | Assessment relevance |
|---|---|---|
| Turn / AP management | `Assets/Scripts/Core/TurnManager.cs`, `GamePhaseManager.cs` | Core rule structure, turn limits, AP costs, and player action flow. |
| Cards and targeting | `Assets/Scripts/Card`, `GameScene` | Draw, play, discard, targeting confirm/cancel, and tactical interaction. |
| Towers and resources | `Assets/Scripts/Tower`, `PlayerResource.cs` | Land ownership, build, upgrade, sell, tower combat, and resource decisions. |
| Enemy routing / canal pressure | `Assets/Scripts/Enemy`, `Assets/Scripts/Path`, `Assets/Scripts/Map` | Main strategic pressure mechanic and enemy-wave challenge. |
| UI, guide, audio, result | `Assets/Scripts/UI`, `Assets/Scripts/Tutorial`, `ResultScene` | Player feedback, onboarding, status clarity, audio feedback, and completion state. |
| AI / Online modes | `AIPrototypeTurnManager.cs`, `Assets/Scripts/Networking` | AI supports offline assessment; Online supports Photon room-code multiplayer. |

## Credits

- Game design, Unity implementation, and project integration: Jingyu Pan.
- Multiplayer networking package: Photon PUN 2.
- Text rendering and UI support: TextMeshPro.
- Main environment art / map / enemy / tower asset pack: `Tiny Swords`
  - https://pixelfrog-assets.itch.io/tiny-swords
- Card visuals and card-slot visuals:
  - Generated with GPT for this project

## Resource references

- Full asset, audio, AI, and reference list:
  - [FinalGame/CanalTD/Assets/Docs/ResourceReferences.md](./FinalGame/CanalTD/Assets/Docs/ResourceReferences.md)
- Main external resource platforms:
  - Tiny Swords asset pack: https://pixelfrog-assets.itch.io/tiny-swords
  - Pixabay sound effects: https://pixabay.com/zh/music/search/sound%20effects/
  - Pixabay game music: https://pixabay.com/zh/music/search/game%20music/
- Confirmed audio items currently identified:
  - `Roblox Minecraft Fortnite Video Game Music`
  - `Click Buttons - UI Menu Sounds Effects - Button 7`
  - `Taking Playing Card 3`
  - `Shoot 5`
  - `Dramatic Death Collapse`
  - `Gaming Victory`
  - `Musical Victory Chime`
- Full local audio-to-source mapping is listed in the resource reference document.

## Legal, ethical, accessibility, and process notes

- AI-assisted coding and writing support are disclosed below and in supporting documentation.
- The Guide scene, clearer turn indicators, and UI feedback improvements were added to improve accessibility and readability.
- Development process and iterative progress are evidenced through the GitHub repository, including project issues, kanban-style task tracking, documentation updates, and ongoing revision records.
- Local and AI gameplay route verification is recorded in the testing log based on student manual Unity Editor checks.

## AI / tutorial / template use

- AI-assisted coding and debugging were used for selected implementation, refactoring, bug investigation, and documentation tasks.
- AI-assisted coding support was used for selected coding suggestions, debugging suggestions, refactoring suggestions, and documentation drafting.
- Final design choices, Unity integration, testing, and acceptance/rejection of AI suggestions remained my responsibility.
- Main AI-assisted art support:
  - GPT-generated card visuals and card-slot visuals for this project
- Local coursework planning templates were used as private planning support and are intentionally not committed because they are excluded by `.gitignore`.
- The template folder is not required for running or assessing the final game.
- Submitted assessment evidence is provided through the README, final design document, testing log, development log, GitHub Project/Issues links, contribution notes, and resource references.

## Development log

- Main development log: [FinalGame/DEVELOPMENT_LOG.md](./FinalGame/DEVELOPMENT_LOG.md)
- Final game design: [FinalGame/FINAL_GAME_DESIGN.md](./FinalGame/FINAL_GAME_DESIGN.md)
- Testing log: [FinalGame/TESTING_LOG.md](./FinalGame/TESTING_LOG.md)
- Early prototype document: [FinalGame/prototype.md](./FinalGame/prototype.md)

## Contribution and feedback notes

- Detailed contribution statement: [FinalGame/CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md](./FinalGame/CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md)

## Documentation map

- Main Unity project folder: [FinalGame/CanalTD](./FinalGame/CanalTD)
- Project folder guide: [FinalGame/CanalTD/README.md](./FinalGame/CanalTD/README.md)
- Final game design: [FinalGame/FINAL_GAME_DESIGN.md](./FinalGame/FINAL_GAME_DESIGN.md)
- Development log: [FinalGame/DEVELOPMENT_LOG.md](./FinalGame/DEVELOPMENT_LOG.md)
- Testing log: [FinalGame/TESTING_LOG.md](./FinalGame/TESTING_LOG.md)
- Contribution notes: [FinalGame/CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md](./FinalGame/CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md)
- Resource / asset / AI references: [FinalGame/CanalTD/Assets/Docs/ResourceReferences.md](./FinalGame/CanalTD/Assets/Docs/ResourceReferences.md)
- GitHub Project board: https://github.com/users/HPPPK/projects/2
- Closed development issues: https://github.com/HPPPK/Game-Programming/issues?q=is%3Aissue%20is%3Aclosed
- Main milestone: https://github.com/HPPPK/Game-Programming/milestone/2

## Backup, draft, and support files

- `FinalGame/CanalTD/Assets/Scenes/GameScene.unity.bak_before_layout_edit` is a scene-layout backup, not the final assessment scene.
- `FinalGame/CanalTD/Assets/Docs/v3.docx` is a draft/report-like support file. The final assessment documentation is the Markdown documentation linked above.
- UI debug/probe helper scripts may remain in `Assets/Scripts/UI` as development support, but they are not listed as main gameplay systems.

## Scope note

This README is the main entry point for the FinalGame coursework documentation.  
The assessed final Unity project is `FinalGame/CanalTD`.
The unrelated folders below are not part of this final game but are related to the class activities documentation merge:

- `2D shooter`
- `Post`
- `Interactive_Solar_System_Assignment`
