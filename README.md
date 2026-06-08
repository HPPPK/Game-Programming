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
- Online room flow and core sync are implemented and under active testing.
- Tutorial / guide flow exists through the dedicated Guide scene.

## Vertical slice coverage

- Clear start through the Bootstrap, Home, and Mode Select scenes
- Clear player goal through castle defence, score pressure, and enemy routing
- Working interaction through card play, targeting, tower building, and turn progression
- Feedback through current-turn UI, status panels, targeting highlights, toasts, and result screens
- Challenge and decision-making through shared enemy pressure, path control, and tactical card use

## Planned features

- Finish and stabilise the remaining online card-effect sync edge cases.
- Continue balancing AI behaviour, card pacing, and multiplayer clarity.
- Improve final presentation polish, testing coverage, and supporting documentation.

## Known issues

- Some online multiplayer card interactions are still being regression-tested.
- Multiplayer room flow and late-match edge cases still need broader validation.
- Balance, tutorial pacing, and final polish are still being tuned.

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
  - `UI Button Click #5`
- Additional local audio files are credited as Pixabay-sourced where the platform is known but the exact original file page is no longer fully traceable from the preserved filename.

## Legal, ethical, accessibility, and process notes

- AI-assisted coding and writing support are disclosed below and in supporting documentation.
- The Guide scene, clearer turn indicators, and UI feedback improvements were added to improve accessibility and readability.
- Development process and iterative progress are evidenced through the GitHub repository, including project issues, kanban-style task tracking, documentation updates, and ongoing revision records.

## AI / tutorial / template use

- AI-assisted coding and debugging were used for selected implementation, refactoring, bug investigation, and documentation tasks.
- Main AI-assisted coding support:
  - `OpenAI GPT Codex 5.4`
  - `OpenAI GPT Codex 5.5`
  - Product page: https://openai.com/codex/
- Main AI-assisted art support:
  - GPT-generated card visuals and card-slot visuals for this project
- Coursework planning templates in [FinalGame/template](./FinalGame/template) were used as lightweight project-management support.

## Development log

- Main development log: [FinalGame/DEVELOPMENT_LOG.md](./FinalGame/DEVELOPMENT_LOG.md)
- Early prototype document: [FinalGame/prototype.md](./FinalGame/prototype.md)

## Contribution and feedback notes

- Detailed contribution statement: [FinalGame/CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md](./FinalGame/CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md)

## Documentation map

- Main Unity project folder: [FinalGame/CanalTD](./FinalGame/CanalTD)
- Project folder guide: [FinalGame/CanalTD/README.md](./FinalGame/CanalTD/README.md)
- Contribution notes: [FinalGame/CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md](./FinalGame/CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md)
- Development log: [FinalGame/DEVELOPMENT_LOG.md](./FinalGame/DEVELOPMENT_LOG.md)
- Prototype document: [FinalGame/prototype.md](./FinalGame/prototype.md)
- Resource / asset / AI references: [FinalGame/CanalTD/Assets/Docs/ResourceReferences.md](./FinalGame/CanalTD/Assets/Docs/ResourceReferences.md)

## Scope note

This README is the main entry point for the FinalGame coursework documentation.  
The unrelated folders below are not part of this final game but are related to the class activities documentation merge:

- `2D shooter`
- `Post`
- `Interactive_Solar_System_Assignment`
