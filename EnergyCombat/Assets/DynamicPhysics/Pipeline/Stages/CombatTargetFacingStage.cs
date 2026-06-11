using Combat.Targeting;
using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Pipeline stage that overrides <see cref="MotionContext.DesiredFacingDirection"/> to point
     * toward the current soft target whenever the player is executing an attack.
     * Produces the Batman Arkham "auto-face-enemy during strike" feel without locking
     * full camera control or hard-snapping rotation.
     * </summary>
     *
     * <remarks>
     * Priority is <see cref="InfluencePriority.InputSteering"/> + 2 so it runs after
     * <c>PlayerRotationStage</c> (InputSteering + 1) and its facing override takes
     * precedence during attacks. Does nothing when <see cref="MotionTag.NoAutoRotate"/>
     * is set (wall run, ledge grab) or when no target exists.
     * </remarks>
     */
    public class CombatTargetFacingStage : IMotionStage
    {
        private readonly ITargetProvider _targeting;
        private readonly PlayerController _player;

        /** <inheritdoc /> */
        public int Priority => InfluencePriority.InputSteering + 2;

        public CombatTargetFacingStage(ITargetProvider targeting, PlayerController player)
        {
            _targeting = targeting;
            _player = player;
        }

        /** <inheritdoc /> */
        public void Execute(MotionContext context, RuntimeMotionConfig config)
        {
            if (!_player.IsAttacking) return;
            if (!_targeting.HasTarget) return;
            if (context.HasTag(MotionTag.NoAutoRotate)) return;

            Vector3 toTarget = _targeting.CurrentTarget.TargetPosition - context.Position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f) return;

            context.DesiredFacingDirection = toTarget.normalized;
        }
    }
}
