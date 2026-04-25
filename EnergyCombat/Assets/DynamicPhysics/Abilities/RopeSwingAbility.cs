using DynamicPhysics.Constraints;
using DynamicPhysics.Core;
using UnityEngine;

namespace DynamicPhysics.Abilities
{
    /**
     * <summary>
     * Manages attachment to a <see cref="RopeEffector"/> and delegates physics enforcement
     * to a <see cref="RopeConstraint"/>. While swinging, gravity still applies and input
     * adds tangential force to steer the swing.
     * </summary>
     *
     * <remarks>
     * Activation requires an explicit <see cref="MotionRequestType.RopeAttach"/> request
     * with a <see cref="RopeEffector"/> set as the request's Target. The ability configures
     * the rope constraint and adds tangential input forces during the swing. Detach via
     * a <see cref="MotionRequestType.RopeDetach"/> request or jump request.
     *
     * The constraint reference must be provided at construction or via the orchestrator
     * so the ability can activate/deactivate it.
     * </remarks>
     */
    public class RopeSwingAbility : IMotionAbility
    {
        #region Configuration

        /** <summary>Force applied from input to steer the swing tangentially.</summary> */
        public float SwingInputForce { get; set; }

        /** <summary>Gravity multiplier while swinging (typically > 1 for faster swings).</summary> */
        public float SwingGravityScale { get; set; }

        #endregion

        #region State

        public bool IsActive { get; private set; }

        private RopeEffector _currentEffector;
        private readonly RopeConstraint _constraint;

        #endregion

        /**
         * <summary>
         * Creates a rope swing ability with a reference to the shared rope constraint.
         * </summary>
         *
         * <param name="constraint">The rope constraint instance managed by the orchestrator.</param>
         * <param name="swingInputForce">Tangential force from input during swing.</param>
         * <param name="swingGravityScale">Gravity multiplier while swinging.</param>
         */
        public RopeSwingAbility(RopeConstraint constraint, float swingInputForce = 20f,
            float swingGravityScale = 1.2f)
        {
            _constraint = constraint;
            SwingInputForce = swingInputForce;
            SwingGravityScale = swingGravityScale;
        }

        public bool CanActivate(MotionContext context, MotionRequest[] requests, int requestCount)
        {
            for (int i = 0; i < requestCount; i++)
            {
                if (requests[i].Type == MotionRequestType.RopeAttach && requests[i].Target is RopeEffector effector)
                {
                    _currentEffector = effector;
                    return true;
                }
            }

            return false;
        }

        public void Activate(MotionContext context)
        {
            IsActive = true;

            // Configure and activate the rope constraint
            _constraint.AnchorPoint = _currentEffector.AnchorPosition;
            _constraint.RopeLength = _currentEffector.CalculateRopeLength(context.Position);
            _constraint.MinRopeLength = _currentEffector.MinRopeLength;
            _constraint.IsActive = true;

            context.Flags |= MotionFlags.Swinging;
            context.Flags &= ~(MotionFlags.Grounded | MotionFlags.WallRunning);
            context.Flags |= MotionFlags.Airborne;
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            // Check for detach request
            // (Handled externally — orchestrator calls Deactivate on RopeDetach or Jump request)

            // Update anchor position (in case effector moves)
            if (_currentEffector != null)
            {
                _constraint.AnchorPoint = _currentEffector.AnchorPosition;
            }

            // Adjust gravity
            context.GravityScale *= SwingGravityScale;

            // Handle rope length adjustment if allowed
            if (_currentEffector != null && _currentEffector.AllowLengthAdjustment)
            {
                float verticalInput = context.Input.MoveInput.y;
                if (Mathf.Abs(verticalInput) > 0.1f)
                {
                    float adjustment = -verticalInput * _currentEffector.ReelSpeed * deltaTime;
                    _constraint.RopeLength = Mathf.Clamp(
                        _constraint.RopeLength + adjustment,
                        _currentEffector.MinRopeLength,
                        _currentEffector.MaxRopeLength);
                }
            }
        }

        public Vector3 GetVelocityInfluence(MotionContext context)
        {
            if (!IsActive) return Vector3.zero;

            // Calculate tangential input force for swing steering
            var input = context.Input;
            Vector3 inputDir = input.CameraForward * input.MoveInput.y + input.CameraRight * input.MoveInput.x;
            inputDir.y = 0f;

            if (inputDir.sqrMagnitude < 0.01f) return Vector3.zero;

            // Project input onto the tangent plane of the rope sphere
            Vector3 toChar = context.Position - _constraint.AnchorPoint;
            float dist = toChar.magnitude;
            if (dist < 0.01f) return Vector3.zero;

            Vector3 radial = toChar * (1f / dist);
            Vector3 tangentialInput = inputDir - Vector3.Dot(inputDir, radial) * radial;

            return tangentialInput * (SwingInputForce * context.DeltaTime);
        }

        public void Deactivate(MotionContext context)
        {
            IsActive = false;
            _constraint.IsActive = false;
            _currentEffector = null;

            context.Flags &= ~MotionFlags.Swinging;
            if ((context.Flags & MotionFlags.Grounded) == 0)
            {
                context.Flags |= MotionFlags.Airborne;
            }
        }
    }
}
