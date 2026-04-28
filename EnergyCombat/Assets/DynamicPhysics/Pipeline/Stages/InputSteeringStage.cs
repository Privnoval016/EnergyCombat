using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Converts player input into velocity changes while respecting momentum and acceleration limits.
     * Produces the "fast but controlled" feel required for parkour combat by interpolating
     * between high-speed drift and low-speed snappiness.
     * </summary>
     *
     * <remarks>
     * At high speed, control is reduced, producing natural sliding when reversing direction.
     * At low speed, control is increased, producing responsive tight movement.
     * Uses sqrMagnitude and avoids normalization where possible for performance.
     * </remarks>
     */
    public class InputSteeringStage : IMotionStage
    {
        /** <summary>Execution priority. Runs first in the pipeline.</summary> */
        public int Priority => InfluencePriority.InputSteering;

        /**
         * <summary>
         * Processes input into velocity changes with momentum-based steering.
         * </summary>
         */
        public void Execute(MotionContext context, RuntimeMotionConfig config)
        {
            var steering = config.Steering;
            var input = context.Input;
            float dt = context.DeltaTime;

            // Suppress input when stunned
            if (context.HasTag(MotionTag.Stunned)) return;

            // Extract state
            float contextualControl = GetContextualControl(context);
            float targetSpeed = GetTargetSpeed(context, steering);
            float targetAcceleration = GetTargetAcceleration(context, steering);
            Vector3 inputDir = GetInputDirection(input, out bool hasInput);
            
            Vector3 horizontalVel = context.Velocity;
            horizontalVel.y = 0f;
            float currentSpeed = GetCurrentSpeed(horizontalVel);
            
            bool airborne = context.HasTag(MotionTag.Airborne);
            float controlFactor = GetControlFactor(steering, targetSpeed, currentSpeed, contextualControl, airborne, config, context);

            // Apply steering logic
            if (hasInput)
            {
                ApplyInputSteering(context, ref horizontalVel, inputDir, steering, targetSpeed, targetAcceleration, currentSpeed, airborne, controlFactor, dt);
            }
            else
            {
                ApplyDeceleration(context, ref horizontalVel, steering, currentSpeed, airborne, config, dt);
            }

            context.Velocity.x = horizontalVel.x;
            context.Velocity.z = horizontalVel.z;
        }

        private float GetContextualControl(MotionContext context)
        {
            if (context.HasTag(MotionTag.Dashing)) return 0.05f;
            else if (context.HasTag(MotionTag.WallRunning)) return 0.15f;
            else if (context.HasTag(MotionTag.Swinging)) return 0.3f;
            else if (context.HasTag(MotionTag.SlidingCrouch)) return 0.2f;
            return 1f;
        }

        private float GetTargetSpeed(MotionContext context, SteeringSettings steering)
        {
            return context.HasTag(MotionTag.Sprinting) ? steering.SprintSpeed : steering.MaxSpeed;
        }

        private float GetTargetAcceleration(MotionContext context, SteeringSettings steering)
        {
            return context.HasTag(MotionTag.Sprinting) ? steering.SprintAcceleration : steering.Acceleration;
        }

        private Vector3 GetInputDirection(MotionInputData input, out bool hasInput)
        {
            Vector3 inputDir = input.CameraForward * input.MoveInput.y + input.CameraRight * input.MoveInput.x;
            inputDir.y = 0f;

            float inputSqrMag = inputDir.sqrMagnitude;
            hasInput = inputSqrMag > 0.001f;

            if (hasInput)
            {
                inputDir *= 1f / Mathf.Sqrt(inputSqrMag);
            }

            return inputDir;
        }

        private float GetCurrentSpeed(Vector3 horizontalVel)
        {
            float currentSpeedSqr = horizontalVel.sqrMagnitude;
            return currentSpeedSqr > 0.001f ? Mathf.Sqrt(currentSpeedSqr) : 0f;
        }

        private float GetControlFactor(SteeringSettings steering, float targetSpeed, float currentSpeed, float contextualControl, bool airborne, RuntimeMotionConfig config, MotionContext context)
        {
            float speedRatio = targetSpeed > 0f ? currentSpeed / targetSpeed : 0f;
            float controlFactor = Mathf.Lerp(steering.LowSpeedControl, steering.HighSpeedControl, speedRatio);
            float airMult = airborne ? config.AirControl : 1f;
            return controlFactor * airMult * contextualControl * context.SteeringMultiplier;
        }

        private void ApplyInputSteering(MotionContext context, ref Vector3 horizontalVel, Vector3 inputDir, SteeringSettings steering, float targetSpeed, float targetAcceleration, float currentSpeed, bool airborne, float controlFactor, float dt)
        {
            Vector3 targetVel = inputDir * targetSpeed;
            context.DesiredVelocity = targetVel;

            Vector3 delta = targetVel - horizontalVel;
            float deltaMag = delta.magnitude;

            if (deltaMag > 0.001f)
            {
                bool canHardStop = !airborne && currentSpeed > 0.5f;
                bool speedThresholdMet = currentSpeed >= targetSpeed * steering.HardStopDriftMinSpeedRatio;

                if (canHardStop && speedThresholdMet)
                {
                    Vector3 currentDir = horizontalVel * (1f / currentSpeed);
                    float angle = Vector3.Angle(currentDir, inputDir);

                    if (angle > steering.HardStopDriftAngleThreshold)
                    {
                        ApplyHardStopDrift(ref horizontalVel, currentDir, inputDir, currentSpeed, targetSpeed, targetAcceleration, steering, dt);
                    }
                    else
                    {
                        ApplyNormalSteering(ref horizontalVel, currentDir, inputDir, currentSpeed, targetAcceleration, controlFactor, delta, deltaMag, steering, dt);
                    }
                }
                else
                {
                    if (currentSpeed > 0.5f)
                    {
                        Vector3 currentDir = horizontalVel * (1f / currentSpeed);
                        ApplyNormalSteering(ref horizontalVel, currentDir, inputDir, currentSpeed, targetAcceleration, controlFactor, delta, deltaMag, steering, dt);
                    }
                    else
                    {
                        ApplyNormalSteering(ref horizontalVel, inputDir, inputDir, currentSpeed, targetAcceleration, controlFactor, delta, deltaMag, steering, dt);
                    }
                }
            }
        }

        private void ApplyHardStopDrift(ref Vector3 horizontalVel, Vector3 currentDir, Vector3 inputDir, float currentSpeed, float targetSpeed, float targetAcceleration, SteeringSettings steering, float dt)
        {
            float frictionDecel = steering.HardStopDriftDeceleration * dt;
            float newSpeed = Mathf.Max(0f, currentSpeed - frictionDecel);
            horizontalVel = currentDir * newSpeed;

            Vector3 hardStopDelta = (inputDir * targetSpeed) - horizontalVel;
            float hardStopDeltaMag = hardStopDelta.magnitude;

            if (hardStopDeltaMag > 0.001f)
            {
                float reaccelMult = steering.HardStopDriftReaccelerationMultiplier;
                float accel = targetAcceleration * reaccelMult * dt;
                float clampedAccel = Mathf.Min(accel, hardStopDeltaMag);

                Vector3 deltaDir = hardStopDelta * (1f / hardStopDeltaMag);
                horizontalVel += deltaDir * clampedAccel;
            }
        }

        private void ApplyNormalSteering(ref Vector3 horizontalVel, Vector3 currentDir, Vector3 inputDir, float currentSpeed, float targetAcceleration, float controlFactor, Vector3 delta, float deltaMag, SteeringSettings steering, float dt)
        {
            if (currentSpeed > 0.5f)
            {
                float maxAngle = steering.MaxTurnRate * dt * controlFactor;
                Vector3 steeredDir = Vector3.RotateTowards(currentDir, inputDir, maxAngle * Mathf.Deg2Rad, 0f);
                horizontalVel = steeredDir * currentSpeed;
            }

            float accel = targetAcceleration * controlFactor * dt;
            float clampedAccel = Mathf.Min(accel, deltaMag);

            Vector3 deltaDir = delta * (1f / deltaMag);
            horizontalVel += deltaDir * clampedAccel;
        }

        private void ApplyDeceleration(MotionContext context, ref Vector3 horizontalVel, SteeringSettings steering, float currentSpeed, bool airborne, RuntimeMotionConfig config, float dt)
        {
            context.DesiredVelocity = Vector3.zero;

            if (currentSpeed > 0.01f)
            {
                float decel = airborne
                    ? steering.Deceleration * config.AirControl * dt
                    : config.Friction * dt;

                float newSpeed = Mathf.Max(0f, currentSpeed - decel);
                horizontalVel *= newSpeed / currentSpeed;
            }
        }
    }
}
