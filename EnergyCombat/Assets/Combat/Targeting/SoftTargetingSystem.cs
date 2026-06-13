using System;
using Extensions.EventBus;
using UnityEngine;

namespace Combat.Targeting
{
    /**
     * <summary>
     * MonoBehaviour that implements soft targeting: no explicit lock-on button,
     * but always tracks the best nearby candidate and locks onto it during attacks.
     * Attach to the player GameObject and assign a <see cref="TargetingSettings"/> asset.
     * </summary>
     *
     * <remarks>
     * Target evaluation is throttled by <see cref="TargetingSettings.UpdateInterval"/>
     * to avoid per-frame iteration over all registered candidates. During an attack
     * ability execution the current target is locked (if <c>LockTargetDuringAttacks</c>
     * is enabled) so mid-strike target switches do not change animation direction.
     * </remarks>
     */
    [AddComponentMenu("Combat/Targeting/Soft Targeting System")]
    public class SoftTargetingSystem : MonoBehaviour, ITargetProvider
    {
        [SerializeField] private TargetingSettings _settings;

        /**
         * <summary>
         * Optional transform whose <c>forward</c> direction is used as the "facing" axis when
         * scoring targeting candidates. Assign the camera transform so scoring uses the direction
         * the <em>player is looking</em> rather than the direction the <em>character body is
         * rotated</em>.
         * </summary>
         *
         * <remarks>
         * Without this, <see cref="DynamicPhysics.CombatTargetFacingStage"/> rotates the player
         * body toward the current target during attacks, giving it an angle factor near 1.0 and
         * making it virtually unbeatable regardless of scorer weights. Assigning the camera
         * transform breaks this self-reinforcing loop so distance and off-angle candidates compete
         * fairly. Falls back to <c>transform.forward</c> (body forward) when left unassigned.
         * </remarks>
         */
        [Tooltip("Transform whose forward is used for angle scoring. Assign the camera transform to score by view direction instead of body direction. Falls back to body forward when unassigned.")]
        [SerializeField] private Transform _facingReference;

        /** <inheritdoc /> */
        public ITargetable CurrentTarget { get; private set; }

        /** <inheritdoc /> */
        public bool HasTarget => CurrentTarget != null;

        /**
         * <summary>
         * Fires when the selected target changes.
         * Parameters: (previousTarget, newTarget). Either may be <c>null</c>.
         * </summary>
         */
        public event Action<ITargetable, ITargetable> OnTargetChanged;

        private bool _isLocked;
        private float _nextUpdateTime;

        private EventBinding<AbilityStartedEvent> _startedBinding;
        private EventBinding<AbilityEndedEvent> _endedBinding;

        private void OnEnable()
        {
            _startedBinding = new EventBinding<AbilityStartedEvent>(OnAbilityStarted);
            _endedBinding   = new EventBinding<AbilityEndedEvent>(OnAbilityEnded);
            EventBus<AbilityStartedEvent>.Register(_startedBinding);
            EventBus<AbilityEndedEvent>.Register(_endedBinding);
        }

        private void OnDisable()
        {
            EventBus<AbilityStartedEvent>.Deregister(_startedBinding);
            EventBus<AbilityEndedEvent>.Deregister(_endedBinding);
        }

        private void Update()
        {
            if (_isLocked) return;
            if (Time.time < _nextUpdateTime) return;
            _nextUpdateTime = Time.time + GetUpdateInterval();
            EvaluateBestTarget();
        }

        private float GetUpdateInterval() =>
            _settings != null ? _settings.UpdateInterval : 0.1f;

        private void EvaluateBestTarget()
        {
            TargetingScorer scorer = _settings?.Scorer;
            if (scorer == null)
            {
                SetTarget(null);
                return;
            }

            Vector3 pos     = transform.position;
            Vector3 forward = _facingReference != null ? _facingReference.forward : transform.forward;

            ITargetable best      = null;
            float       bestScore = float.NegativeInfinity;

            foreach (ITargetable candidate in TargetingRegistry.All)
            {
                if (!candidate.IsTargetable) continue;
                float score = scorer.Score(candidate, pos, forward, CurrentTarget);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            SetTarget(bestScore > 0f ? best : null);
        }

        private void SetTarget(ITargetable newTarget)
        {
            if (ReferenceEquals(newTarget, CurrentTarget)) return;
            ITargetable old = CurrentTarget;
            CurrentTarget = newTarget;
            OnTargetChanged?.Invoke(old, newTarget);
        }

        private void OnAbilityStarted(AbilityStartedEvent evt)
        {
            if (_settings != null && _settings.LockTargetDuringAttacks)
                _isLocked = true;
        }

        private void OnAbilityEnded(AbilityEndedEvent evt)
        {
            _isLocked = false;
        }
    }
}
