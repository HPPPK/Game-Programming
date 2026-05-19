# Activity 1 (Tuesday, 19 May 2026)

## Title
CanalTD

---

## One sentence
A 2D top-down multiplayer turn-based strategy tower defense game where players compete for score by buying and seizing land, controlling canal gates, building towers, using tactical cards, and defending their castles across escalating enemy waves.

---

## Player action
Build towers, buy and seize land, manipulate canal gates, play tactical cards, defend castles, and compete against other players through turn-based strategic decisions.

---

## Unity format
2D / Top-down

---

## Vertical slice (Smallest playable version is what)
A playable 4-player local prototype where players take turns building towers, controlling canal gates, buying land, and using cards to redirect enemy waves while defending castles and competing for score.

---

## Core systems (The Unity system/scripts needed first)
- Turn Management System
- Gate Control System
- Tower Building System
- Enemy Wave System
- Card System
- Territory Ownership System
- Score and Resource System
- Targeting and Interaction System

---

## GitHub status (Repo link/repo not yet created/setup plan)
Repository created and used for version control, feature development, and issue tracking for the CanalTD project.

GitHub Repository:  
https://github.com/HPPPK/Game-Programming/

---

## Biggest risk (what might stop the project succeeding?)
The biggest risk is controlling the difficulty and scope of the project. The game combines multiplayer strategy, tower defense, territory ownership, gate/path control, and card systems, which could become difficult to balance and complete within the available development time.

To reduce this risk, the project will focus on finishing a polished vertical slice first while postponing advanced systems such as online multiplayer, advanced AI players, tower upgrade trees, and large enemy variety.

---

## Next action (one visible task before next session)
Implement the Take Over card targeting system, including:
- dark overlay targeting mode
- selectable opponent-owned tiles
- confirm/cancel targeting interaction
- ownership transfer
- tower removal from seized land
- delayed activation until the player's next turn
- ownership color updates
- proper AP/card consumption handling

---

# Peer Feedback

## Feedback Received
The peer feedback was mainly about controlling project scope. Since the game already combines several complex systems, including turn-based strategy, tower defense, territory ownership, canal gate control, and tactical cards, adding online multiplayer too early could make the project too difficult to finish and test properly. (From Yuhang Chen)

The feedback also suggested that the project should focus on polishing the local multiplayer version first. Instead of immediately adding networking, a better expansion direction could be creating additional maps or different map layouts, because new maps would improve replayability while staying closer to the existing core mechanics. (From Jiachi Zhu)

## Reflection on Feedback
This feedback is useful because the biggest challenge of CanalTD is not a lack of ideas, but controlling how many systems are developed at the same time. Online multiplayer, advanced AI, many enemy types, tower upgrade trees, and multiple maps are all interesting features, but adding them too early could make the prototype unstable.

Based on the feedback, the project direction will focus on completing a strong local multiplayer vertical slice first. The current priority is to make sure that the main gameplay loop works clearly:

- players take turns
- players use cards
- players buy or seize land
- players control canal gates
- enemy waves move through the map
- towers attack enemies
- castles lose HP
- players gain or lose score
- the final ranking screen shows the result

Once this core loop is stable, the project can expand in safer directions.

## Planned Response to Feedback
The project will delay online multiplayer and advanced AI systems until the local gameplay is stable. This makes the project more realistic for the course timeline and reduces the risk of spending too much time on networking problems before the main gameplay is fun.

The next development focus will be:

- polishing the card interaction system
- improving territory control
- making gate control clearer
- strengthening the scoring system
- improving wave progression
- testing whether the game is understandable to players

If there is enough time after the vertical slice is complete, additional maps may be added as an expansion. This would follow the peer suggestion and give the game more variety without requiring a full online multiplayer system.