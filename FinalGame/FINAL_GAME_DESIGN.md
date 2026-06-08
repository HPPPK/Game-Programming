# CanalTD Final Game Design

This is the final assessed design summary for CanalTD. Historical prototype and rule-version documents remain as design-iteration evidence, but this file is the clean final design reference for marking.

## Game title

CanalTD

## One-sentence concept

CanalTD is a turn-based strategy and tower-defense game where players use cards, canal gates, towers, and enemy-route pressure to protect their castle while redirecting danger toward opponents.

## Intended player experience

The intended experience is a readable tactical match where each player makes turn-based decisions, manages limited resources, reacts to enemy waves, and uses indirect interaction instead of direct attacks.

## Core mechanic

The core mechanic is indirect pressure control: players influence enemy routes through canal/path control, cards, and tower placement so that enemy waves become a shared strategic threat.

## Player goal

Protect your own castle, manage resources effectively, survive enemy pressure, and finish with the strongest score and result state.

## Main rules

- Players act through turn-based decisions.
- Cards provide disruption, targeting, or tactical effects.
- Towers defend against enemies that move through the map.
- Canal/path control affects route pressure and enemy movement.
- Player resources limit building, purchasing, and tactical decisions.
- Results are shown through the final result flow.

## Turn structure

Turns are managed through AP and action restrictions. The core loop includes drawing or using cards, selecting targets, building or upgrading towers, interacting with gates/paths, and ending the turn.

## Card interaction

Cards are selected from the player hand, then played, discarded, or used through targeting flows. Targeting interactions include confirm/cancel behavior and should respect turn/AP restrictions.

## Tower building and defence

Players use tower build areas and resource checks to buy land, build towers, upgrade towers, sell towers, and defend against enemy waves. Tower actions are part of the main Local/AI assessment route.

## Enemy routing / canal pressure

Enemies use route and path systems rather than a single fixed lane. Canal and gate control can affect reachable paths, destination pressure, and how enemy waves threaten players.

## Local mode

Local Mode is one of the primary stable assessment routes. It supports the core vertical slice through scene flow, turn structure, cards, towers, enemies, UI feedback, and results.

## AI mode

AI Mode is one of the primary stable assessment routes. It provides AI-controlled opponents with multiple difficulty settings so the game can be assessed without requiring four human players.

## Online mode status

Online Mode is implemented as an extension using Photon-related scripts and room/lobby flow. It is not the primary stable assessment path unless broader multiplayer validation is completed. See `FinalGame/CanalTD/ONLINE_STATUS.md`.

Manual validation of online room joining, scene transition, turn authority, and card/build synchronization must be recorded separately before Online Mode is described as fully stable.

## Win / lose / completion condition

The match completion route leads to `ResultScene`, where ranking/result information is displayed. Manual Unity verification is required to record final pass/fail evidence for every result-state scenario.

## Feedback and UI

The project includes current-turn indicators, player status panels, targeting feedback, warnings/toasts, guide/onboarding flow, audio feedback, and result UI to make the game easier to understand and assess.

## Scope control: what is included in the vertical slice

The assessed vertical slice includes the Bootstrap/Home/ModeSelect flow, Local and AI modes, Guide scene, Game scene, card actions, turn/AP rules, tower building, enemy waves, route pressure, UI feedback, audio feedback, and Result scene.

## Future work / intentionally reduced features

- Broader online multiplayer validation.
- More regression and playtesting evidence.
- Further balance tuning for cards, AI behavior, tutorial pacing, and multiplayer clarity.
- Historical prototype ideas that are not present in the final Unity route should be treated as iteration evidence, not final feature claims.

## Legal / ethical / accessibility considerations

External assets and AI-assisted support are disclosed in `FinalGame/CanalTD/Assets/Docs/ResourceReferences.md`. The Guide scene, readable turn indicators, UI feedback, and audio feedback support accessibility and assessability.

## Tools, assets, and resources summary

- Unity version: `2022.3.62f3`
- Main Unity project: `FinalGame/CanalTD`
- Multiplayer package: Photon PUN 2
- UI text support: TextMeshPro
- Main visual asset pack: Tiny Swords
- Audio source platform: Pixabay
- AI-assisted support: selected coding suggestions, debugging suggestions, refactoring suggestions, documentation drafting, and generated card/card-slot visual assistance where disclosed

## Links to supporting documents

- Root README: [../README.md](../README.md)
- Testing log: [TESTING_LOG.md](./TESTING_LOG.md)
- Development log: [DEVELOPMENT_LOG.md](./DEVELOPMENT_LOG.md)
- Kanban and issue evidence: [KANBAN_AND_ISSUE_EVIDENCE.md](./KANBAN_AND_ISSUE_EVIDENCE.md)
- Submission checklist: [SUBMISSION_CHECKLIST.md](./SUBMISSION_CHECKLIST.md)
- Contribution and feedback notes: [CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md](./CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md)
- Unity system overview: [CanalTD/SYSTEM_OVERVIEW.md](./CanalTD/SYSTEM_OVERVIEW.md)
- Online status: [CanalTD/ONLINE_STATUS.md](./CanalTD/ONLINE_STATUS.md)
- Resource references: [CanalTD/Assets/Docs/ResourceReferences.md](./CanalTD/Assets/Docs/ResourceReferences.md)
