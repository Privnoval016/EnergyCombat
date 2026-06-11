using UnityEngine;

namespace Combat.Targeting
{
    /**
     * <summary>
     * MonoBehaviour that marks any GameObject as a targetable point.
     * Attach this to an enemy, prop, or test object to register it with
     * <see cref="TargetingRegistry"/> automatically.
     * </summary>
     *
     * <remarks>
     * Registration is driven by Unity's enable/disable lifecycle so pooled
     * or dynamically spawned objects are handled correctly without manual wiring.
     * The <see cref="ParentEntity"/> field is resolved at Awake from the nearest
     * <see cref="ITargetableEntity"/> ancestor — for simple objects it will be null.
     * </remarks>
     */
    [AddComponentMenu("Combat/Targeting/Target Candidate")]
    public class TargetCandidate : MonoBehaviour, ITargetable
    {
        /** <summary>World-space offset from the transform origin to the aim point (e.g. chest height).</summary> */
        [Tooltip("Offset from the object's pivot to the targeting aim point.")]
        [SerializeField] private Vector3 _offset = Vector3.up * 0.5f;

        /** <summary>Initial targetable state. Can be toggled at runtime via <see cref="SetTargetable"/>.</summary> */
        [Tooltip("Whether this object starts as targetable.")]
        [SerializeField] private bool _isTargetable = true;

        private ITargetableEntity _parentEntity;

        #region ITargetable

        /** <inheritdoc /> */
        public Vector3 TargetPosition => transform.position + _offset;

        /** <inheritdoc /> */
        public Transform TargetTransform => transform;

        /** <inheritdoc /> */
        public bool IsTargetable => _isTargetable && gameObject.activeInHierarchy;

        /** <inheritdoc /> */
        public ITargetableEntity ParentEntity => _parentEntity;

        #endregion

        private void Awake()
        {
            _parentEntity = GetComponentInParent<ITargetableEntity>();
        }

        private void OnEnable()  => TargetingRegistry.Register(this);
        private void OnDisable() => TargetingRegistry.Unregister(this);

        /**
         * <summary>
         * Toggles targetability at runtime. Use this for invulnerability windows,
         * death sequences, or phase transitions on bosses.
         * </summary>
         */
        public void SetTargetable(bool value) => _isTargetable = value;
    }
}
