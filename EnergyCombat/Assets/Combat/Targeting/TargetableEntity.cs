using System.Collections.Generic;
using UnityEngine;

namespace Combat.Targeting
{
    /**
     * <summary>
     * Optional MonoBehaviour that groups multiple <see cref="TargetCandidate"/> components
     * on a single logical entity (e.g. a boss with targetable weak spots and limbs).
     * Attach to the root GameObject of the enemy and assign child <see cref="TargetCandidate"/>
     * components in order of priority. The first entry is the primary target.
     * </summary>
     *
     * <remarks>
     * Simple enemies with a single target point do not need this component —
     * a standalone <see cref="TargetCandidate"/> is sufficient.
     * Override <see cref="IsAlive"/> in a subclass to tie entity death to a
     * <c>HealthComponent</c> without modifying this base class.
     * </remarks>
     */
    [AddComponentMenu("Combat/Targeting/Targetable Entity")]
    public class TargetableEntity : MonoBehaviour, ITargetableEntity
    {
        [SerializeField] private string _displayName;

        /**
         * <summary>
         * All target points on this entity, ordered by priority.
         * Index 0 is used as <see cref="PrimaryTarget"/>.
         * </summary>
         */
        [Tooltip("All target candidates on this entity. Index 0 = primary target.")]
        [SerializeField] private List<TargetCandidate> _targetPoints = new();

        /** <inheritdoc /> */
        public ITargetable PrimaryTarget => _targetPoints.Count > 0 ? _targetPoints[0] : null;

        /** <inheritdoc /> */
        public IReadOnlyList<ITargetable> AllTargetPoints => _targetPoints;

        /**
         * <inheritdoc />
         * Override in a subclass to return <c>false</c> when a <c>HealthComponent</c>
         * reaches zero — the base implementation returns <c>true</c> while the
         * GameObject is active.
         */
        public virtual bool IsAlive => gameObject.activeInHierarchy;

        /** <inheritdoc /> */
        public string DisplayName => _displayName;
    }
}
