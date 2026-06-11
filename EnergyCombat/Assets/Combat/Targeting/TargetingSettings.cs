using UnityEngine;

namespace Combat.Targeting
{
    /**
     * <summary>
     * ScriptableObject configuration for <see cref="SoftTargetingSystem"/>.
     * Create via <c>Assets → Create → Combat → Targeting → Settings</c>.
     * </summary>
     */
    [CreateAssetMenu(menuName = "Combat/Targeting/Settings", fileName = "TargetingSettings")]
    public class TargetingSettings : ScriptableObject
    {
        /**
         * <summary>
         * The scoring strategy used to rank targeting candidates.
         * Assign a <see cref="DefaultTargetingScorer"/> or any custom <see cref="TargetingScorer"/> asset.
         * </summary>
         */
        [Tooltip("Scorer asset that ranks targeting candidates. Assign a DefaultTargetingScorer or a custom subclass.")]
        public TargetingScorer Scorer;

        /**
         * <summary>
         * Time in seconds between full targeting re-evaluations.
         * Lower values are more responsive but more expensive. 0.1 is a good default.
         * </summary>
         */
        [Tooltip("Seconds between targeting re-evaluations. Lower = more responsive.")]
        public float UpdateInterval = 0.1f;

        /**
         * <summary>
         * When <c>true</c>, the current target is locked for the duration of an executing
         * attack ability so it cannot switch mid-strike.
         * </summary>
         */
        [Tooltip("Prevent target switching while an attack ability is executing.")]
        public bool LockTargetDuringAttacks = true;
    }
}
