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
- Online room flow and core sync are implemented as an experimental extension that still needs broader multiplayer validation.
- Tutorial / guide flow exists through the dedicated Guide scene.

## Final assessment route

For marking, the primary assessment route is Local mode or AI mode:

`BootstrapScene` -> `HomeScene` -> `ModeSelectScene` -> Local / AI mode -> `GameScene` -> `ResultScene`

Online mode is included as an implemented extension, but it is not the primary stable assessment path for final marking.

## Vertical slice coverage

- Clear start through the Bootstrap, Home, and Mode Select scenes
- Clear player goal through castle defence, score pressure, and enemy routing
- Working interaction through card play, targeting, tower building, and turn progression
- Feedback through current-turn UI, status panels, targeting highlights, toasts, and result screens
- Challenge and decision-making through shared enemy pressure, path control, and tactical card use

## Future work

- Broaden validation for online card-effect sync edge cases.
- Continue balancing AI behaviour, card pacing, and multiplayer clarity.
- Add more regression coverage and playtesting evidence.

## Known limitations

- Online mode is an extension and still needs broader multi-device and late-match validation.
- Balance and tutorial pacing can be improved with more playtesting.

## Development tracking / Kanban evidence

I used GitHub Issues as the main task-tracking method for CanalTD development. Issues were used to break down design, implementation, tutorial, UI, audio, online-mode, and bug-fix work into reviewable tasks.

Most implementation tasks are now closed because the related work was completed before final submission. The GitHub tab may show `Issues 0`; this means there are currently zero open issues, not that issues were unused. Closed issues are positive process evidence because they show completed planning, progress, iteration, and bug-fix records.

The GitHub Project/Kanban board was used to move tasks through planning, progress, and completion states. The main milestone is named `Initial game design`, but it contains both design and implementation tasks; this README and the linked final documentation are the authoritative final submission entry points.

- GitHub Project board: https://github.com/users/HPPPK/projects/2
- Closed development issues: https://github.com/HPPPK/Game-Programming/issues?q=is%3Aissue%20is%3Aclosed
- Open remaining issues / limitations: https://github.com/HPPPK/Game-Programming/issues?q=is%3Aissue%20is%3Aopen
- Main milestone: https://github.com/HPPPK/Game-Programming/milestone/2
- Kanban and issue evidence summary: [FinalGame/KANBAN_AND_ISSUE_EVIDENCE.md](./FinalGame/KANBAN_AND_ISSUE_EVIDENCE.md)

Known limitations are documented as future work above. If any limitation needs active tracking after submission, it should be represented by a separate open issue instead of reopening completed development tasks.

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
- Unity Editor compilation and runtime testing must be manually verified through the testing log before final submission.

## AI / tutorial / template use

- AI-assisted coding and debugging were used for selected implementation, refactoring, bug investigation, and documentation tasks.
- AI-assisted coding support was used for selected coding suggestions, debugging suggestions, refactoring suggestions, and documentation drafting.
- Final design choices, Unity integration, testing, and acceptance/rejection of AI suggestions remained my responsibility.
- Main AI-assisted art support:
  - GPT-generated card visuals and card-slot visuals for this project
- Local coursework planning templates were used as private planning support and are intentionally not committed because they are excluded by `.gitignore`.
- The template folder is not required for running or assessing the final game.
- Submitted assessment evidence is provided through the README, final design document, testing log, development log, Kanban evidence, contribution notes, and resource references.

## Development log

- Main development log: [FinalGame/DEVELOPMENT_LOG.md](./FinalGame/DEVELOPMENT_LOG.md)
- Final game design: [FinalGame/FINAL_GAME_DESIGN.md](./FinalGame/FINAL_GAME_DESIGN.md)
- Testing log: [FinalGame/TESTING_LOG.md](./FinalGame/TESTING_LOG.md)
- Submission checklist: [FinalGame/SUBMISSION_CHECKLIST.md](./FinalGame/SUBMISSION_CHECKLIST.md)
- Early prototype document: [FinalGame/prototype.md](./FinalGame/prototype.md)

## Contribution and feedback notes

- Detailed contribution statement: [FinalGame/CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md](./FinalGame/CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md)

## Documentation map

- Main Unity project folder: [FinalGame/CanalTD](./FinalGame/CanalTD)
- Project folder guide: [FinalGame/CanalTD/README.md](./FinalGame/CanalTD/README.md)
- Contribution notes: [FinalGame/CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md](./FinalGame/CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md)
- Final game design: [FinalGame/FINAL_GAME_DESIGN.md](./FinalGame/FINAL_GAME_DESIGN.md)
- Testing log: [FinalGame/TESTING_LOG.md](./FinalGame/TESTING_LOG.md)
- Development log: [FinalGame/DEVELOPMENT_LOG.md](./FinalGame/DEVELOPMENT_LOG.md)
- Submission checklist: [FinalGame/SUBMISSION_CHECKLIST.md](./FinalGame/SUBMISSION_CHECKLIST.md)
- Unity system overview: [FinalGame/CanalTD/SYSTEM_OVERVIEW.md](./FinalGame/CanalTD/SYSTEM_OVERVIEW.md)
- Online mode status: [FinalGame/CanalTD/ONLINE_STATUS.md](./FinalGame/CanalTD/ONLINE_STATUS.md)
- Kanban and issue evidence: [FinalGame/KANBAN_AND_ISSUE_EVIDENCE.md](./FinalGame/KANBAN_AND_ISSUE_EVIDENCE.md)
- Prototype document: [FinalGame/prototype.md](./FinalGame/prototype.md)
- Resource / asset / AI references: [FinalGame/CanalTD/Assets/Docs/ResourceReferences.md](./FinalGame/CanalTD/Assets/Docs/ResourceReferences.md)

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
