using System.Collections.Generic;
using DynamicPhysics.Abilities;
using DynamicPhysics.Constraints;
using DynamicPhysics.Pipeline.Modifiers;
using DynamicPhysics.Profiles;

namespace DynamicPhysics.Orchestration
{
    /**
     * <summary>
     * The fully resolved configuration for a single fixed tick of the motion pipeline.
     * Built from a <see cref="MovementProfile"/> base with optional runtime overrides
     * applied through the <see cref="MovementProfileBuilder"/>.
     * </summary>
     *
     * <remarks>
     * This object is allocated once and reused each frame. The orchestrator refreshes its
     * contents before each pipeline execution. It references the currently active modifiers,
     * constraints, and abilities without owning them.
     * </remarks>
     */
    public class RuntimeMotionConfig
    {
        /** <summary>Resolved steering parameters for this tick.</summary> */
        public SteeringSettings Steering;

        /** <summary>Base gravity scale before modifiers.</summary> */
        public float GravityScale;

        /** <summary>Ground friction coefficient.</summary> */
        public float Friction;

        /** <summary>Air control multiplier (0-1).</summary> */
        public float AirControl;

        /** <summary>Active modifiers for this tick, sorted by Order. Pre-allocated list.</summary> */
        public readonly List<IMotionModifier> ActiveModifiers = new(16);

        /** <summary>Active constraints for this tick, sorted by Priority. Pre-allocated list.</summary> */
        public readonly List<IMotionConstraint> ActiveConstraints = new(8);

        /** <summary>Active abilities for this tick. Pre-allocated list.</summary> */
        public readonly List<IMotionAbility> ActiveAbilities = new(8);

        /**
         * <summary>
         * Populates this config from a <see cref="MovementProfile"/> baseline.
         * Clears all dynamic collections.
         * </summary>
         *
         * <param name="profile">The profile to load baseline values from.</param>
         */
        public void LoadFromProfile(MovementProfile profile)
        {
            Steering = profile.Steering;
            GravityScale = profile.GravityScale;
            Friction = profile.Friction;
            AirControl = profile.AirControl;

            ActiveModifiers.Clear();
            ActiveConstraints.Clear();
            ActiveAbilities.Clear();
        }

        /**
         * <summary>
         * Clears all resolved state. Called before rebuilding for the next tick.
         * </summary>
         */
        public void Clear()
        {
            ActiveModifiers.Clear();
            ActiveConstraints.Clear();
            ActiveAbilities.Clear();
        }
    }
}
