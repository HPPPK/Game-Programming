/*
 * File: GateActionType.cs
 *
 * Purpose:
 * This enum lists the gate actions that can be requested by card gameplay.
 * It gives CardDrawManager, GateTargetingManager, and GateFrameAnimation a shared
 * vocabulary so they can agree on what kind of gate operation is currently being
 * performed.
 *
 * Values:
 * - None: no gate action is active.
 * - OpenGate: the player wants to open an existing blocking gate.
 * - LockGate: the player wants to close/build/lock a gate, depending on state.
 *
 * Dependency notes:
 * - CardDrawManager converts card prefab names into GateActionType values.
 * - GateTargetingManager uses this enum to filter valid target gates.
 */
public enum GateActionType
{
    None,
    OpenGate,
    LockGate
}
