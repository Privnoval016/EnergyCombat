using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Manages attachment to a <see cref="RopeEffector"/> and delegates physics enforcement
     * to a <see cref="RopeConstraint"/>. Intercepts Jump and RopeDetach requests to detach.
     * </summary>
     */
    public class RopeSwingAbility : IMotionAbility
    {
        #region Configuration

        public float SwingInputForce { get; set; }
        public float SwingGravityScale { get; set; }

        #endregion

        #region State

        public bool IsActive { get; private set; }
        private RopeEffector _currentEffector;
        private readonly RopeConstraint _constraint;

        #endregion

        public RopeSwingAbility(RopeConstraint constraint, float swingInputForce = 20f,
            float swingGravityScale = 1.2f)
        {
            _constraint = constraint;
            SwingInputForce = swingInputForce;
            SwingGravityScale = swingGravityScale;
        }

        /**
         * <summary>
         * Intercepts Jump requests (launch off rope) and RopeDetach requests (clean detach).
         * </summary>
         */
        public bool TryConsumeRequest(MotionContext context, MotionRequest request)
        {
            if (request.Type == MotionRequestType.Jump || request.Type == MotionRequestType.RopeDetach)
            {
                Deactivate(context);
                return true;
            }
            return false;
        }

        public bool CanActivate(MotionContext context, List<MotionRequest> requests)
        {
            for (int i = 0; i < requests.Count; i++)
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

            _constraint.AnchorPoint = _currentEffector.AnchorPosition;
            _constraint.RopeLength = _currentEffector.CalculateRopeLength(context.Position);
            _constraint.MinRopeLength = _currentEffector.MinRopeLength;
            _constraint.IsActive = true;

            context.SetTag(MotionTag.Swinging);
            context.RemoveTag(MotionTag.Grounded);
            context.RemoveTag(MotionTag.WallRunning);
            context.SetTag(MotionTag.Airborne);
        }

        public void Tick(MotionContext context, float deltaTime)
        {
            if (_currentEffector != null)
            {
                _constraint.AnchorPoint = _currentEffector.AnchorPosition;
            }

            context.GravityScale *= SwingGravityScale;

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

            var input = context.Input;
            Vector3 inputDir = input.CameraForward * input.MoveInput.y + input.CameraRight * input.MoveInput.x;
            inputDir.y = 0f;

            if (inputDir.sqrMagnitude < 0.01f) return Vector3.zero;

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

            context.RemoveTag(MotionTag.Swinging);
            if (!context.HasTag(MotionTag.Grounded))
            {
                context.SetTag(MotionTag.Airborne);
            }
        }
    }
}
