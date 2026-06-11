using UnityEngine;

namespace Combat.Targeting
{
    /**
     * <summary>
     * Abstract ScriptableObject base class for targeting scorer strategies.
     * Subclass this and decorate with <c>[CreateAssetMenu]</c> to create configurable
     * scorer assets assignable from the Inspector without modifying any runtime code.
     * </summary>
     *
     * <remarks>
     * The Strategy pattern here means <see cref="SoftTargetingSystem"/> never has to change
     * when new targeting feels are needed — just create a new subclass and swap the asset.
     * </remarks>
     */
    public abstract class TargetingScorer : ScriptableObject, ITargetingScorer
    {
        /** <inheritdoc /> */
        public abstract float Score(
            ITargetable candidate,
            Vector3 playerPos,
            Vector3 playerForward,
            ITargetable currentTarget);

        /** <inheritdoc /> */
        public abstract bool IsInRange(ITargetable candidate, Vector3 playerPos);
    }
}
