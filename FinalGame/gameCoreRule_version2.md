# Game Design Document v2 (System Version)

## 1. Game Positioning

This game is a 2–4 player turn-based strategy tower defense game.

Players do not directly attack each other. Instead, they influence the game through:
- Controlling path nodes (gates)
- Competing for territory
- Using cards to interfere
- Building and upgrading towers

The game allows a high degree of player freedom and interaction.  
It does not enforce a single optimal strategy.

---

## 2. Target Players

This version targets:

- Players who enjoy strategic multiplayer games  
- Players who prefer indirect competition  
- Players who enjoy systems with uncertainty and interaction  
- Casual to mid-core strategy players  

The game rewards players who can:
- Read the current situation  
- Predict other players  
- Make timely decisions  

---

## 3. Design Philosophy

Players compete by manipulating a shared system rather than directly attacking each other.

---

## 4. Game Objective

Players aim to:
- Protect their own base  
- Gain higher score through monster kills and system control  

Final ranking is based on total score.

---

## 5. Core Systems Overview

The game includes:

- Base system (health and elimination)  
- Score system  
- Coin economy  
- Turn and wave structure  
- AP (action point) system  
- Card system  
- Territory system  
- Tower system  
- Enemy system  

---

## 6. Key System Concepts

### AP vs Coins
- AP is used for strategic actions  
- Coins are used for building and expansion  

### Territory Control
- Nodes correspond to control points  
- Ownership determines path direction  

### Cards
- Provide temporary advantages or disruption  
- Used to break stable situations  

---

## 7. Core Gameplay Loop

Draw card  
→ Decide actions (AP and coins)  
→ Play card or change path  
→ End turn  
→ Monster wave resolves  

---

## 8. Gameplay Decisions

Players constantly balance:
- Defense vs aggression  
- Saving vs spending resources  
- Stability vs disruption  

---

## 9. Core Focus

The design emphasizes:

- Path manipulation  
- Territory competition  
- Player interaction through cards  
- Indirect player conflict  

---

## 10. System Depth

Compared to v1, this version introduces:

- Detailed AP rules  
- Territory ownership mechanics  
- Card interactions  
- Multi-wave structure  
- Resource balancing  

---

## 11. Summary

A multiplayer strategy game where players compete by controlling paths and using cards to influence a shared system instead of direct combat.