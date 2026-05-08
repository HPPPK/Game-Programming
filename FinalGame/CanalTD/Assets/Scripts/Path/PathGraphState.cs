/*
 * File: PathGraphState.cs
 *
 * Purpose:
 * This static class is a tiny global version counter for the path graph. It lets
 * path-related systems know when cached path results may be outdated because a
 * gate has opened or closed.
 *
 * Runtime behavior:
 * - Version starts at 0.
 * - MarkDirty() increments Version.
 * - Any system that caches path data can compare its cached version with the
 *   current Version.
 *
 * Dependency notes:
 * - GateFrameAnimation calls MarkDirty() when a gate changes blocking state.
 * - EnemyPathAssignmentManager clears cached paths when Version changes.
 */
public static class PathGraphState
{
    public static int Version = 0;

    public static void MarkDirty()
    {
        Version++;
    }
}
