using System.Collections.Generic;
using UnityEngine;

namespace DynamicPhysics
{
    /**
     * <summary>
     * Primary MonoBehaviour component that drives the DynamicPhysics locomotion engine.
     * Attach this to any character GameObject with a <see cref="Rigidbody"/>.
     *
     * All gameplay interaction flows through a single generic <see cref="Request(MotionRequest)"/>
     * method. The orchestrator routes requests to abilities — it never contains ability-specific logic.
     * </summary>
     *
     * <remarks>
     * <para>
     * Request routing order:
     * <list type="number">
     *   <item>Active abilities receive each request via <see cref="IMotionAbility.TryConsumeRequest"/>.</item>
     *   <item>Unconsumed requests are passed to inactive abilities via <see cref="IMotionAbility.CanActivate"/>.</item>
     * </list>
     * This keeps all ability-specific logic in the ability layer (e.g., wall jump is handled
     * by WallRunAbility intercepting a Jump request, not by the orchestrator).
     * </para>
     * </remarks>
     */
    [RequireComponent(typeof(Rigidbody))]
    public class MotionOrchestrator : MonoBehaviour
    {
        #region Serialized Fields

        /** <summary>Default movement profile loaded on initialization.</summary> */
        [Header("Profile")]
        [SerializeField] private MovementProfile defaultProfile;

        /** <summary>Ground detection configuration.</summary> */
        [Header("Ground Detection")]
        [SerializeField] private GroundDetector groundDetector = new();

        #endregion

        #region Runtime State

        private Rigidbody _rigidbody;
        private PhysicsMotor _motor;
        private MotionPipeline _pipeline;
        private MotionContext _context;
        private RuntimeMotionConfig _config;
        private MovementProfile _activeProfile;

        private readonly List<IMotionAbility> _abilities = new();
        private readonly List<IMotionModifier> _modifiers = new();
        private readonly List<IMotionConstraint> _constraints = new();

        private readonly List<MotionRequest> _requestBuffer = new();

        private readonly List<MotionRequest> _unconsumedBuffer = new();

        private IMotionInputProvider _inputProvider;

        #endregion

        #region Properties

        /** <summary>The shared motion context. Read-only access for debugging.</summary> */
        public MotionContext Context => _context;

        /** <summary>Whether the character is currently grounded.</summary> */
        public bool IsGrounded => _context.HasTag(MotionTag.Grounded);

        /** <summary>Whether the character is currently dashing.</summary> */
        public bool IsDashing => _context.HasTag(MotionTag.Dashing);

        /** <summary>Whether the character is currently in a sliding crouch state.</summary> */
        public bool IsSlidingCrouch => _context.HasTag(MotionTag.SlidingCrouch);

        /** <summary>Current velocity of the character.</summary> */
        public Vector3 Velocity => _context.Velocity;

        /** <summary>The currently active movement profile.</summary> */
        public MovementProfile ActiveProfile => _activeProfile;

        /** <summary>Checks whether a motion tag is currently active.</summary> */
        public bool HasTag(string motionTag) => _context.HasTag(motionTag);

        #endregion

        #region MonoBehaviour Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            _motor = new PhysicsMotor();
            _motor.Initialize(_rigidbody);

            _context = new MotionContext();
            _config = new RuntimeMotionConfig();

            _pipeline = new MotionPipeline();
            _pipeline.AddStage(new InputSteeringStage());
            _pipeline.AddStage(new AbilityInfluenceStage());
            _pipeline.AddStage(new ExternalForceStage());
            _pipeline.AddStage(new GravityStage());
            _pipeline.AddStage(new ModifierStage());
            _pipeline.AddStage(new ConstraintStage());
            
            _constraints.Add(new GroundSnapConstraint(defaultProfile.SnapForce));
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // 1. Snapshot current physics state
            _motor.SnapshotState(_context);
            _context.DeltaTime = dt;

            // 2. Ground detection
            groundDetector.Detect(_context.Position);
            _context.GroundNormal = groundDetector.GroundNormal;
            _context.GroundPoint = groundDetector.GroundPoint;
            _context.GroundAngle = groundDetector.GroundAngle;

            if (groundDetector.IsGrounded)
            {
                _context.SetTag(MotionTag.Grounded);
                _context.RemoveTag(MotionTag.Airborne);
            }
            else
            {
                _context.SetTag(MotionTag.Airborne);
                _context.RemoveTag(MotionTag.Grounded);
            }

            // 3. Gather input
            if (_inputProvider != null)
            {
                _context.Input = _inputProvider.GetInput();
            }

            // 4. Reset per-frame context values
            _context.GravityScale = 1f;
            _context.SteeringMultiplier = 1f;

            // 5. Route requests through abilities, then tick abilities
            RouteRequestsAndTickAbilities();

            // 6. Build runtime config
            BuildConfig();

            // 7. Execute pipeline
            _pipeline.Execute(_context, _config);

            // 8. Apply to rigidbody
            _motor.ApplyVelocity(_context);

            // 9. Clear request buffers
            _requestBuffer.Clear();
            _unconsumedBuffer.Clear();

            // 10. Cleanup expired temporal modifiers
            CleanupExpiredModifiers();
        }

        #endregion

        #region Public API — Requests

        /**
         * <summary>
         * Queues a motion request for processing on the next fixed tick.
         * All requests are routed through abilities — the orchestrator has no ability-specific logic.
         * </summary>
         *
         * <param name="request">The request to queue.</param>
         */
        public void Request(MotionRequest request)
        {
            _requestBuffer.Add(request);
        }

