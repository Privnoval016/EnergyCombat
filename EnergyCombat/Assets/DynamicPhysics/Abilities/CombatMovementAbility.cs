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
     * This ability produces no velocity influence of its own — it acts purely as a tag setter
     * so the rest of the pipeline can react to it. Deactivates as soon as the ability ends.
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
        }

        /** <inheritdoc /> */
        public void Tick(MotionContext context, float deltaTime)
        {
            if (_combatController == null || !_combatController.IsMovementLocked)
                Deactivate(context);
        }

        /** <inheritdoc /> */
        public Vector3 GetVelocityInfluence(MotionContext context) => Vector3.zero;

        /** <inheritdoc /> */
        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            context.RemoveTag(MotionTag.AttackMovementLocked);
        }
    }
}
