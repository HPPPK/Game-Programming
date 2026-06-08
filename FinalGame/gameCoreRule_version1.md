# Game Design Document v1 (Concept Version)

> This is a historical design iteration document. The final assessed design is summarised in [FinalGame/FINAL_GAME_DESIGN.md](./FINAL_GAME_DESIGN.md).

## 1. Game Positioning

This game is a multiplayer turn-based tower defense party game.

Players do not directly attack each other. Instead, they interact by:
- Controlling path directions (gates)
- Occupying key tiles
- Using cards to interfere with others
- Building towers to stop monsters

The game focuses on player interaction, unpredictability, and decision-making.

It is not designed to be strictly balanced or competitive. Instead, it allows:
- Interference
- Risk-taking
- Unpredictable outcomes

---

## 2. Target Players

This game is designed for:

- Players who enjoy multiplayer interaction and competition  
- Players who like party games such as Monopoly or Mario Party  
- Players who accept unfair or chaotic situations  
- Casual to mid-core players  

In short, the game targets players who enjoy affecting others while trying to survive themselves.

---

## 3. Game Goal

Players aim to:
- Protect their own base  
- Achieve a higher score than others  

Score comes from:
- Killing monsters (positive)  
- Losing base health (negative)  

Final ranking is determined by total score.

---

## 4. Core Systems

### Player
- Has coins, cards, and control over tiles  
- Takes turns  

### Base
- Each player has one base  
- Takes damage from monsters  

### Monster
- Spawns from entrances  
- Moves along fixed paths  
- Damages base if not stopped  

### Tower
- Automatically attacks monsters  
- Only basic tower is included in this version  

---

## 5. Tile Control System

Tiles represent control over key points on the map.

- Each tile corresponds to a path node (gate)  
- Only the owner can change direction  
- Direction changes persist  

---

## 6. Path System

- Monsters follow fixed paths  
- No dynamic pathfinding  
- Direction is decided at intersections  
- Players control intersections through gates  

---

## 7. Card System

Cards introduce temporary changes and interaction.

Examples:
- Force path change  
- Disrupt another player  
- Boost towers  

Rules:
- Draw 1 card per turn  
- Play at most 1 card per turn  

---

## 8. Economy

- Coins are gained by killing monsters  
- Coins are used for:
  - Towers  
  - Tile control  
  - (optional future card costs)  

---

## 9. Turn Structure

Each player turn:
1. Draw a card  
2. Optionally play a card  
3. Perform actions (build, control, upgrade)  
4. End turn  

After all players:
- Monsters move and resolve  

---

## 10. Core Gameplay Experience

Players constantly decide:
- Whether to defend themselves  
- Whether to redirect monsters to others  
- Whether to use or save cards  

---

## 11. Summary

A turn-based multiplayer tower defense game where players manipulate paths and use cards to influence each other within a shared system.
