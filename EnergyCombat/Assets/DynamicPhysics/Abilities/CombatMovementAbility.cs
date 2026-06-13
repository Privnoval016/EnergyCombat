using System.Collections.Generic;
using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Motion ability that suppresses joystick steering while a movement-locking attack is active
     * and applies per-attack target pull impulses for any executing ability that requests it.
     * </summary>
     *
     * <remarks>
     * The ability activates whenever <see cref="Combat.CombatController.IsExecuting"/> is true,
     * then stays active for the duration of the whole combo chain (deactivating only when
     * <c>IsExecuting</c> becomes false). This ensures target pull fires for every attack in a
     * chain — not just the first one — regardless of whether the attack locks movement.
     *
     * Movement locking (<see cref="MotionTag.AttackMovementLocked"/>) is applied per-tick based
     * on the current ability's <see cref="Combat.AnimationRequest.LockMovement"/> setting, so it
     * correctly turns on and off as the combo advances through mixed locked/unlocked attacks.
     *
     * Horizontal velocity deceleration on attack start is controlled by
     * <see cref="Combat.CombatInputSettings.AttackEntryDecelerationRate"/>:
     * <list type="bullet">
     *   <item><c>0</c> — instant stop on attack start.</item>
     *   <item>Positive value — smooth ramp-out giving a brief momentum-carry feel.</item>
     * </list>
     * </remarks>
     */
    public class CombatMovementAbility : IMotionAbility
    {
        private readonly Combat.CombatController _combatController;
        private readonly MotionOrchestrator _orchestrator;

        private Combat.CombatContext _lastContext;

        /** <inheritdoc /> */
        public bool IsActive { get; private set; }

        public CombatMovementAbility(Combat.CombatController combatController,
                                     MotionOrchestrator orchestrator = null)
        {
            _combatController = combatController;
            _orchestrator     = orchestrator;
        }

        /** <inheritdoc /> */
        public bool TryConsumeRequest(MotionContext context, MotionRequest request) => false;

        /** <inheritdoc /> */
        public bool CanActivate(MotionContext context, List<MotionRequest> requests) =>
            _combatController != null && _combatController.IsExecuting && !IsActive;

        /** <inheritdoc /> */
        public void Activate(MotionContext context)
        {
            IsActive = true;
            _lastContext = null;
        }

        /** <inheritdoc /> */
        public void Tick(MotionContext context, float deltaTime)
        {
            if (_combatController == null || !_combatController.IsExecuting)
            {
                Deactivate(context);
                return;
            }

            // Apply or remove movement lock based on the current ability's settings
            if (_combatController.IsMovementLocked)
            {
                context.SetTag(MotionTag.AttackMovementLocked);
                ApplyAttackEntryDeceleration(context);
            }
            else
            {
                context.RemoveTag(MotionTag.AttackMovementLocked);
            }

            // Fire target pull once per new attack context (covers every hit in a combo chain)
            var activeCtx = _combatController.ActiveContext;
            if (activeCtx != null && !ReferenceEquals(activeCtx, _lastContext))
            {
                _lastContext = activeCtx;
                ApplyTargetPull(context, activeCtx.Ability);
            }
        }

        /** <inheritdoc /> */
        public Vector3 GetVelocityInfluence(MotionContext context) => Vector3.zero;

        /** <inheritdoc /> */
        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            _lastContext = null;
            context.RemoveTag(MotionTag.AttackMovementLocked);
        }

        /**
         * <summary>
         * Applies a velocity impulse toward the soft target when the ability requests it and
         * the target is within <see cref="Combat.AbilityDefinition.TargetPullRange"/>.
         * No-op when no orchestrator, no target, or the ability has pull disabled.
         * </summary>
         */
        private void ApplyTargetPull(MotionContext context, Combat.AbilityDefinition ability)
        {
            if (_orchestrator == null) return;
            if (ability == null || !ability.EnableTargetPull) return;

            var target = _combatController.CurrentTarget;
            if (target == null) return;

            Vector3 toTarget = target.TargetPosition - context.Position;
            if (toTarget.magnitude > ability.TargetPullRange) return;

            _orchestrator.AddImpulse(toTarget.normalized * ability.TargetPullForce);
        }

        /**
         * <summary>
         * Removes horizontal velocity according to <see cref="Combat.CombatInputSettings.AttackEntryDecelerationRate"/>.
         * Called on activation and each tick so the ramp-out continues across multiple frames.
         * Skipped when root motion is driving position — the clip owns movement in that case.
         * </summary>
         */
        private void ApplyAttackEntryDeceleration(MotionContext context)
        {
            if (context.HasTag(MotionTag.RootMotionDriven)) return;

            float rate = _combatController.InputSettings?.AttackEntryDecelerationRate ?? 0f;
            Vector3 vel = context.Velocity;
            Vector2 horiz = new Vector2(vel.x, vel.z);
            float speed = horiz.magnitude;
            if (speed < 0.001f) return;

            if (rate <= 0f)
            {
                context.Velocity = new Vector3(0f, vel.y, 0f);
                return;
            }

            float remove = rate * context.DeltaTime;
            if (remove >= speed)
            {
                context.Velocity = new Vector3(0f, vel.y, 0f);
            }
            else
            {
                Vector2 newHoriz = horiz * ((speed - remove) / speed);
                context.Velocity = new Vector3(newHoriz.x, vel.y, newHoriz.y);
            }
        }
    }
}
