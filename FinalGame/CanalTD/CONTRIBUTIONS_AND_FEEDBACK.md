# Contributions and Feedback Notes

## Purpose of this document

This file explains my personal contribution to the CanalTD project, clarifies where AI-assisted help was used, and records feedback from peers and tutors together with the changes I made in response.

## Summary of my contribution

My contribution covers a large part of the project across gameplay design, systems design, Unity scene implementation, user-experience improvements, AI-mode planning, multiplayer integration support, and documentation preparation.

I did not only work on one isolated script or one visual feature.  
My work influenced the project at several levels:

- core gameplay rules
- enemy pathing logic and route-selection ideas
- AI difficulty structure and behaviour goals
- user feedback and tutorial clarity
- scene construction and Unity object setup
- UI polish and readability
- system integration between cards, towers, players, enemies, and online flow
- ongoing debugging and final documentation

## Personal contribution evidence table

| Area / system | Main files or scenes | What I personally did | AI assistance used? | Evidence / related issue or document |
|---|---|---|---|---|
| Game concept and core rules | `FinalGame/FINAL_GAME_DESIGN.md`, `FinalGame/prototype.md`, `FinalGame/gameCoreRule_version*.md` | Defined the core CanalTD idea, player goal, indirect pressure concept, and final scope distinction between final and historical design documents. | AI-assisted documentation support was used. | [FINAL_GAME_DESIGN.md](../FINAL_GAME_DESIGN.md) |
| Card system | `Assets/Scripts/Card` | Integrated card draw/play/targeting behavior and documented how card interaction supports the vertical slice. | AI-assisted coding/debugging suggestions were used selectively. | [SYSTEM_OVERVIEW.md](./SYSTEM_OVERVIEW.md), issue [#95](https://github.com/HPPPK/Game-Programming/issues/95) |
| Turn system / AP rules | `Assets/Scripts/Core`, `Assets/Scripts/UI` | Developed and integrated turn/AP restrictions, turn feedback, and active-player clarity. | AI-assisted debugging/refactoring suggestions were used selectively. | [TESTING_LOG.md](../TESTING_LOG.md), issue [#90](https://github.com/HPPPK/Game-Programming/issues/90) |
| Tower building / upgrading / selling | `Assets/Scripts/Tower`, `Assets/Scenes/GameScene.unity` | Integrated tower build, upgrade, sell, ownership/resource checks, and tower defense behavior. | AI-assisted coding suggestions were used selectively. | [SYSTEM_OVERVIEW.md](./SYSTEM_OVERVIEW.md) |
| Enemy routing / canal pressure | `Assets/Scripts/Enemy`, `Assets/Scripts/Path`, `Assets/Scripts/Map` | Designed and integrated route pressure as the main indirect competition mechanic. | AI-assisted explanation/debugging support was used selectively. | Issue [#29](https://github.com/HPPPK/Game-Programming/issues/29) |
| AI mode difficulty structure | `Assets/Scripts/Core/AIPrototypeTurnManager.cs`, `Assets/Scripts/Core/AIDifficulty.cs` | Designed AI difficulty expectations and integrated AI route as a primary assessment mode. | AI-assisted coding/debugging suggestions were used selectively. | [FINAL_GAME_DESIGN.md](../FINAL_GAME_DESIGN.md) |
| UI feedback / turn clarity | `Assets/Scripts/UI`, `Assets/Scenes/GameScene.unity` | Improved active-turn indication, player status readability, warnings/toasts, and result feedback. | AI-assisted documentation/refactoring suggestions were used selectively. | Issues [#90](https://github.com/HPPPK/Game-Programming/issues/90), [#93](https://github.com/HPPPK/Game-Programming/issues/93) |
| Guide scene / onboarding | `Assets/Scripts/Tutorial`, `Assets/Scenes/GuideScene.unity` | Created and documented the beginner guide/onboarding route. | AI-assisted documentation support was used. | Issues [#92](https://github.com/HPPPK/Game-Programming/issues/92), [#95](https://github.com/HPPPK/Game-Programming/issues/95)-[#97](https://github.com/HPPPK/Game-Programming/issues/97) |
| Online mode integration/debugging | `Assets/Scripts/Networking`, `Assets/Scenes/ModeSelectScene.unity` | Integrated online room/lobby/sync work and documented Online Mode as an extension. | AI-assisted debugging suggestions were used selectively. | [ONLINE_STATUS.md](./ONLINE_STATUS.md), issues [#62](https://github.com/HPPPK/Game-Programming/issues/62), [#89](https://github.com/HPPPK/Game-Programming/issues/89), [#91](https://github.com/HPPPK/Game-Programming/issues/91) |
| Audio / feedback polish | `Assets/Scripts/UI/AudioManager.cs`, `Assets/Art/Asset/Sound` | Integrated and documented sourced audio feedback. | AI-assisted documentation support was used. | [ResourceReferences.md](./Assets/Docs/ResourceReferences.md), issue [#87](https://github.com/HPPPK/Game-Programming/issues/87) |
| Documentation and submission preparation | `README.md`, `FinalGame/*.md`, `FinalGame/CanalTD/*.md` | Prepared assessment-facing docs, testing log, final design, process evidence, checklist, and system overview. | AI-assisted drafting and cleanup support was used. | [SUBMISSION_CHECKLIST.md](../SUBMISSION_CHECKLIST.md) |
| GitHub Issues / Kanban process | `FinalGame/KANBAN_AND_ISSUE_EVIDENCE.md` | Used Issues/Project board as process tracking and documented closed issues as completed work. | AI-assisted documentation support was used. | [KANBAN_AND_ISSUE_EVIDENCE.md](../KANBAN_AND_ISSUE_EVIDENCE.md) |

## Detailed contribution analysis

### 1. Enemy routing strategy and path-calculation contribution

One of my most important contributions was the design direction behind the enemy movement strategy.

This included:

- thinking about how enemies should move through a shared canal / path network instead of using a single fixed lane
- shaping the idea that pressure should feel fair enough to be readable, but still dynamic enough to create player interaction
- supporting the design where enemies can be distributed toward reachable castles instead of always collapsing onto one destination
- contributing to the route-selection logic that works from path lists, reachable nodes, and recalculated results after control or gate changes

More specifically, my contribution here was not only “making enemies move.”  
It was the gameplay logic behind why they move in a certain way.

I contributed to:

- the idea of **equal or balanced pressure distribution**
- the idea that route choice should react to the current graph state
- the logic direction where path availability is calculated first, then valid paths are collected into lists, then a result path is selected from those lists
- the design of a system where route calculation supports both strategic control and unpredictable pressure

This matters because the game is built around indirect competition.  
If enemies always moved in the simplest possible way, the game would lose much of its strategic depth.  
The routing system had to support:

- readable player decisions
- meaningful control over intersections
- shifting pressure between opponents
- enough variation that the game still feels active and competitive

That routing and path-selection design is one of the clearest creative contributions I made to the project.

### 2. AI mode strategy design for three levels

Another major contribution was the design direction for the AI mode, especially the structure of the three difficulty levels.

My work here included:

- deciding that AI should not just be “faster” or “have more resources,” but should feel strategically different
- helping define different behaviour expectations for easy, medium, and hard AI
- shaping how AI should evaluate cards, targets, path pressure, and tactical moments
- helping determine how aggressive or disruptive AI should be at different levels

The main design idea was that each AI level should communicate a different style of challenge:

- **Easy AI**
  - should be understandable and less punishing
  - should make simpler or more conservative decisions
  - should allow new players to learn the rules and the pacing of the game

- **Medium AI**
  - should use the systems more coherently
  - should begin making better card and targeting decisions
  - should create pressure without feeling unfair

- **Hard AI**
  - should make stronger use of available tactics
  - should react better to player state and opportunities
  - should use the rules in a way that makes the game feel more competitive

My contribution here was especially important because AI difficulty affects assessability:

- it shows the systems can run without requiring four human players
- it demonstrates decision-making depth
- it helps prove that the project has meaningful interaction and challenge

### 3. Core rule design and interaction structure

I also contributed strongly to the project’s rule design.

This includes the way the game combines:

- turn-based decision making
- cards as interaction tools
- towers as defense tools
- path control as indirect offense
- score and survival pressure as the long-term objective

My contribution was not only technical.  
A large part of it was deciding what kind of game this should be.

I helped shape rules such as:

- what actions matter during a player turn
- how cards create interaction without direct combat
- how map control and redirection become a form of attack
- how enemy waves create shared pressure across players
- how tower building and tactical interference can coexist in one turn structure

This design work is part of the project’s creativity contribution because it creates:

- a new combination of mechanics
- new constraints on how players influence one another
- a clearer game goal built around indirect competition rather than direct damage

### 4. Unity scene construction and practical implementation contribution

A large part of my contribution was the practical Unity work needed to make the project real and playable.

This includes:

- scene setup
- object placement
- inspector reference wiring
- UI hierarchy organisation
- gameplay object positioning
- layout adjustment
- flow between scenes such as Home, Mode Select, Guide, Game, and Result

I also contributed to the many production tasks that often do not show up clearly in one script but are essential to the final game:

- moving objects to more readable positions
- cleaning UI spacing and visibility
- adjusting scene presentation
- making sure the player can follow the game state more easily
- connecting systems that were implemented separately into a working scene

This Unity-side integration is a significant contribution because the project is not assessable if systems only work in isolation.  
A major part of my work was making the game understandable and playable inside the actual Unity scenes.

### 5. UI polish and readability contribution

I also contributed a lot to UI polish and clarity.

This includes:

- making status information easier to read
- helping the player understand whose turn it is
- improving the placement and logic of visual indicators
- adjusting feedback so actions are easier to follow
- making the game easier to demonstrate during assessment

I contributed to the idea that the UI should support:

- turn clarity
- readable action feedback
- understandable player state
- a more polished and less confusing overall experience

This work matters because a project can have good systems but still be hard to assess if the player cannot understand what is happening.

### 6. Turn indication and player feedback contribution

One specific contribution that came directly from peer feedback was improving turn indication.

I designed and integrated:

- a clearer current-player label near the round / turn display
- a turn marker / turn indication element that visually points to the active player
- stronger coordination between the player turn system and the feedback the UI gives

This was important because in a multi-player strategy game, confusion about whose turn it is immediately hurts playability.

This improvement directly supports the course expectations around:

- clarity
- feedback
- assessability
- working controls and understandable interaction

### 7. Guide scene and onboarding contribution

Another clear contribution was designing a dedicated Guide scene to improve onboarding.

This was created in response to feedback that the game needed a more beginner-friendly explanation and step-by-step introduction.

My contribution here included:

- deciding that a separate guide flow would be better than relying only on a text explanation
- designing a more guided entry point for first-time users
- improving the project’s accessibility for players who are unfamiliar with the rules

This supports the assessment criteria because it makes:

- the controls clearer
- the goal clearer
- the game easier to run and understand

### 8. Script organisation and system categorisation

I also contributed to the way the codebase is organised into meaningful groups.

The scripts are structured into categories such as:

- `Card`
- `Core`
- `Enemy`
- `Networking`
- `Path`
- `Tower`
- `Tutorial`
- `UI`

This kind of organisation is important for:

- maintainability
- individual review and debugging
- debugging
- making contribution clearer during marking

This is part of my contribution because project clarity is not only about gameplay; it is also about how understandable the implementation becomes.

### 9. Online mode integration and debugging contribution

I also spent meaningful time contributing to online mode integration and debugging.

This includes:

- room and scene flow understanding
- online card-hand sync work
- private-hand and public-hand separation
- authoritative card sync debugging
- connection and lifecycle regression investigation
- player-targeting sync fixes

Even where AI tools helped suggest code-level fixes, I was still responsible for:

- identifying the actual gameplay requirement
- deciding what the online system should and should not do
- testing whether the fix matched the design intent
- deciding what counts as acceptable multiplayer behaviour

### 10. Documentation and assessment-readiness contribution

I also contributed to making the project more assessable through documentation.

This includes:

- clarifying README content
- preparing structured contribution notes
- documenting feedback and responses
- improving script comment coverage to meet the course expectation for assessable work

This part matters because coursework is not only about implementation.  
It also needs to clearly communicate:

- what the game is
- how it runs
- what the player does
- what my contribution was

## Creativity contribution

Based on the course slides, creativity can include:

- a new rule
- a new constraint
- a new goal
- a new challenge
- a new combination
- better pacing
- better accessibility

I believe my contribution supports creativity in several of those ways:

### New combination

I helped shape a game that combines:

- tower defense
- turn-based strategy
- indirect player-versus-player interaction
- card disruption
- route control through a shared path network

### New constraint

Players do not simply attack each other directly.  
Instead, they influence a shared enemy-flow system.  
That creates a different kind of strategy constraint from more standard battle systems.

### Better pacing

My work on turn structure, AI strategy direction, path pressure, and UI feedback all contributed to a more readable pace.

### Better accessibility

The Guide scene, clearer turn indication, and UI polish are direct contributions to accessibility and assessability.

## AI-assisted work

AI tools were used as development support, not as the origin of the project concept.

### Areas where AI helped

- generating or refining some implementation scaffolding
- suggesting fixes for specific bugs
- helping refactor or reorganise code during debugging
- helping write documentation headers and README structure
- helping explain or compare potential implementation approaches during online sync work

### Areas where I remained responsible

I remained responsible for:

- deciding the design direction
- deciding the rules
- deciding what behaviour the systems should have
- deciding which implementation approach matches the game design
- deciding which suggestions to accept or reject
- testing and evaluating whether changes actually improved the project

So although AI support was used, the creative and integrative direction of the project remained my own contribution.

Final design decisions, Unity integration, testing responsibility, accepting or rejecting AI suggestions, and final submission responsibility remained mine.

## Peer and tutor feedback, with my response

### Feedback from Sihan Wang and Yiran Xu

Feedback:

- The active user turn was not clear enough.
- Players needed stronger visual indication of whose turn it currently was.

My response:

- I designed a clearer current-turn label next to the round / turn area.
- I added a turn marker so the active player can be identified more quickly.
- I continued refining player-status and turn-related UI so the gameplay state is easier to read.

Why this change mattered:

- It improved clarity.
- It reduced confusion in multi-player situations.
- It made the game more assessable because the active player is now easier to identify during demos and playtests.

### Feedback from Yajin Xu

Feedback:

- The game needed a more user-friendly beginner guide.
- A step-by-step tutorial or guided introduction would help new players understand the rules faster.

My response:

- I designed a dedicated Guide scene.
- I used the Guide scene to make the onboarding process more structured.
- I improved the project’s approach to first-time-user explanation instead of relying only on immediate gameplay discovery.

Why this change mattered:

- It improved accessibility.
- It made the game easier for new players to approach.
- It better matched the course expectation that the game should clearly explain how to play.

### Broader feedback themes I responded to

Across peer and class feedback, the main repeated themes were:

- stronger turn clarity
- better onboarding
- more readable feedback
- clearer explanation of contribution
- better documentation for assessment

My later changes intentionally responded to those themes through:

- UI improvements
- tutorial / guide flow
- documentation work
- clearer project structuring

## Final statement

My contribution to CanalTD is broad and substantial.

It includes:

- core gameplay design decisions
- enemy routing strategy ideas
- path-related logic direction
- AI difficulty strategy design
- UI and clarity improvements
- Unity scene and object integration
- onboarding improvements
- online integration and debugging support
- documentation and assessment preparation

AI tools helped with some implementation and writing support, but the project direction, gameplay reasoning, system goals, and many integration decisions remained my own work.
