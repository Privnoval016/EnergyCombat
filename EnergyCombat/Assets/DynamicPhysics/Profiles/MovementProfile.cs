using UnityEngine;

namespace DynamicPhysics.Profiles
{
    /**
     * <summary>
     * ScriptableObject that defines baseline movement tuning for a movement mode.
     * Contains all data-driven parameters that control movement feel without any runtime state.
     * </summary>
     *
     * <remarks>
     * Profiles are purely data containers — they must never store runtime state.
     * Runtime state lives in <see cref="Core.MotionContext"/> and
     * <see cref="Orchestration.RuntimeMotionConfig"/>.
     * Swap profiles to change movement modes (ground, air, combat, etc.) or layer
     * overrides via <see cref="Orchestration.MovementProfileBuilder"/>.
     * </remarks>
     */
    [CreateAssetMenu(fileName = "New Movement Profile", menuName = "DynamicPhysics/Movement Profile")]
    public class MovementProfile : ScriptableObject
    {
        #region Steering

        /** <summary>Steering configuration controlling acceleration, speed, and turn behavior.</summary> */
        [Header("Steering")]
        public SteeringSettings Steering = SteeringSettings.Default;

        #endregion

        #region Physics

        /** <summary>Gravity multiplier for this movement mode. 1.0 = normal gravity.</summary> */
        [Header("Physics")]
        [Tooltip("Gravity scale multiplier. 1 = normal, 0 = no gravity.")]
        public float GravityScale = 1f;

        /** <summary>Ground friction coefficient applied during deceleration.</summary> */
        [Tooltip("Friction applied when decelerating on ground.")]
        public float Friction = 8f;

        /**
         * <summary>
         * Multiplier on steering responsiveness while airborne.
         * 0 = no air control, 1 = full ground-level control.
         * </summary>
         */
        [Tooltip("Air control factor. 0 = no air control, 1 = full.")]
        [Range(0f, 1f)]
        public float AirControl = 0.4f;

        #endregion
    }
}
