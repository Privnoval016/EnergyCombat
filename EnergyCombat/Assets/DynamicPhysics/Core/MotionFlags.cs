using System;

namespace DynamicPhysics.Core
{
    /**
     * <summary>
     * Bitfield flags representing the current contextual state of a character's motion.
     * Multiple flags can be active simultaneously, enabling complex state queries
     * without branching or string comparisons.
     * </summary>
     *
     * <remarks>
     * Flags are set by the orchestrator and various stages during pipeline execution.
     * Stages read flags to conditionally adjust their behavior (e.g., InputSteeringStage
     * reduces control when <see cref="Dashing"/> is active).
     * </remarks>
     */
    [Flags]
    public enum MotionFlags
    {
        /** <summary>No flags active.</summary> */
        None = 0,

        /** <summary>Character is on a walkable surface.</summary> */
        Grounded = 1 << 0,

        /** <summary>Character is not grounded.</summary> */
        Airborne = 1 << 1,

        /** <summary>Character is on a slope exceeding the maximum walkable angle.</summary> */
        Sliding = 1 << 2,

        /** <summary>Character is in contact with a wall (wall running or touching).</summary> */
        WallContact = 1 << 3,

        /** <summary>Character is attached to a rope and swinging.</summary> */
        Swinging = 1 << 4,

        /** <summary>Character is actively dashing.</summary> */
        Dashing = 1 << 5,

        /** <summary>Character is in combat movement mode.</summary> */
        InCombat = 1 << 6,

        /** <summary>Character is stunned and input should be suppressed.</summary> */
        Stunned = 1 << 7,

        /** <summary>Character is performing a slide.</summary> */
        SlidingCrouch = 1 << 8,

        /** <summary>Character is wall running.</summary> */
        WallRunning = 1 << 9
    }
}
