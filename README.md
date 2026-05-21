# CanalTD

## Game Idea

A 2D top-down multiplayer turn-based strategy tower defense game where players compete for score by buying and seizing land, controlling canal gates, building towers, using tactical cards, and defending their castles across escalating enemy waves.

## Controls

- Use the mouse to select cards, land tiles, towers, gates, players, and path targets.
- Click **Draw** to draw one card during your turn.
- Click a card, then click **Play** to use it.
- For targeting cards, select a highlighted target, then click **Confirm**.
- Click **Cancel** to leave targeting mode without spending the card.
- Click **End Turn** to pass to the next player.
- Use build-area clicks to buy land or build towers when allowed.

## Unity Version

- Unity `2022.3.62f3`

## How to Run the Project

1. Open Unity Hub.
2. Add and open the project folder:
   `FinalGame/CanalTD`
3. Open `Assets/Scenes/GameScene.unity`.
4. Make sure `GameScene` and `ResultScene` are included in Build Settings.
5. Press **Play** in the Unity Editor.

## Current Status

- Local 4-player turn flow is implemented.
- Players are represented by four castles.
- Each player has independent resources, score, card count, and card hand.
- The visible card slots switch based on the current player.
- Land ownership and tower ownership are tracked per player.
- Towers damage enemies and scoring supports kill and assist rewards.
- Castle damage reduces the owning player's score.
- Configurable enemy waves are implemented.
- A result scene displays final rankings after the final wave.
- Tactical cards currently include:
  - Freeze Claim
  - Power Boost
  - Shock Trap
  - Steal Card
  - Take Over
  - Trade Hands
  - Disrupt
  - Lock Gate
  - Open Gate

## Planned Features

- More enemy variety.
- Boss waves.
- More card balancing and visual polish.
- Improved home / new game flow.
- Better tutorial and player guidance.
- Possible AI or matchmaking setup in the future.

## Credits

- Game project: CanalTD
- Engine: Unity 2022.3.62f3
- UI text: TextMesh Pro
- Art assets include Tiny Swords style sprites and custom card/tower UI assets used for this class project.

## Development Log

- Built local 4-player turn order and wave phase flow.
- Added independent player hands and current-player card rendering.
- Added player resources, scoring, card counts, and status panels.
- Added tower ownership, projectile owner tracking, kill score, and assist score.
- Added configurable wave difficulty and final ranking scene.
- Added ownership visuals for land and player-specific tower styles.
- Expanded the tactical card system with tile, tower, player, and route targeting modes.
