using System.Collections.Generic;
using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Motion ability that suppresses joystick steering while a movement-locking attack is active.
     * Activates automatically when <see cref="Combat.CombatController.IsMovementLocked"/> is true
     * and sets <see cref="MotionTag.AttackMovementLocked"/> so <see cref="InputSteeringStage"/>
     * returns zero contextual control.
     * </summary>
     *
     * <remarks>
     * On activation and each tick, horizontal velocity is bled off at the rate configured by
     * <see cref="Combat.CombatInputSettings.AttackEntryDecelerationRate"/>:
     * <list type="bullet">
     *   <item><c>0</c> (default) — instant stop: the character plants immediately on attack start.</item>
     *   <item>Positive value — smooth ramp-out over multiple <c>FixedUpdate</c> ticks, giving a
     *     brief momentum-carry feel before the character fully commits to the attack stance.</item>
     * </list>
     * </remarks>
     */
    public class CombatMovementAbility : IMotionAbility
    {
        private readonly Combat.CombatController _combatController;

        /** <inheritdoc /> */
        public bool IsActive { get; private set; }

        public CombatMovementAbility(Combat.CombatController combatController)
        {
            _combatController = combatController;
        }

        /** <inheritdoc /> */
        public bool TryConsumeRequest(MotionContext context, MotionRequest request) => false;

        /** <inheritdoc /> */
        public bool CanActivate(MotionContext context, List<MotionRequest> requests) =>
            _combatController != null && _combatController.IsMovementLocked && !IsActive;

        /** <inheritdoc /> */
        public void Activate(MotionContext context)
        {
            IsActive = true;
            context.SetTag(MotionTag.AttackMovementLocked);
            ApplyAttackEntryDeceleration(context);
        }

        /** <inheritdoc /> */
        public void Tick(MotionContext context, float deltaTime)
        {
            if (_combatController == null || !_combatController.IsMovementLocked)
            {
                Deactivate(context);
                return;
            }

            ApplyAttackEntryDeceleration(context);
        }

        /** <inheritdoc /> */
        public Vector3 GetVelocityInfluence(MotionContext context) => Vector3.zero;

        /** <inheritdoc /> */
        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            context.RemoveTag(MotionTag.AttackMovementLocked);
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
