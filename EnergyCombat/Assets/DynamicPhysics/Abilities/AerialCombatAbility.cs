using System.Collections.Generic;
using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Motion ability that manages gravity suppression during aerial attack combos,
     * producing the "hang in the air" feel seen in DMC, Nier, and MGRR.
     * </summary>
     *
     * <remarks>
     * The ability activates automatically whenever the player is airborne and
     * <see cref="Combat.CombatController.IsExecuting"/> is true.
     * Settings are read live from <see cref="Combat.CombatController.InputSettings"/> each tick
     * so runtime ScriptableObject edits take effect immediately without a reload.
     * <see cref="Combat.CombatInputSettings.AerialAttackGravityScale"/> is normalised against the
     * active <see cref="MovementProfile.GravityScale"/> so that <c>0</c> always means no gravity
     * and <c>1</c> always means full perceived gravity, regardless of the profile's base multiplier.
     *
     * Each new aerial attack (detected by a new <see cref="Combat.CombatContext"/> reference)
     * applies two effects:
     * <list type="number">
     *   <item>Cancels a fraction of downward velocity via
     *     <see cref="Combat.CombatInputSettings.AerialAttackVelocityDamping"/>.</item>
     *   <item>Reduces <see cref="MotionContext.GravityScale"/> to produce the float window,
     *     governed by <see cref="Combat.CombatInputSettings.AerialAttackGravityScale"/> with
     *     exponential decay via <see cref="Combat.CombatInputSettings.AerialGravityDecayPerHit"/>.</item>
     * </list>
     *
     * Per-ability overrides are available on <see cref="Combat.AbilityDefinition"/> via
     * <c>OverrideAerialGravity</c> / <c>AerialGravityScaleOverride</c>.
     *
     * The aerial hit counter resets to zero on landing (<see cref="MotionTag.Grounded"/>)
     * so each new jump starts with full float capacity.
     * </remarks>
     */
    public class AerialCombatAbility : IMotionAbility
    {
        private readonly Combat.CombatController _combatController;
        private readonly MotionOrchestrator _orchestrator;

        private int _aerialAttackCount;
        private Combat.CombatContext _lastContext;
        private float _postAttackTimer;

        /** <inheritdoc /> */
        public bool IsActive { get; private set; }

        // Read live so runtime SO edits take effect immediately, matching CombatMovementAbility.
        private Combat.CombatInputSettings Settings => _combatController?.InputSettings;

        // The profile's GravityScale multiplies context.GravityScale inside GravityStage
        // (effective = context.GravityScale * profile.GravityScale). Dividing by the profile
        // value lets the designer-facing AerialAttackGravityScale express a fraction of
        // perceived normal gravity rather than a fraction of an opaque internal unit.
        private float ProfileGravityScale =>
            _orchestrator?.ActiveProfile?.GravityScale is float f && f > 0f ? f : 1f;

        public AerialCombatAbility(Combat.CombatController combatController,
                                   MotionOrchestrator orchestrator)
        {
            _combatController = combatController;
            _orchestrator     = orchestrator;
        }

        /** <inheritdoc /> */
        public bool TryConsumeRequest(MotionContext context, MotionRequest request) => false;

        /** <inheritdoc /> */
        public bool CanActivate(MotionContext context, List<MotionRequest> requests) =>
            !IsActive
            && !context.HasTag(MotionTag.Grounded)
            && _combatController != null
            && _combatController.IsExecuting;

        /** <inheritdoc /> */
        public void Activate(MotionContext context)
        {
            IsActive = true;
            _postAttackTimer = 0f;

            // The orchestrator calls Activate but not Tick on the same frame, so apply gravity
            // suppression and damping immediately to avoid 1 frame of full gravity at attack start.
            var activeCtx = _combatController.ActiveContext;
            if (activeCtx != null)
            {
                _lastContext = activeCtx;
                _aerialAttackCount++;
                ApplyDamping(context);
                context.GravityScale = ComputeGravityScale(activeCtx.Ability);
            }
        }

        /** <inheritdoc /> */
        public void Tick(MotionContext context, float deltaTime)
        {
            // Landing resets everything
            if (context.HasTag(MotionTag.Grounded))
            {
                _aerialAttackCount = 0;
                _lastContext       = null;
                _postAttackTimer   = 0f;
                Deactivate(context);
                return;
            }

            if (_combatController.IsExecuting)
            {
                // Refresh the hover window each frame an attack is running
                _postAttackTimer = Settings?.PostAttackHoverDuration ?? 0f;

                // Track new attacks by CombatContext reference change.
                // ActiveContext can be transiently null during combo transitions even while
                // IsExecuting is true, so we only process a new attack when it is non-null.
                var activeCtx = _combatController.ActiveContext;
                if (activeCtx != null && !ReferenceEquals(activeCtx, _lastContext))
                {
                    _lastContext = activeCtx;
                    _aerialAttackCount++;
                    ApplyDamping(context);
                }

                // Use last-known ability for per-ability gravity overrides
                context.GravityScale = ComputeGravityScale(_lastContext?.Ability);
            }
            else if (_postAttackTimer > 0f)
            {
                // Keep gravity suppressed during the post-attack hover window so the player
                // doesn't immediately drop when the attack pipeline ends
                _postAttackTimer -= deltaTime;
                context.GravityScale = ComputeGravityScale(_lastContext?.Ability);
            }
            else
            {
                Deactivate(context);
            }
        }

        /** <inheritdoc /> */
        public Vector3 GetVelocityInfluence(MotionContext context) => Vector3.zero;

        /** <inheritdoc /> */
        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            _postAttackTimer = 0f;
        }

        /**
         * <summary>
         * Cancels the current downward velocity component by the configured damping fraction.
         * Only affects downward velocity (y &lt; 0) so upward momentum from a jump is preserved.
         * </summary>
         */
        private void ApplyDamping(MotionContext context)
        {
            if (context.Velocity.y >= 0f) return;
            float damping = Settings?.AerialAttackVelocityDamping ?? 0.85f;
            context.Velocity = new Vector3(
                context.Velocity.x,
                context.Velocity.y * (1f - damping),
                context.Velocity.z);
        }

        /**
         * <summary>
         * Returns the gravity scale to apply this frame.
         * Per-ability <see cref="Combat.AbilityDefinition.OverrideAerialGravity"/> takes
         * precedence over the global decay curve.
         * </summary>
         */
        private float ComputeGravityScale(Combat.AbilityDefinition ability)
        {
            float profileScale = ProfileGravityScale;

            if (ability != null && ability.OverrideAerialGravity)
            {
                float abs = Mathf.Clamp01(ability.AerialGravityScaleOverride);
                return abs / profileScale;
            }

            float baseScale = Settings?.AerialAttackGravityScale             ?? 0.2f;
            float decay     = Settings?.AerialGravityDecayPerHit             ?? 1.25f;
            int   maxHits   = Settings?.MaxAerialAttacksBeforeGravityReturns ?? 5;

            int effectiveCount = maxHits > 0
                ? Mathf.Min(_aerialAttackCount, maxHits)
                : _aerialAttackCount;

            // Divide by profileScale so the SO value expresses a true fraction of perceived
            // gravity. Without this, a profile GravityScale of 8 would mean 0.05 in the SO
            // becomes 0.05 × 8 = 0.4 effective gravity — far heavier than intended.
            float intended = Mathf.Clamp01(baseScale * Mathf.Pow(decay, effectiveCount - 1));
            return intended / profileScale;
        }
    }
}
