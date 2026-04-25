using DynamicPhysics.Core;
using DynamicPhysics.Orchestration;
using UnityEngine;

namespace DynamicPhysics.Pipeline.Stages
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
     * The stage also handles camera-relative input conversion and friction-based deceleration.
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
            if ((context.Flags & MotionFlags.Stunned) != 0) return;

            // Reduce control during dashing, wall running, swinging, or sliding
            float contextualControl = 1f;
            if ((context.Flags & MotionFlags.Dashing) != 0) contextualControl = 0.05f;
            else if ((context.Flags & MotionFlags.WallRunning) != 0) contextualControl = 0.15f;
            else if ((context.Flags & MotionFlags.Swinging) != 0) contextualControl = 0.3f;
            else if ((context.Flags & MotionFlags.SlidingCrouch) != 0) contextualControl = 0.2f;

            // Camera-relative input direction (horizontal plane)
            Vector3 inputDir = input.CameraForward * input.MoveInput.y + input.CameraRight * input.MoveInput.x;
            inputDir.y = 0f;

            float inputSqrMag = inputDir.sqrMagnitude;
            bool hasInput = inputSqrMag > 0.001f;

            if (hasInput)
            {
                // Fast inverse sqrt approximation: normalize only when needed
                float invMag = 1f / Mathf.Sqrt(inputSqrMag);
                inputDir *= invMag;
            }

            // Extract horizontal velocity
            Vector3 horizontalVel = context.Velocity;
            horizontalVel.y = 0f;
            float currentSpeedSqr = horizontalVel.sqrMagnitude;
            float currentSpeed = currentSpeedSqr > 0.001f ? Mathf.Sqrt(currentSpeedSqr) : 0f;

            // Air control multiplier
            bool airborne = (context.Flags & MotionFlags.Airborne) != 0;
            float airMult = airborne ? config.AirControl : 1f;

            // Speed-dependent control: lerp between low-speed (snappy) and high-speed (drifty)
            float speedRatio = steering.MaxSpeed > 0f ? currentSpeed / steering.MaxSpeed : 0f;
            float controlFactor = Mathf.Lerp(steering.LowSpeedControl, steering.HighSpeedControl, speedRatio);
            controlFactor *= airMult * contextualControl * context.SteeringMultiplier;

            if (hasInput)
            {
                // Target velocity
                Vector3 targetVel = inputDir * steering.MaxSpeed;
                context.DesiredVelocity = targetVel;

                // Velocity delta
                Vector3 delta = targetVel - horizontalVel;
                float deltaMag = delta.magnitude;

                if (deltaMag > 0.001f)
                {
                    // Angular steering limit: rotate current velocity toward target direction
                    if (currentSpeed > 0.5f)
                    {
                        float maxAngle = steering.MaxTurnRate * dt * controlFactor;
                        Vector3 currentDir = horizontalVel * (1f / currentSpeed);
                        Vector3 steeredDir = Vector3.RotateTowards(currentDir, inputDir, maxAngle * Mathf.Deg2Rad, 0f);
                        horizontalVel = steeredDir * currentSpeed;
                    }

                    // Acceleration toward target speed
                    float accel = steering.Acceleration * controlFactor * dt;
                    float clampedAccel = Mathf.Min(accel, deltaMag);

                    Vector3 deltaDir = delta * (1f / deltaMag);
                    horizontalVel += deltaDir * clampedAccel;
                }
            }
            else
            {
                // No input: apply deceleration / friction
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

            // Write back horizontal velocity, preserving vertical
            context.Velocity.x = horizontalVel.x;
            context.Velocity.z = horizontalVel.z;
        }
    }
}
