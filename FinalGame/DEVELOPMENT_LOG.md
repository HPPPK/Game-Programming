# CanalTD Development Log

## Overview

This log summarises the main development milestones for the CanalTD project and provides an assessable record of progress.

## Early concept phase

- Defined the core idea: indirect competition through canal-flow and gate control rather than direct attacks.
- Chose a turn-based structure so players could make readable strategic decisions.
- Combined cards, tower defense, and shared map control into a single ruleset.
- Built the first visual prototype and documented the concept in [prototype.md](./prototype.md).

## Core gameplay foundation

- Built the player turn structure for draw, play, build, and map interaction.
- Added card-based interaction as the main player-versus-player disruption layer.
- Implemented towers, enemy damage, castle health, and result tracking.
- Added wave spawning and enemy pressure as the main challenge loop.

## Path and routing systems

- Implemented path graph structures, pathfinding utilities, and gate-aware route changes.
- Developed enemy path assignment so waves can be distributed across reachable castles.
- Added route recalculation when gates or ownership states change.
- Tuned the system so enemy pressure remains readable and strategically meaningful.

## AI mode development

- Added AI-driven turn control and multiple strategy layers.
- Designed three AI difficulty levels with increasing decision quality and pressure.
- Tuned AI card usage, target selection, and tactical behaviour to better match player expectations.

## UX and clarity improvements

- Improved turn readability with player turn indication and turn markers.
- Added feedback through status panels, current turn UI, toasts, and result displays.
- Built a dedicated Guide scene to improve onboarding and step-by-step user understanding.
- Continued UI cleanup, layout adjustment, and scene polish for better readability.

## Multiplayer and online sync

- Added room flow for online mode with Photon.
- Implemented authoritative online sync for core build, turn, and card-hand state.
- Added private hand visibility rules and public hand-count display for multiplayer fairness.
- Continued debugging connection, lifecycle, and card-sync regressions during integration.

## Documentation and assessment preparation

- Added README content to explain how the game runs and how it should be assessed.
- Standardised script header comments to improve assessability.
- Added contribution notes, feedback response notes, and placeholder reference sections.

## Current focus

- Stabilise the online experience.
- Finish documentation and contribution evidence.
- Keep improving clarity, testing, and final demo readiness.