        /**
         * <summary>
         * Convenience overload: queues a request by type string.
         * </summary>
         *
         * <param name="type">Request type. Use <see cref="MotionRequestType"/> constants or custom strings.</param>
         */
        public void Request(string type) => Request(new MotionRequest(type));

        /**
         * <summary>
         * Convenience overload: queues a directional request.
         * </summary>
         *
         * <param name="type">Request type.</param>
         * <param name="direction">Direction for the request.</param>
         */
        public void Request(string type, Vector3 direction) =>
            Request(new MotionRequest(type) { Direction = direction });

        #endregion

        #region Public API — Forces

        /**
         * <summary>
         * Adds an external force. Integrated by the ExternalForceStage as F*dt/m.
         * </summary>
         */
        public void AddForce(Vector3 force)
        {
            _context.AccumulatedForce += force;
        }

        /**
         * <summary>
         * Adds an immediate velocity impulse (bypasses force integration).
         * </summary>
         */
        public void AddImpulse(Vector3 impulse)
        {
            _context.Velocity += impulse;
        }

        #endregion

        #region Public API — Configuration

        /** <summary>Sets the input provider for continuous input data.</summary> */
        public void SetInputProvider(IMotionInputProvider provider)
        {
            _inputProvider = provider;
        }

        /** <summary>Switches the active movement profile.</summary> */
        public void SetProfile(MovementProfile profile)
        {
            _activeProfile = profile;
        }

        /** <summary>Registers an ability. Abilities persist across frames and are ticked automatically.</summary> */
        public void RegisterAbility(IMotionAbility ability)
        {
            _abilities.Add(ability);
        }

        /**
         * <summary>Unregisters an ability by type. Deactivates it first if active.</summary>
         * <typeparam name="T">The ability type to remove.</typeparam>
         */
        public bool UnregisterAbility<T>() where T : IMotionAbility
        {
            for (int i = 0; i < _abilities.Count; i++)
            {
                if (_abilities[i] is T)
                {
                    if (_abilities[i].IsActive) _abilities[i].Deactivate(_context);
                    _abilities.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /** <summary>Adds a persistent modifier.</summary> */
        public void AddModifier(IMotionModifier modifier)
        {
            _modifiers.Add(modifier);
            SortModifiers();
        }

        /** <summary>Removes a persistent modifier.</summary> */
        public bool RemoveModifier(IMotionModifier modifier) => _modifiers.Remove(modifier);

        /** <summary>Adds a time-limited modifier that auto-expires.</summary> */
        public void AddTemporalModifier(IMotionModifier modifier, float duration)
        {
            _modifiers.Add(new TemporalModifier(modifier, duration));
            SortModifiers();
        }

        /** <summary>Adds a persistent constraint.</summary> */
        public void AddConstraint(IMotionConstraint constraint)
        {
            _constraints.Add(constraint);
            SortConstraints();
        }

        /** <summary>Removes a persistent constraint.</summary> */
        public bool RemoveConstraint(IMotionConstraint constraint) => _constraints.Remove(constraint);

        /** <summary>Creates a fluent builder for composing runtime configurations.</summary> */
        public MovementProfileBuilder CreateBuilder() => new MovementProfileBuilder(_config);

        #endregion

        #region Internal Methods

        /**
         * <summary>
         * Routes requests through abilities using the two-pass intercept/activate pattern,
         * then ticks all active abilities.
         * </summary>
         */
        private void RouteRequestsAndTickAbilities()
        {
            for (int r = 0; r < _requestBuffer.Count; r++)
            {
                bool consumed = false;
                for (int a = 0; a < _abilities.Count; a++)
                {
                    if (_abilities[a].IsActive && _abilities[a].TryConsumeRequest(_context, _requestBuffer[r]))
                    {
                        consumed = true;
                        break;
                    }
                }

                if (!consumed)
                {
                    _unconsumedBuffer.Add(_requestBuffer[r]);
                }
            }

            // Pass 2: Tick active abilities, check activation for inactive abilities
            for (int i = 0; i < _abilities.Count; i++)
            {
                var ability = _abilities[i];

                if (ability.IsActive)
                {
                    ability.Tick(_context, _context.DeltaTime);
                }
                else
                {
                    if (ability.CanActivate(_context, _unconsumedBuffer))
                    {
                        ability.Activate(_context);
                    }
                }
            }
        }

        private void BuildConfig()
        {
            if (_activeProfile != null)
            {
                _config.LoadFromProfile(_activeProfile);
            }
            else
            {
                _config.Steering = SteeringSettings.Default;
                _config.GravityScale = 1f;
                _config.Friction = 8f;
                _config.AirControl = 0.4f;
                _config.ActiveModifiers.Clear();
                _config.ActiveConstraints.Clear();
                _config.ActiveAbilities.Clear();
            }

            for (int i = 0; i < _modifiers.Count; i++)
                _config.ActiveModifiers.Add(_modifiers[i]);

            for (int i = 0; i < _constraints.Count; i++)
                _config.ActiveConstraints.Add(_constraints[i]);

            for (int i = 0; i < _abilities.Count; i++)
                _config.ActiveAbilities.Add(_abilities[i]);
        }

        private void CleanupExpiredModifiers()
        {
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (_modifiers[i] is TemporalModifier temporal && temporal.IsExpired)
                {
                    _modifiers.RemoveAt(i);
                }
            }
        }

        private void SortModifiers()
        {
            _modifiers.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        private void SortConstraints()
        {
            _constraints.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        #endregion
    }
}
