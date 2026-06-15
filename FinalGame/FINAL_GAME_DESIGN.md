# CanalTD Final Game Design

This is the final assessed design summary for CanalTD. Historical prototype and rule-version documents remain as design-iteration evidence, but this file is the clean final design reference for marking.

## Game title

CanalTD

## One-sentence concept

CanalTD is a turn-based tower-defense and card-strategy game where players build towers, use tactical cards, manage enemy pressure, and compete for the strongest score.

## Intended player experience

The intended experience is a readable tactical match where each player makes turn-based decisions, manages limited resources, reacts to enemy waves, and uses indirect interaction instead of direct attacks.

## Core mechanic

The core mechanic is the combination of tower defense and card-based turn decisions: players defend against enemy waves, use cards to interfere with or support strategic positions, and compete through score changes.

## Player goal

Finish with the strongest score. Killing or helping kill enemies increases score, while castle damage reduces score, so players must balance defense, resources, and card timing.

## Main rules

- Players act through turn-based decisions.
- Cards provide disruption, targeting, or tactical effects.
- Towers defend against enemies that move through the map.
- Some card and map interactions can affect route pressure and enemy movement.
- Player resources limit building, purchasing, and tactical decisions.
- Results are shown through the final result flow.

## Turn structure

Turns are managed through card action limits and turn restrictions rather than an AP system. In each player turn, the card flow is limited to drawing, playing, and discarding through per-turn flags, while Disrupt blocks card actions for the affected player's turn. The wider loop also includes selecting targets, building or upgrading towers, interacting with gates/paths, and ending the turn.

## Card interaction

Cards are selected from the player hand, then played, discarded, or used through targeting flows. Targeting interactions include confirm/cancel behavior and should respect turn, ownership, card action, and Disrupt restrictions.

## Tower building and defence

Players use tower build areas and resource checks to buy land, build towers, upgrade towers, sell towers, and defend against enemy waves. Tower actions are part of the main Local/AI assessment route.

## Enemy routing / canal pressure

Enemies use route and path systems rather than a single fixed lane. Canal and gate control can affect reachable paths, destination pressure, and how enemy waves threaten players.

## Local mode

Local Mode is one of the primary stable assessment routes. It supports the core vertical slice through scene flow, turn structure, cards, towers, enemies, UI feedback, and results.

## AI mode

AI Mode is one of the primary stable assessment routes. It provides AI-controlled opponents with multiple difficulty settings so the game can be assessed without requiring four human players.

## Online mode status

Online Mode is implemented using Photon room-code multiplayer. It supports create/join room flow, player slots, ready/start flow, synchronized turns, build actions, card actions, gate actions, enemy waves, combat outcomes, result flow, and player-left cleanup when clients use compatible Photon settings and builds. If a remote player leaves, their cards are returned to the authoritative deck by the Master Client, their land/towers/traps are cleared locally, and the synchronized build snapshot is cleared so removed towers do not reappear after a wave or room-property refresh. The root README records the final online scope and known limitations, including the current 20 CCU Photon development/prototype plan.

## Exit and player-left behavior

Local Mode and AI Mode treat the Exit button as ending the shared local match and moving to `ResultScene`. This is different from Online Mode, where one remote client may leave while the remaining clients continue the match. Online player-left cleanup therefore has extra responsibility for cards, land, towers, traps, turn state, and synchronized snapshots.

## Win / lose / completion condition

The match completion route leads to `ResultScene`, where ranking/result information is displayed. The main result flow has been manually verified through the testing log. Additional rare result-state edge cases can be expanded through future regression testing.

## Feedback and UI

The project includes current-turn indicators, player status panels, targeting feedback, warnings/toasts, guide/onboarding flow, audio feedback, and result UI to make the game easier to understand and assess.

## Scope control: what is included in the vertical slice

The assessed vertical slice includes the Bootstrap/Home/ModeSelect flow, Local, AI, and Online modes, Guide scene, Game scene, card actions, turn/card-action rules, tower building, enemy waves, route pressure, UI feedback, audio feedback, and Result scene.

## Future work / intentionally reduced features

- Automated multiplayer regression tests and longer soak testing.
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
- UI font: Medieval Sharp from DaFont
- Main visual asset pack: Tiny Swords
- Audio source platform: Pixabay
- AI-assisted support: selected coding suggestions, debugging suggestions, refactoring suggestions, documentation drafting, and generated card/card-slot visual assistance where disclosed

## Links to supporting documents

- Root README: [../README.md](../README.md)
- Testing log: [TESTING_LOG.md](./TESTING_LOG.md)
- Development log: [DEVELOPMENT_LOG.md](./DEVELOPMENT_LOG.md)
- Contribution and feedback notes: [CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md](./CanalTD/CONTRIBUTIONS_AND_FEEDBACK.md)
- Resource references: [CanalTD/Assets/Docs/ResourceReferences.md](./CanalTD/Assets/Docs/ResourceReferences.md)
