# Game Rules and System Design (v3)

> This is a historical design iteration document. The final assessed design is summarised in [FinalGame/FINAL_GAME_DESIGN.md](./FINAL_GAME_DESIGN.md). Older AP and gate-focused rule wording here records design evolution and should not be read as the final implemented rule set.

## 1. Game Positioning

This game is a 2–4 player turn-based strategy tower defense game.

Players do not directly attack each other. Instead, they influence the game by:
- Controlling canal gates (path direction)
- Competing for territory
- Using cards to interfere with others
- Building towers to control monster flow

The game is not designed to enforce strict balance or a single optimal strategy.  
Instead, it emphasizes:
- Player freedom
- Interaction
- Unpredictable outcomes

As long as actions are within the rules, all strategies are considered valid, including defensive play, aggressive interference, and actions that may negatively affect other players.

---

## 2. Player Count, Modes, and Objectives

### 2.1 Player Count
- Designed primarily for 4 players  
- Supports 2–3 players with reduced interaction density  

### 2.2 Game Modes
Default mode is Free-For-All (FFA).

Optional team mode (e.g. 2v2):
- Players act independently
- No shared resources (coins, cards, territory)
- Team communication is allowed
- Final results are based on team score

The system does not restrict player behaviour (e.g. redirecting pressure toward another player).

### 2.3 Objective
Players aim to:
- Protect their own base  
- Achieve the highest score  

Final ranking is determined by total score.

---

## 3. Base, Elimination, and End Conditions

Each player has one base.

- Initial HP: 20  
- Monsters deal fixed damage upon reaching the base  
- HP cannot go below 0  

When a player's HP reaches 0:
- The player loses all future actions  
- The player keeps their current score  
- The player remains in the final ranking  

### Towers After Elimination
- Towers remain active until the end of the current wave  
- From the next wave onward, they become inactive  

This avoids unnecessary complexity in ownership transfer.

---

## 4. Score System

Score is the only factor determining victory.

### 4.1 Score Sources
- Monster kills → positive score  
- Base damage → negative score  

### 4.2 Rules
- Normal / Fast / Tank monsters: +1  
- Boss: +5  
- Base damage: -1 per HP  

### 4.3 Design Intention
This system ensures:
- Offensive actions are rewarded  
- Poor defense is punished  
- Players can choose their own playstyle  

Score has no upper limit.

---

## 5. Coin System

Coins are used for construction and expansion.

### 5.1 Sources
- +1 coin per non-boss kill  
- +5 coins for boss  

### 5.2 Usage
Coins are used for:
- Building towers  
- Upgrading towers  
- Buying or taking over territory  

Cards do not consume coins by default.

### 5.3 Starting Coins
- 12 coins per player  

This allows early defense but prevents immediate expansion.

---

## 6. Turn and Wave Structure

Each wave consists of:

1. Player Phase  
2. Monster Phase  

### 6.1 Player Phase
Each player:
1. Draws 1 card  
2. Gains 2 AP  
3. Performs actions  
4. Ends turn  

### 6.2 Monster Phase
After all players:
- Monsters spawn  
- Follow current gate directions  
- Towers attack  
- Rewards and damage are resolved  

### 6.3 Design Intention
Monsters do not move during player turns.

This ensures:
- Clarity of decisions  
- Predictable outcomes  
- No real-time pressure  

---

## 7. Turn Order

- First wave: random order  
- Each wave: rotate order clockwise  

This avoids permanent first-player advantage.

---

## 8. AP System

AP represents how much a player can actively change the game state.

### 8.1 AP Per Turn
- 2 AP per turn  

### 8.2 AP Usage
- Change one gate direction: 1 AP  
- Play one card: 1 AP  

### 8.3 Restrictions
- Max 1 card per turn  
- Max 1 gate change per turn  
- Gate-changing cards count toward this limit  
- Each gate can only be changed once per player phase  

### 8.4 Design Intention
These restrictions are intentional.

They:
- Prevent excessive system changes  
- Maintain readability  
- Ensure each action has meaningful impact  

---

## 9. Player Actions

During a turn:

### Mandatory
- Draw 1 card  
- Gain 2 AP  

### Optional (AP)
- Change gate direction  
- Play a card  

### Optional (Coins)
- Build tower  
- Upgrade tower  
- Buy territory  
- Take over territory  

Actions can be performed in any order.

---

## 10. Cards and Deck

### 10.1 Starting Hand
- 2 cards  

### 10.2 Draw
- +1 card per turn  

### 10.3 Hand Limit
- Maximum 3 cards  

Excess cards must be discarded at end of turn.

---

## 11. Card Design

Cards:
- Cost 1 AP  
- Used during player's turn  
- Limited to 1 per turn  

They are designed to:
- Break stable situations  
- Create interaction  
- Introduce temporary advantage  

---

## 12. Map Structure

- Fixed map (no procedural generation)  
- 4 player corners  
- 2–3 spawn points  
- 7 control nodes (gates)  

### Gate Types
- 3 central gates  
- 4 terminal gates  

### Rules
- Binary direction  
- No probability split  
- No loops  

---

## 13. Personal vs Public Area

### Personal Area
- Only owner can build  
- Cannot be captured  
- Used for defense  

### Public Area
- Shared build space  
- No ownership  
- Used for early setup  

---

## 14. Territory System

Each controllable gate corresponds to one territory.

### Rules
- Owner controls direction  
- Can be purchased or taken over  
- Activation is delayed until next turn  

### Design Intention
Prevents immediate chain actions and improves predictability.

---

## 15. Tower System

Two types:

### Cannon Tower
- Stable single-target damage  

### Burst Tower
- Area damage  

### Rules
- No AP cost  
- Only coins  
- Only owner can upgrade  

### Design Intention
Towers support the system but do not dominate gameplay.

---

## 16. Monster System

Includes:
- Normal  
- Fast  
- Tank  
- Boss  

Each wave increases pressure gradually.

---

## 17. Game Start

- Random turn order  
- 20 HP each  
- 12 coins  
- 2 cards  
- All territories neutral  

### Preparation Phase
Before Wave 1:
- Players can build towers using starting coins  
- No AP or cards  

---

## 18. Full Turn Flow

1. Draw card  
2. Gain AP  
3. Perform actions  
4. End turn  
5. Resolve monster phase  

---

## 19. Rule Priority

- Locked gates cannot be changed  
- Gate changes limited per phase  
- Territory activation delayed  
- Card stacking limited  
- Eliminated players lose control  

---

## 20. Design Focus

The core of the system is the interaction between:

- AP (decision capacity)  
- Coins (construction capacity)  
- Gate control (system manipulation)  

Monsters serve as both:
- Resource source  
- Indirect attack mechanism  

---

## 21. Scope

This document describes the full intended system.

The current implementation focuses on:
- Card system (partial)  
- Gate interaction  
- Core gameplay loop  

Other systems are defined here but planned for later development.
