
namespace DynamicPhysics
{
    /**
     * <summary>
     * Fluent builder for composing <see cref="RuntimeMotionConfig"/> instances at runtime.
     * Loads a base <see cref="MovementProfile"/> and applies code-driven overrides,
     * bridging data-driven defaults with expressive runtime behavior.
     * </summary>
     *
     * <example>
     * <code>
     * var config = orchestrator.CreateBuilder()
     *     .FromProfile(groundProfile)
     *     .WithGravityScale(0.5f)
     *     .WithModifier(new DragModifier(0.1f))
     *     .WithMaxSpeed(25f)
     *     .Build();
     * </code>
     * </example>
     */
    public class MovementProfileBuilder
    {
        private readonly RuntimeMotionConfig _config;

        public MovementProfileBuilder(RuntimeMotionConfig config)
        {
            _config = config;
        }

        /**
         * <summary>
         * Loads baseline values from a <see cref="MovementProfile"/>.
         * </summary>
         *
         * <param name="profile">The profile to load.</param>
         * <returns>This builder for chaining.</returns>
         */
        public MovementProfileBuilder FromProfile(MovementProfile profile)
        {
            _config.LoadFromProfile(profile);
            return this;
        }

        /**
         * <summary>
         * Overrides the gravity scale.
         * </summary>
         *
         * <param name="scale">New gravity scale value.</param>
         * <returns>This builder for chaining.</returns>
         */
        public MovementProfileBuilder WithGravityScale(float scale)
        {
            _config.GravityScale = scale;
            return this;
        }

        /**
         * <summary>
         * Overrides the maximum movement speed.
         * </summary>
         *
         * <param name="maxSpeed">New max speed.</param>
         * <returns>This builder for chaining.</returns>
         */
        public MovementProfileBuilder WithMaxSpeed(float maxSpeed)
        {
            var s = _config.Steering;
            s.MaxSpeed = maxSpeed;
            _config.Steering = s;
            return this;
        }

        /**
         * <summary>
         * Overrides the acceleration rate.
         * </summary>
         *
         * <param name="acceleration">New acceleration in units/s².</param>
         * <returns>This builder for chaining.</returns>
         */
        public MovementProfileBuilder WithAcceleration(float acceleration)
        {
            var s = _config.Steering;
            s.Acceleration = acceleration;
            _config.Steering = s;
            return this;
        }

        /**
         * <summary>
         * Overrides the air control factor.
         * </summary>
         *
         * <param name="airControl">New air control value (0-1).</param>
         * <returns>This builder for chaining.</returns>
         */
        public MovementProfileBuilder WithAirControl(float airControl)
        {
            _config.AirControl = airControl;
            return this;
        }

        /**
         * <summary>
         * Overrides the ground friction coefficient.
         * </summary>
         *
         * <param name="friction">New friction value.</param>
         * <returns>This builder for chaining.</returns>
         */
        public MovementProfileBuilder WithFriction(float friction)
        {
            _config.Friction = friction;
            return this;
        }

        /**
         * <summary>
         * Adds a modifier to the active modifier list.
         * </summary>
         *
         * <param name="modifier">The modifier to add.</param>
         * <returns>This builder for chaining.</returns>
         */
        public MovementProfileBuilder WithModifier(IMotionModifier modifier)
        {
            _config.ActiveModifiers.Add(modifier);
            return this;
        }

        /**
         * <summary>
         * Adds a constraint to the active constraint list.
         * </summary>
         *
         * <param name="constraint">The constraint to add.</param>
         * <returns>This builder for chaining.</returns>
         */
        public MovementProfileBuilder WithConstraint(IMotionConstraint constraint)
        {
            _config.ActiveConstraints.Add(constraint);
            return this;
        }

        /**
         * <summary>
         * Adds a time-limited modifier that auto-expires.
         * </summary>
         *
         * <param name="modifier">The modifier to wrap.</param>
         * <param name="duration">Duration in seconds.</param>
         * <returns>This builder for chaining.</returns>
         */
        public MovementProfileBuilder WithTemporalModifier(IMotionModifier modifier, float duration)
        {
            _config.ActiveModifiers.Add(new TemporalModifier(modifier, duration));
            return this;
        }

        /**
         * <summary>
         * Returns the built configuration. The config object is reused — do not cache the reference.
         * </summary>
         *
         * <returns>The resolved runtime configuration.</returns>
         */
        public RuntimeMotionConfig Build() => _config;
    }
}
