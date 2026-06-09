using Combat;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Provides built-in <see cref="Tag"/> constants for common motion states.
     * Tags replace a hardcoded flags enum, allowing any system to define custom tags
     * without modifying core code.
     * </summary>
     *
     * <remarks>
     * These are convenience constants for common states. Custom tags can be defined
     * in any static class: <c>public static readonly Tag MyTag = new Tag("MyTag");</c>.
     * Tags are stored in a <see cref="System.Collections.Generic.HashSet{T}"/> on
     * the <see cref="MotionContext"/> for O(1) lookups.
     * </remarks>
     */
    public static class MotionTag
    {
        /** <summary>Character is on a walkable surface.</summary> */
        public static readonly Tag Grounded = new Tag("Grounded");

        /** <summary>Character is not grounded.</summary> */
        public static readonly Tag Airborne = new Tag("Airborne");

        /** <summary>Character is currently sprinting.</summary> */
        public static readonly Tag Sprinting = new Tag("Sprinting");

        /** <summary>Character is on a slope exceeding the maximum walkable angle.</summary> */
        public static readonly Tag Sliding = new Tag("Sliding");

        /** <summary>Character is in contact with a wall.</summary> */
        public static readonly Tag WallContact = new Tag("WallContact");

        /** <summary>Character is attached to a rope and swinging.</summary> */
        public static readonly Tag Swinging = new Tag("Swinging");

        /** <summary>Character is actively dashing.</summary> */
        public static readonly Tag Dashing = new Tag("Dashing");

        /** <summary>Character is in combat movement mode.</summary> */
        public static readonly Tag InCombat = new Tag("InCombat");

        /** <summary>Character is stunned and input should be suppressed.</summary> */
        public static readonly Tag Stunned = new Tag("Stunned");

        /** <summary>Character is performing a sliding crouch.</summary> */
        public static readonly Tag SlidingCrouch = new Tag("SlidingCrouch");

        /** <summary>Character is wall running.</summary> */
        public static readonly Tag WallRunning = new Tag("WallRunning");

        /** <summary>Disables automatic rotation based on input direction.</summary> */
        public static readonly Tag NoAutoRotate = new Tag("NoAutoRotate");

        /** <summary>Character is grabbing a ledge and auto-climbing up.</summary> */
        public static readonly Tag LedgeGrabbing = new Tag("LedgeGrabbing");

        /** <summary>Character just performed a wall kick (active for one physics tick).</summary> */
        public static readonly Tag WallKicking = new Tag("WallKicking");

        /**
         * <summary>
         * Character is executing or has recently completed a hard-stop momentum reversal.
         * The tag is held for a configurable duration after the physics condition clears
         * so the animation system can play the full turn animation.
         * See <see cref="SteeringSettings.QuickTurnHoldDuration"/>.
         * </summary>
         */
        public static readonly Tag QuickTurning = new Tag("QuickTurning");

        /**
         * <summary>
         * Character is playing a walk-start enter animation. Directional steering acceleration
         * is suppressed (contextual control = 0) so the character stays stationary until the
         * enter clip finishes and the loop begins.
         * </summary>
         */
        public static readonly Tag WalkStarting = new Tag("WalkStarting");

        /**
         * <summary>
         * Set by <see cref="WallRunAbility"/> for one physics tick when a Jump request ends the
         * wall run. <see cref="WallKickAbility"/> reads and clears this in <c>CanActivate</c> so
         * it can select the correct wall-run-exit animation instead of the standalone-kick clip.
         * </summary>
         */
        public static readonly Tag WallRunJump = new Tag("WallRunJump");
    }
}
