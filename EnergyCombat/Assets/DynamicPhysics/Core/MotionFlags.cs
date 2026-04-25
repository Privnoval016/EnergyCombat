namespace DynamicPhysics
{
    /**
     * <summary>
     * Provides built-in string constants for common motion tags.
     * Tags replace a hardcoded flags enum, allowing any system to define custom tags
     * without modifying core code.
     * </summary>
     *
     * <remarks>
     * These are convenience constants for common states. Users can define and use
     * any arbitrary string tag: <c>context.SetTag("MyCustomState")</c>.
     * Tags are stored in a <see cref="System.Collections.Generic.HashSet{T}"/> on
     * the <see cref="MotionContext"/> for O(1) lookups.
     * </remarks>
     */
    public static class MotionTag
    {
        /** <summary>Character is on a walkable surface.</summary> */
        public const string Grounded = "Grounded";

        /** <summary>Character is not grounded.</summary> */
        public const string Airborne = "Airborne";

        /** <summary>Character is on a slope exceeding the maximum walkable angle.</summary> */
        public const string Sliding = "Sliding";

        /** <summary>Character is in contact with a wall.</summary> */
        public const string WallContact = "WallContact";

        /** <summary>Character is attached to a rope and swinging.</summary> */
        public const string Swinging = "Swinging";

        /** <summary>Character is actively dashing.</summary> */
        public const string Dashing = "Dashing";

        /** <summary>Character is in combat movement mode.</summary> */
        public const string InCombat = "InCombat";

        /** <summary>Character is stunned and input should be suppressed.</summary> */
        public const string Stunned = "Stunned";

        /** <summary>Character is performing a slide.</summary> */
        public const string SlidingCrouch = "SlidingCrouch";

        /** <summary>Character is wall running.</summary> */
        public const string WallRunning = "WallRunning";
    }
}
