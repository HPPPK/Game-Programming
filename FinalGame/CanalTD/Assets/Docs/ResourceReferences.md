# Resource References

This file records the main external resources, AI-assisted support, and reference sources used during development of CanalTD.

## Game assets

| Resource type | Local / project use | Source / creator | Source link | Licence / attribution note | Traceability status |
|---|---|---|---|---|---|
| Main visual asset pack | Environment, castle, enemy, tower, and map-style assets | `Tiny Swords` / Pixel Frog | https://pixelfrog-assets.itch.io/tiny-swords | Source page for the asset pack | Source link recorded |
| Project-specific generated visuals | Card art and card-slot presentation visuals | AI-assisted visual generation workflow | No separate public asset page; generated specifically for this project | Disclosed as AI-assisted project visuals | Disclosed in project documentation |

## Audio

- Music source:
  - Name: `Pixabay game music`
  - Link: `https://pixabay.com/zh/music/search/game%20music/`
- Sound effects source:
  - Name: `Pixabay sound effects`
  - Link: `https://pixabay.com/zh/music/search/sound%20effects/`

### Confirmed audio files currently used in Unity

| Resource type | Local path / file | Source / creator | Source link | Licence / attribution note | Used for | Traceability status |
|---|---|---|---|---|---|---|
| Music | `Assets/Art/Asset/Sound/backgroundmusicforvideos-roblox-minecraft-fortnite-video-game-music-358426.mp3` | `Roblox Minecraft Fortnite Video Game Music` / `BackgroundMusicForVideos` | https://pixabay.com/music/video-games-roblox-minecraft-fortnite-video-game-music-358426/ | Pixabay source page | `backgroundMusic` | Complete source link recorded |
| Sound effect | `Assets/Art/Asset/Sound/skyscraper_seven-click-buttons-ui-menu-sounds-effects-button-7-203601.mp3` | `Click Buttons - UI Menu Sounds Effects - Button 7` / `skyscraper_seven` | https://pixabay.com/sound-effects/film-special-effects-click-buttons-ui-menu-sounds-effects-button-7-203601/ | Pixabay source page | `uiClickSound` | Complete source link recorded |
| Sound effect | `Assets/Art/Asset/Sound/oxidvideos-taking-playing-card-3-522513.mp3` | `Taking Playing Card` / `OxidVideos` | https://pixabay.com/sound-effects/film-special-effects-taking-playing-card-522520/ | Pixabay source page | `cardPlaySound` | Complete source link recorded |
| Sound effect | `Assets/Art/Asset/Sound/Gun.mp3` | `Shoot 5` / `freesound_community` | https://pixabay.com/sound-effects/film-special-effects-shoot-5-102360/ | Pixabay source page | `towerShootSound` | Complete source link recorded |
| Sound effect | `Assets/Art/Asset/Sound/death.mp3` | `Dramatic Death Collapse` / `Universfield` | https://pixabay.com/sound-effects/film-special-effects-dramatic-death-collapse-352720/ | Pixabay source page | `enemyDeathSound` | Complete source link recorded |
| Sound effect | `Assets/Art/Asset/Sound/building.mp3` | `Gaming Victory` / `EAGLAXLE` | https://pixabay.com/sound-effects/gaming-victory-464016/ | Pixabay source page | `buildSound` | Complete source link recorded |
| Sound effect | `Assets/Art/Asset/Sound/Victory.mp3` | `Victory Chime` / `Scratchonix` | https://pixabay.com/sound-effects/musical-victory-chime-366449/ | Pixabay source page | `victorySound` | Complete source link recorded |

### Short local audio filenames already covered above

- `Assets/Art/Asset/Sound/Gun.mp3`
- `Assets/Art/Asset/Sound/Victory.mp3`
- `Assets/Art/Asset/Sound/building.mp3`
- `Assets/Art/Asset/Sound/death.mp3`

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

| Assistance area | Tool type | Used for | Responsibility note |
|---|---|---|---|
| Coding and debugging support | AI-assisted coding and documentation tools | Selected coding suggestions, debugging suggestions, refactoring suggestions, and documentation drafting | Final design choices, Unity integration, testing, and acceptance/rejection of suggestions remained the student's responsibility. |
| Visual/card support | AI-assisted visual generation workflow | Project-specific card visuals and card-slot presentation assets | These visuals are disclosed as generated support rather than an external asset-pack source. |

## Templates or borrowed structure

- Local coursework planning templates were used as private planning support.
- `FinalGame/template` exists locally but is intentionally excluded by `.gitignore` through the `template/` rule.
- The template folder is not committed GitHub evidence and is not required for running or assessing the final game.
- Submitted assessment evidence is provided through the README, final design document, testing log, development log, Kanban evidence, contribution notes, and resource references.

## Items with incomplete traceability

- No currently assigned audio file is documented here with incomplete traceability.
- If an additional asset is later found without a source link, it should be added to this section as `Incomplete` rather than given an invented source.

## Local project assets already used

### Tiny Swords castle sprites

- `Assets/Art/Asset/Tiny Swords (Update 010)/Factions/Knights/Buildings/Castle/Castle_Blue.png`
- `Assets/Art/Asset/Tiny Swords (Update 010)/Factions/Knights/Buildings/Castle/Castle_Red.png`
- `Assets/Art/Asset/Tiny Swords (Update 010)/Factions/Knights/Buildings/Castle/Castle_Purple.png`
- `Assets/Art/Asset/Tiny Swords (Update 010)/Factions/Knights/Buildings/Castle/Castle_Yellow.png`

### Door-related local asset used in the project

- `Assets/Art/Asset/Tiny Swords (Update 010)/Factions/Goblins/Buildings/Wood_Tower/Wood_Tower_Blue.png`
