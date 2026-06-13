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
     *
     * When airborne and <see cref="Combat.CombatInputSettings.EnableAerialVerticalAim"/> is
     * enabled, a vertical pitch component is added toward the target's elevation, clamped to
     * <see cref="Combat.CombatInputSettings.MaxAerialPitchAngle"/> degrees. The pitch is
     * zeroed out on the first frame the attack ends to prevent stale pitched directions from
     * persisting into normal locomotion.
     * </remarks>
     */
    public class CombatTargetFacingStage : IMotionStage
    {
        private readonly ITargetProvider _targeting;
        private readonly PlayerController _player;
        private readonly Combat.CombatInputSettings _settings;

        /** <inheritdoc /> */
        public int Priority => InfluencePriority.InputSteering + 2;

        public CombatTargetFacingStage(ITargetProvider targeting, PlayerController player,
                                       Combat.CombatInputSettings settings = null)
        {
            _targeting = targeting;
            _player    = player;
            _settings  = settings;
        }

        /** <inheritdoc /> */
        public void Execute(MotionContext context, RuntimeMotionConfig config)
        {
            bool isAttacking = _player.IsAttacking;
            bool hasTarget   = _targeting.HasTarget;

            // When not attacking or no target: zero any vertical pitch left from a prior aerial
            // attack so stale pitched directions don't bleed into normal locomotion.
            if (!isAttacking || !hasTarget || context.HasTag(MotionTag.NoAutoRotate))
            {
                Vector3 cur = context.DesiredFacingDirection;
                if (cur.y != 0f)
                {
                    cur.y = 0f;
                    if (cur.sqrMagnitude > 0.001f)
                        context.DesiredFacingDirection = cur.normalized;
                }
                return;
            }

            Vector3 toTarget   = _targeting.CurrentTarget.TargetPosition - context.Position;
            Vector3 horizontal = new Vector3(toTarget.x, 0f, toTarget.z);
            if (horizontal.sqrMagnitude < 0.01f) return;

            // Override rotation speed so combat snap is faster than locomotion default
            if (_settings != null)
                context.RotationSpeedOverride = _settings.CombatFacingRotationSpeed;

            bool verticalAim = (_settings?.EnableAerialVerticalAim ?? false) && _player.IsAirborne;
            if (verticalAim)
            {
                float maxPitch  = _settings.MaxAerialPitchAngle;
                float vertAngle = Mathf.Atan2(toTarget.y, horizontal.magnitude) * Mathf.Rad2Deg;
                float clamped   = Mathf.Clamp(vertAngle, -maxPitch, maxPitch);
                float rad       = clamped * Mathf.Deg2Rad;

                Vector3 facingDir = horizontal.normalized * Mathf.Cos(rad)
                                  + Vector3.up            * Mathf.Sin(rad);
                context.DesiredFacingDirection = facingDir.normalized;
            }
            else
            {
                context.DesiredFacingDirection = horizontal.normalized;
            }
        }
    }
}
