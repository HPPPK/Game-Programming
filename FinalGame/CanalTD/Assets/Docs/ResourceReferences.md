# Resource References

This file records the main external resources, AI-assisted support, and reference sources used during development of CanalTD.

## Game assets

- Main asset pack / visual pack:
  - Name: `Tiny Swords`
  - Link: `https://pixelfrog-assets.itch.io/tiny-swords`
- Additional art or icon source:
  - Name: `Card art and card slot visuals generated with GPT`
  - Link: `No separate public asset page. These visuals were generated specifically for this project.`

## Audio

- Music source:
  - Name: `Pixabay game music`
  - Link: `https://pixabay.com/zh/music/search/game%20music/`
- Sound effects source:
  - Name: `Pixabay sound effects`
  - Link: `https://pixabay.com/zh/music/search/sound%20effects/`

### Confirmed audio files currently used in Unity

- Background music:
  - Local file: `Assets/Art/Asset/Sound/backgroundmusicforvideos-roblox-minecraft-fortnite-video-game-music-358426.mp3`
  - Assigned use: `backgroundMusic`
  - Pixabay title: `Roblox Minecraft Fortnite Video Game Music`
  - Creator: `BackgroundMusicForVideos`
  - Link: `https://pixabay.com/music/search/gameplay%20background/`
- UI click:
  - Local file: `Assets/Art/Asset/Sound/skyscraper_seven-click-buttons-ui-menu-sounds-effects-button-7-203601.mp3`
  - Assigned use: `uiClickSound`
  - Pixabay title: `Click Buttons - UI Menu Sounds Effects - Button 7`
  - Creator: `skyscraper_seven`
  - Link: `https://pixabay.com/sound-effects/film-special-effects-click-buttons-ui-menu-sounds-effects-button-7-203601/`
- Card play:
  - Local file: `Assets/Art/Asset/Sound/oxidvideos-taking-playing-card-3-522513.mp3`
  - Assigned use: `cardPlaySound`
  - Pixabay title: `Taking Playing Card 3`
  - Creator: `OxidVideos`
  - Link: `https://pixabay.com/hu/sound-effects/film-%C3%A9s-speci%C3%A1lis-effektusok-taking-playing-card-3-522513/`

### Other local audio files in the project

- `Assets/Art/Asset/Sound/Gun.mp3`
- `Assets/Art/Asset/Sound/Victory.mp3`
- `Assets/Art/Asset/Sound/building.mp3`
- `Assets/Art/Asset/Sound/buttonClick.mp3`
- `Assets/Art/Asset/Sound/death.mp3`
- `Assets/Art/Asset/Sound/audley_fergine-ui-button-click-5-327756.mp3`

### Audio source links still needed

These files are assigned in `BootstrapScene` through the main `AudioManager`. The source platform is Pixabay; the exact item links should be added when they are matched.

- Tower shooting:
  - Local file: `Assets/Art/Asset/Sound/Gun.mp3`
  - Assigned use: `towerShootSound`
  - Source status: `Pixabay item source link to be added when matched`
- Enemy death:
  - Local file: `Assets/Art/Asset/Sound/death.mp3`
  - Assigned use: `enemyDeathSound`
  - Source status: `Pixabay item source link to be added when matched`
- Tower build:
  - Local file: `Assets/Art/Asset/Sound/building.mp3`
  - Assigned use: `buildSound`
  - Source status: `Pixabay item source link to be added when matched`
- Victory:
  - Local file: `Assets/Art/Asset/Sound/Victory.mp3`
  - Assigned use: `victorySound`
  - Source status: `Pixabay item source link to be added when matched`

### Unassigned local audio file

- Local file: `Assets/Art/Asset/Sound/buttonClick.mp3`
- Current status: present in project files but not assigned in `BootstrapScene`
- Source status: `Pixabay item source link to be added if this file is later used or confirmed`

### Additional identified Pixabay file

- Local file: `Assets/Art/Asset/Sound/audley_fergine-ui-button-click-5-327756.mp3`
- Current status: present in project files but not currently assigned by the main `AudioManager`
- Pixabay title: `UI Button Click #5`
- Creator: `Audley_Fergine`
- Link: `https://pixabay.com/sound-effects/ui-button-click-5-327756/`

## Tutorials and technical references

- Unity tutorial or documentation reference:
  - Topic: `General Unity editor, scene setup, and scripting reference`
  - Link: `No single tutorial page is being claimed here. This project used normal Unity documentation and standard editor reference during development.`
- Networking / Photon reference:
  - Topic: `Photon PUN 2 and Photon Realtime documentation`
  - Link: `Used as the technical basis for the online room and multiplayer sync implementation together with the Photon package included in the project.`
- AI / gameplay / pathfinding reference:
  - Topic: `Internal design iteration and project-specific implementation discussion`
  - Link: `No single external gameplay or pathfinding article is being formally cited here.`

## AI-assisted coding or documentation

- AI coding support:
  - Tool / model: `OpenAI GPT Codex 5.4 and GPT Codex 5.5`
  - Link: `https://openai.com/codex/`
  - Notes: `Used mainly for coding assistance, debugging support, refactoring suggestions, and documentation drafting.`
- AI-generated or AI-assisted art reference:
  - Tool: `GPT image / visual generation workflow for card and card-slot art`
  - Link: `Used as project-specific generative support for card visuals; no separate asset-pack page is being cited here.`
  - Notes: `Used to generate the card visuals and card-slot presentation assets for this project.`

## Templates or borrowed structure

- Planning / feature / bug / integration template source:
  - Link: `Templates are stored locally in FinalGame/template and were used as lightweight internal project-management support.`

## Local project assets already used

### Tiny Swords castle sprites

- `Assets/Art/Asset/Tiny Swords (Update 010)/Factions/Knights/Buildings/Castle/Castle_Blue.png`
- `Assets/Art/Asset/Tiny Swords (Update 010)/Factions/Knights/Buildings/Castle/Castle_Red.png`
- `Assets/Art/Asset/Tiny Swords (Update 010)/Factions/Knights/Buildings/Castle/Castle_Purple.png`
- `Assets/Art/Asset/Tiny Swords (Update 010)/Factions/Knights/Buildings/Castle/Castle_Yellow.png`

### Door-related local asset used in the project

- `Assets/Art/Asset/Tiny Swords (Update 010)/Factions/Goblins/Buildings/Wood_Tower/Wood_Tower_Blue.png`
