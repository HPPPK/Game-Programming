# CanalTD Online Mode Status

## Summary

Online Mode is implemented as an extension for CanalTD. It demonstrates additional networking ambition and process evidence, but Local Mode and AI Mode remain the primary stable assessment routes unless broader online validation is completed separately.

## Implemented online work

- Photon-based room and lobby flow.
- Player slot and ready-state handling.
- Online scene transition work.
- Online turn authority and player lifecycle work.
- Online card-hand and build synchronization support.
- Private hand / public hand-count handling for multiplayer fairness.

Related student-authored scripts are under `Assets/Scripts/Networking`.

## What was intentionally scoped

Online Mode is not presented as the core marking route. It is retained as extension evidence because networked multiplayer has more edge cases than the Local/AI vertical slice.

## Primary assessment route

`BootstrapScene` -> `HomeScene` -> `ModeSelectScene` -> Local / AI mode -> `GameScene` -> `ResultScene`

Local Mode and AI Mode are the primary assessment routes.

## Known online limitations

- Broader multi-device testing is required.
- Late-match and disconnection edge cases require more validation.
- Online card/build synchronization requires manual regression testing before claiming full stability.
- Room joining should be checked with both clients using the same Photon AppId, fixed region, and game version.
- Unity Editor compilation and runtime behavior must be manually verified.

## Related closed issues

- [Game Mode Selection & Online/AI Room System #62](https://github.com/HPPPK/Game-Programming/issues/62)
- [Online Match Start Does Not Synchronize Scene Transition #89](https://github.com/HPPPK/Game-Programming/issues/89)
- [Online Turn Authority & Player Lifecycle Goal #91](https://github.com/HPPPK/Game-Programming/issues/91)

Closed issues are completed development tasks and process evidence. They do not by themselves prove every runtime edge case has passed validation.

## Future work

- Complete broader online multiplayer regression testing.
- Record manual online test outcomes in `FinalGame/TESTING_LOG.md`.
- Keep open issues only for real unresolved limitations rather than reopening completed tasks.
