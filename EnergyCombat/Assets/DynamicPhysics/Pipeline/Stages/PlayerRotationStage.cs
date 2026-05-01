using Extensions.Utils;
using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Determines the direction the player character should face based on input and momentum.
     * Sets DesiredFacingDirection in the motion context for the orchestrator to apply.
     * 
     * Rotation responsiveness is dampened by the control factor - at high speed with low control,
     * the facing direction changes more slowly, just like movement steering.
     * 
     * During hard stops (rapid direction changes), the character's facing lags behind the input,
     * tracking the momentum direction first and gradually transitioning to the new direction.
     * This creates a "runner skidding and turning" effect.
     * 
     * Respects the NoAutoRotate tag so abilities can override rotation control
     * (e.g., for dodge, attack, or special abilities where facing direction is custom).
     * </summary>
     */
    public class PlayerRotationStage : IMotionStage
    {
        /** <summary>Execution priority. Runs after InputSteering so we have control factor data.</summary> */
        public int Priority => InfluencePriority.InputSteering + 1;

        /** <summary>Smoothed facing direction maintained across frames.</summary> */
        private Vector3 _smoothedFacingDirection = Vector3.zero;
        
        /** <summary>Previous frame's velocity direction to detect hard stops.</summary> */
        private Vector3 _previousVelocityDirection = Vector3.zero;

        /**
         * <summary>
         * Processes rotation input and sets the desired facing direction,
         * dampened by the control factor to reflect movement responsiveness.
         * Blends between velocity direction (momentum) and input direction based on the angle between them.
         * </summary>
         */
        public void Execute(MotionContext context, RuntimeMotionConfig config)
        {
            // Skip rotation if explicitly disabled
            if (context.HasTag(MotionTag.NoAutoRotate))
            {
                context.DesiredFacingDirection = Vector3.zero;
                return;
            }

            var input = context.Input;
            var steering = config.Steering;
            
            Vector3 inputDir = input.CameraForward * input.MoveInput.y + input.CameraRight * input.MoveInput.x;
            inputDir.y = 0f;
            
            Vector3 horizontalVel = context.Velocity.ZeroVector3Axis();
            Vector3 velocityDir = horizontalVel.sqrMagnitude > 0.01f ? horizontalVel.normalized : _previousVelocityDirection;

            if (inputDir.sqrMagnitude > 0.01f)
            {
                inputDir = inputDir.normalized;
                
                // Detect hard stop: large angle between velocity and desired input direction
                // During hard stop, the character keeps facing momentum direction
                float angleToInput = Vector3.Angle(velocityDir, inputDir);
                bool isHardStop = angleToInput > steering.HardStopDriftAngleThreshold && context.HasTag(MotionTag.Grounded);
                
                // Choose facing target: blend between velocity direction (momentum) and input direction (desired)
                // Hard stop: mostly use velocity direction (stay facing where momentum takes you)
                // Normal movement: use input direction
                Vector3 facingTarget = inputDir;
                if (isHardStop && velocityDir.sqrMagnitude > 0.01f)
                {
                    // Blend toward velocity direction during hard stop
                    // As new velocity builds up and angle decreases, smoothly transition back to input
                    float skidInfluence = Mathf.Clamp01((angleToInput - steering.HardStopDriftAngleThreshold) / steering.HardStopSkidWindowSize);
                    facingTarget = Vector3.Lerp(inputDir, velocityDir, skidInfluence).normalized;
                }
                
                // Smooth the facing direction update based on control factor and movement profile
                // At high speed (low control), facing changes more slowly
                // At low speed (high control), facing responds more quickly
                float controlFactor = context.ControlFactor;
                float updateSpeed = steering.FacingDirectionUpdateSpeed;
                float blendAmount = controlFactor * updateSpeed * context.DeltaTime;
                
                _smoothedFacingDirection = Vector3.Lerp(_smoothedFacingDirection, facingTarget, blendAmount);
                context.DesiredFacingDirection = _smoothedFacingDirection;
            }
            else
            {
                context.DesiredFacingDirection = Vector3.zero;
            }
            
            _previousVelocityDirection = velocityDir;
        }
    }
}





