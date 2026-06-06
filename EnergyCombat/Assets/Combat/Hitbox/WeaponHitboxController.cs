using System;
using System.Collections.Generic;
using Extensions.EventBus;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Concrete <see cref="IHitboxController"/> that sweeps a physics overlap shape
     * around a weapon bone each <c>FixedUpdate</c> while active.
     * </summary>
     *
     * <remarks>
     * Attach this to the weapon bone <c>Transform</c> in the character prefab.
     * Set <see cref="HitboxId"/> to a unique name (e.g. "Sword", "LeftFist") so that
     * <see cref="ActivePhase"/> can look it up by name via
     * <c>CombatController.GetHitboxController(id)</c>.
     *
     * The controller is auto-discovered at runtime — no manual wiring required as long
     * as it lives anywhere in the character's GameObject hierarchy.
     *
     * Hit deduplication is per <see cref="CombatContext"/> — each activation tracks
     * its own hit set so multi-swing combos don't carry over hits from the previous swing.
     * </remarks>
     */
    [AddComponentMenu("Combat/Weapon Hitbox Controller")]
    public class WeaponHitboxController : MonoBehaviour, IHitboxController
    {
        private static readonly Collider[] OverlapBuffer = new Collider[32];

        /** <summary>Unique identifier for this hitbox. Must match the HitboxId in ActivePhase.Hitboxes.</summary> */
        [Header("Identity")]
        [Tooltip("Unique name used by ActivePhase to look up this controller. Match HitboxActivation.HitboxId.")]
        [SerializeField] private string _hitboxId = "Default";

        private HitboxConfig _config;
        private CombatContext _context;
        private readonly HashSet<GameObject> _hitTargets = new();

        /** <inheritdoc /> */
        public string HitboxId => _hitboxId;

        /** <inheritdoc /> */
        public bool IsActive { get; private set; }

        /** <inheritdoc /> */
        public event Action<HitData> OnHit;

        /** <inheritdoc /> */
        public void Activate(HitboxConfig config, CombatContext context)
        {
            _config = config;
            _context = context;
            _hitTargets.Clear();
            IsActive = true;
        }

        /** <inheritdoc /> */
        public void Deactivate()
        {
            IsActive = false;
            _config = null;
            _context = null;
        }

        private void FixedUpdate()
        {
            if (!IsActive || _config == null) return;
            PerformOverlap();
        }

        private void PerformOverlap()
        {
            int count = QueryOverlap();

            for (int i = 0; i < count; i++)
            {
                Collider col = OverlapBuffer[i];
                if (col == null) continue;

                GameObject go = col.gameObject;
                if (go == gameObject) continue;
                if (_hitTargets.Contains(go)) continue;
                if (_config.MaxHitsPerSwing > 0 && _hitTargets.Count >= _config.MaxHitsPerSwing) break;

                ICombatTarget target = go.GetComponent<ICombatTarget>();
                IHittable hittable = go.GetComponent<IHittable>();

                if (hittable == null) continue;
                if (target != null && !_config.TargetFilter.Matches(target.TagContainer)) continue;

                _hitTargets.Add(go);

                HitData hitData = BuildHitData(col, go, target);
                hittable.ReceiveHit(hitData);

                OnHit?.Invoke(hitData);
                EventBus<HitEvent>.Raise(new HitEvent { Hit = hitData, Context = _context });
            }
        }

        private int QueryOverlap()
        {
            Vector3 origin = transform.TransformPoint(_config.Offset);

            switch (_config.Shape)
            {
                case HitboxShape.Sphere:
                    return Physics.OverlapSphereNonAlloc(origin, _config.Radius, OverlapBuffer, _config.TargetLayers);

                case HitboxShape.Capsule:
                    Vector3 point1 = transform.TransformPoint(_config.Offset);
                    Vector3 point2 = transform.TransformPoint(_config.CapsuleEndOffset);
                    return Physics.OverlapCapsuleNonAlloc(point1, point2, _config.Radius, OverlapBuffer, _config.TargetLayers);

                case HitboxShape.Box:
                    return Physics.OverlapBoxNonAlloc(origin, _config.BoxHalfExtents, OverlapBuffer, transform.rotation, _config.TargetLayers);

                default:
                    return 0;
            }
        }

        private HitData BuildHitData(Collider col, GameObject target, ICombatTarget combatTarget)
        {
            Vector3 contactPoint = col.ClosestPoint(transform.position);
            Vector3 normal = (contactPoint - transform.position).normalized;
            float damage = _context?.Stats?.Evaluate(StatId.Damage, _context) ?? 0f;

            return new HitData
            {
                Source = gameObject,
                Target = target,
                Point = contactPoint,
                Normal = normal,
                Damage = damage,
                AttackTags = BuildAttackTags(),
                Context = _context
            };
        }

        private Tag[] BuildAttackTags()
        {
            if (_context?.Ability?.Tags == null) return Array.Empty<Tag>();

            var tags = new List<Tag>();
            foreach (var tag in _context.Ability.Tags)
                tags.Add(tag);
            return tags.ToArray();
        }
    }
}
