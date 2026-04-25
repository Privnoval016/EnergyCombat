namespace DynamicPhysics
{
    /**
     * <summary>
     * Defines the execution priority ordering for pipeline stages.
     * Lower values execute first. This ordering ensures that weaker influences
     * (input) are applied first and stronger influences (constraints) apply last.
     * </summary>
     *
     * <remarks>
     * Conflict resolution follows: Input (weakest) → Abilities → External Forces → Gravity → Modifiers → Constraints (strongest).
     * Constraints always dominate because they enforce physical invariants.
     * </remarks>
     */
    public static class InfluencePriority
    {
        /** <summary>Input steering: weakest influence, applied first so everything else can shape it.</summary> */
        public const int InputSteering = 100;

        /** <summary>Ability velocity contributions (jump impulse, dash override, wall run).</summary> */
        public const int AbilityInfluence = 200;

        /** <summary>External forces (knockback, wind, explosions).</summary> */
        public const int ExternalForce = 300;

        /** <summary>Gravity application with scaling.</summary> */
        public const int Gravity = 400;

        /** <summary>Modifier transformations (drag, speed cap, steering override).</summary> */
        public const int Modifiers = 500;

        /** <summary>Hard constraints: strongest influence, always executes last.</summary> */
        public const int Constraints = 600;
    }
}
