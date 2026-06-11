using UnityEngine;

namespace Combat.Targeting
{
    /**
     * <summary>
     * Default angle-weighted targeting scorer inspired by Batman: Arkham.
     * Prefers the enemy most directly in front of the player rather than the nearest one,
     * matching the feel of FreeFlow combat where attacks always aim forward.
     * </summary>
     *
     * <remarks>
     * Score formula:
     * <code>
     *   angleFactor = 1 - (angle / MaxAngleDegrees)   // 1.0 = perfectly ahead
     *   distFactor  = 1 - (dist  / MaxRange)           // 1.0 = at player position
     *   score       = angleFactor * AngleWeight + distFactor * DistanceWeight
     *               + (isCurrent ? StickinessBias : 0)
     * </code>
     * Candidates outside the cone or beyond <see cref="MaxRange"/> return -1 and are excluded.
     * </remarks>
     */
    [CreateAssetMenu(menuName = "Combat/Targeting/Default Scorer", fileName = "DefaultTargetingScorer")]
    public class DefaultTargetingScorer : TargetingScorer
    {
        /** <summary>Maximum distance at which a target can be selected.</summary> */
        [Tooltip("Maximum targeting range in world units.")]
        public float MaxRange = 15f;

        /** <summary>Targeting cone: candidates outside this angle from player forward are excluded.</summary> */
        [Tooltip("Maximum targeting cone half-angle in degrees.")]
        public float MaxAngleDegrees = 120f;

        /**
         * <summary>
         * Weight given to the angle component.
         * Higher values favour targets directly in front over nearby off-angle targets.
         * </summary>
         */
        [Tooltip("Contribution of facing angle to the final score (0–1). Increase to prefer targets more directly ahead.")]
        [Range(0f, 1f)]
        public float AngleWeight = 0.7f;

        /**
         * <summary>
         * Weight given to the distance component.
         * Higher values favour closer targets, lowering the importance of facing angle.
         * </summary>
         */
        [Tooltip("Contribution of distance to the final score (0–1). Increase to prefer closer targets.")]
        [Range(0f, 1f)]
        public float DistanceWeight = 0.3f;

        /**
         * <summary>
         * Extra score added to the candidate that is already selected.
         * Prevents the target from flickering when two candidates score similarly.
         * </summary>
         */
        [Tooltip("Score bonus added to the current target to prevent rapid switching near scoring ties.")]
        [Range(0f, 1f)]
        public float StickinessBias = 0.2f;

        /** <inheritdoc /> */
        public override float Score(
            ITargetable candidate,
            Vector3 playerPos,
            Vector3 playerForward,
            ITargetable currentTarget)
        {
            if (!candidate.IsTargetable) return -1f;

            Vector3 toCandidate = candidate.TargetPosition - playerPos;
            float dist = toCandidate.magnitude;

            if (dist > MaxRange || dist < 0.01f) return -1f;

            float angle = Vector3.Angle(playerForward, toCandidate / dist);
            if (angle > MaxAngleDegrees) return -1f;

            float angleFactor = 1f - angle / MaxAngleDegrees;
            float distFactor  = 1f - dist  / MaxRange;

            float score = angleFactor * AngleWeight + distFactor * DistanceWeight;
            if (ReferenceEquals(candidate, currentTarget)) score += StickinessBias;
            return score;
        }

        /** <inheritdoc /> */
        public override bool IsInRange(ITargetable candidate, Vector3 playerPos)
        {
            return Vector3.Distance(candidate.TargetPosition, playerPos) <= MaxRange;
        }
    }
}
