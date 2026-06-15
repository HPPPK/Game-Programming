# CanalTD Runtime Logic Cleanup Audit

## Scope

This audit records the conservative runtime cleanup pass for the final CanalTD project. The pass focuses on AP legacy cleanup and Disrupt card behaviour without changing the Local, AI, or Online playable routes.

No core architecture rewrite is included in this pass.

## Low-risk changes completed

| Area | Action taken | Risk control |
|---|---|---|
| Turn rules | `TurnManager` now treats card actions as the final rule source: draw, play, and discard are controlled by per-turn flags and Disrupt state. | Public fields and method names were kept to avoid Unity reference or compile breakage. |
| AP compatibility | `maxAP`, `currentAP`, `HasEnoughAP(int)`, and `ApplyAuthoritativePlayConsumed(int)` remain for older callers, but AP no longer blocks or consumes card play. | Existing callers can still compile while final card rules no longer depend on AP. |
| Disrupt | Disrupt blocks draw, play, and discard through `cardActionsBlockedThisTurn`. | Build, gate viewing/interaction, UI navigation, and end turn are not intentionally blocked by Disrupt. |
| AP UI | `APDisplayUI` now hides/clears legacy AP text if the component is still present in a scene. | Script and scene references are preserved; no Unity scene deletion is required. |
| Comments/debug wording | AP-facing comments and the online card-state debug line were updated to refer to card action state instead of AP. | Runtime behaviour is not changed outside the AP/card action rule path. |

## Current card action rule

- A player can draw at most once per turn.
- A player can play at most once per turn.
- A player can discard at most once per turn.
- If Disrupt is active for that player's current turn, draw/play/discard are blocked for that turn.
- Disrupt is consumed when that player's disrupted turn begins, so it should not persist forever.

## Duplicate or overlapping logic observed

| Area | Observation | Recommendation |
|---|---|---|
| Turn/player/phase state | `TurnManager`, `GamePhaseManager`, `AIPrototypeTurnManager`, and `PhotonOnlineGameSceneManager` each maintain some turn or phase state. | Do not merge before submission. Keep current routes stable and only fix direct bugs. |
| Local/AI/Online card effects | Card effects are split between local targeting managers, AI helpers, and `PhotonOnlineCardSyncManager`. | Avoid large abstraction now. Use shared permission methods such as `CanDrawCard`, `CanPlayCard`, and `CanDiscardCard` where possible. |
| Wave/result ranking | Wave completion, score updates, and result data appear in several systems. | Leave as-is unless a concrete scoring/result bug appears. |
| Targeting managers | Player, tile, gate, tower, and trap targeting managers repeat confirm/cancel/highlight/toast patterns. | A shared targeting base could help later, but it is too risky for final cleanup. |
| Toast/message helpers | Several scripts use local reflection-based or direct toast helpers. | Keep current helpers unless a specific message route fails. |
| PlayerPrefs mode state | Mode/session data is stored and read in multiple places. | Do not replace before submission; document the expected scene flow instead. |
| Guide/Tutorial direct state changes | Tutorial code directly prepares scene state, cards, towers, and wave setup. | Keep tutorial-specific state control isolated to GuideScene; do not merge into core systems now. |

## High-risk changes intentionally not done

- Merging `AIPrototypeTurnManager` with `GamePhaseManager`.
- Rewriting online authoritative state architecture.
- Abstracting all targeting managers into a shared base class.
- Replacing PlayerPrefs session state with a new runtime session service.
- Rewriting GuideScene tutorial text or tutorial control flow.
- Renaming C# scripts/classes.
- Deleting Unity scene objects or assets.

## Manual validation checklist

- Local mode: draw/play/discard limits still work.
- Local mode: Disrupt blocks target player's draw/play/discard on that player's next turn.
- Local mode: Disrupt does not block build tower, view UI, or end turn.
- AI mode: AI and human turns continue after Disrupt.
- Online mode: synchronized Disrupt blocks card actions on the target client's disrupted turn.
- Turn change: Disrupt is consumed after one affected turn.
- UI: no visible AP counter appears as a final rule indicator.
