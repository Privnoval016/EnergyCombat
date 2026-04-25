using System.Collections.Generic;
using DynamicPhysics.Abilities;
using DynamicPhysics.Constraints;
using DynamicPhysics.Core;
using DynamicPhysics.Input;
using DynamicPhysics.Motor;
using DynamicPhysics.Pipeline;
using DynamicPhysics.Pipeline.Modifiers;
using DynamicPhysics.Pipeline.Stages;
using DynamicPhysics.Profiles;
using UnityEngine;

namespace DynamicPhysics.Orchestration
{
    /**
     * <summary>
     * Primary MonoBehaviour component that drives the DynamicPhysics locomotion engine.
     * Attach this to any character GameObject with a <see cref="Rigidbody"/>.
     *
     * Manages the full lifecycle: initializes the motion pipeline from a profile,
     * ticks all stages in FixedUpdate, and exposes a high-level API for gameplay code.
     * </summary>
     *
     * <remarks>
     * <para>
     * Gameplay code interacts with this component using simple commands (RequestJump, RequestDash, etc.)
     * that are converted into pipeline configurations. No external system should directly modify
     * the Rigidbody — all physics interaction is channeled through this orchestrator.
     * </para>
     *
     * <para>
     * The orchestrator owns:
     * <list type="bullet">
     *   <item>The <see cref="MotionPipeline"/> with all processing stages.</item>
     *   <item>The <see cref="PhysicsMotor"/> (sole Rigidbody accessor).</item>
     *   <item>The <see cref="GroundDetector"/> for terrain sensing.</item>
     *   <item>Registered <see cref="IMotionAbility"/> instances.</item>
     *   <item>Active <see cref="IMotionModifier"/> and <see cref="IMotionConstraint"/> lists.</item>
     * </list>
     * </para>
     * </remarks>
     */
    [RequireComponent(typeof(Rigidbody))]
    public class MotionOrchestrator : MonoBehaviour
    {
        #region Serialized Fields

        /** <summary>Default movement profile loaded on initialization.</summary> */
        [Header("Profile")]
        [Tooltip("Default movement profile to use. Defines baseline tuning.")]
        [SerializeField] private MovementProfile _defaultProfile;

        /** <summary>Ground detection configuration.</summary> */
        [Header("Ground Detection")]
        [SerializeField] private GroundDetector _groundDetector = new();

        #endregion

        #region Runtime State

        private Rigidbody _rigidbody;
        private PhysicsMotor _motor;
        private MotionPipeline _pipeline;
        private MotionContext _context;
        private RuntimeMotionConfig _config;
        private MovementProfile _activeProfile;

        // Abilities
        private readonly List<IMotionAbility> _abilities = new(8);

        // Modifiers & constraints (persistent, not per-frame)
        private readonly List<IMotionModifier> _modifiers = new(16);
        private readonly List<IMotionConstraint> _constraints = new(8);

        // Request buffer (fixed-size array to avoid allocations)
        private readonly MotionRequest[] _requestBuffer = new MotionRequest[16];
        private int _requestCount;

        // Input
        private IMotionInputProvider _inputProvider;

        // Cached references for wall run → jump interaction
        private WallRunAbility _wallRunAbility;

        #endregion

        #region Properties

        /** <summary>The shared motion context. Read-only access for debugging and external queries.</summary> */
        public MotionContext Context => _context;

        /** <summary>Whether the character is currently grounded.</summary> */
        public bool IsGrounded => (_context.Flags & MotionFlags.Grounded) != 0;

        /** <summary>Current velocity of the character.</summary> */
        public Vector3 Velocity => _context.Velocity;

        /** <summary>The currently active movement profile.</summary> */
        public MovementProfile ActiveProfile => _activeProfile;

        #endregion

        #region MonoBehaviour Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            // Initialize motor
            _motor = new PhysicsMotor();
            _motor.Initialize(_rigidbody);

            // Initialize context and config (allocated once, reused)
            _context = new MotionContext();
            _config = new RuntimeMotionConfig();

            // Initialize pipeline with default stages
            _pipeline = new MotionPipeline();
            _pipeline.AddStage(new InputSteeringStage());
            _pipeline.AddStage(new AbilityInfluenceStage());
            _pipeline.AddStage(new ExternalForceStage());
            _pipeline.AddStage(new GravityStage());
            _pipeline.AddStage(new ModifierStage());
            _pipeline.AddStage(new ConstraintStage());

            // Load default profile
            if (_defaultProfile != null)
            {
                _activeProfile = _defaultProfile;
            }

            // Add default ground snap constraint
            _constraints.Add(new GroundSnapConstraint());
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // 1. Snapshot current physics state
            _motor.SnapshotState(_context);
            _context.DeltaTime = dt;

            // 2. Ground detection
            _groundDetector.Detect(_context.Position);
            _context.GroundNormal = _groundDetector.GroundNormal;
            _context.GroundPoint = _groundDetector.GroundPoint;
            _context.GroundAngle = _groundDetector.GroundAngle;

            // Set grounded / airborne flags
            if (_groundDetector.IsGrounded)
            {
                _context.Flags |= MotionFlags.Grounded;
                _context.Flags &= ~MotionFlags.Airborne;
            }
            else
            {
                _context.Flags |= MotionFlags.Airborne;
                _context.Flags &= ~MotionFlags.Grounded;
            }

            // 3. Gather input
            if (_inputProvider != null)
            {
                _context.Input = _inputProvider.GetInput();
            }

            // 4. Reset per-frame context values
            _context.GravityScale = 1f;
            _context.SteeringMultiplier = 1f;

            // 5. Tick abilities (check activation, tick active ones)
            TickAbilities();

            // 6. Build runtime config
            BuildConfig();

            // 7. Execute pipeline
            _pipeline.Execute(_context, _config);

            // 8. Apply to rigidbody
            _motor.ApplyVelocity(_context);

            // 9. Clear requests
            _requestCount = 0;

            // 10. Cleanup expired temporal modifiers
            CleanupExpiredModifiers();
        }

        #endregion

        #region Public API — Requests

        /**
         * <summary>
         * Requests a jump. The <see cref="JumpAbility"/> (if registered) will consume this.
         * Supports jump buffering — the request persists for the ability's buffer window.
         * </summary>
         */
        public void RequestJump()
        {
            // Check for wall jump
            if (_wallRunAbility != null && _wallRunAbility.IsActive)
            {
                _wallRunAbility.WallJump(_context);
                return;
            }

            QueueRequest(new MotionRequest(MotionRequestType.Jump));
        }

        /**
         * <summary>
         * Requests a dash in the given direction.
         * </summary>
         *
         * <param name="direction">Desired dash direction. If zero, uses current input or character forward.</param>
         */
        public void RequestDash(Vector3 direction = default)
        {
            var request = new MotionRequest(MotionRequestType.Dash) { Direction = direction };
            QueueRequest(request);
        }

        /**
         * <summary>
         * Requests a slide.
         * </summary>
         */
        public void RequestSlide()
        {
            QueueRequest(new MotionRequest(MotionRequestType.Slide));
        }

        /**
         * <summary>
         * Attaches to a <see cref="RopeEffector"/> and begins swinging.
         * </summary>
         *
         * <param name="effector">The rope effector to attach to.</param>
         */
        public void AttachRope(RopeEffector effector)
        {
            var request = new MotionRequest(MotionRequestType.RopeAttach) { Target = effector };
            QueueRequest(request);
        }

        /**
         * <summary>
         * Detaches from the current rope.
         * </summary>
         */
        public void DetachRope()
        {
            // Find and deactivate the rope swing ability directly
            for (int i = 0; i < _abilities.Count; i++)
            {
                if (_abilities[i] is RopeSwingAbility rope && rope.IsActive)
                {
                    rope.Deactivate(_context);
                    break;
                }
            }
        }

        /**
         * <summary>
         * Adds an external force (knockback, explosion, wind). Integrated by the ExternalForceStage.
         * </summary>
         *
         * <param name="force">Force vector in world space.</param>
         */
        public void AddForce(Vector3 force)
        {
            _context.AccumulatedForce += force;
        }

        /**
         * <summary>
         * Adds an immediate velocity impulse (bypasses force integration).
         * Use for instant knockbacks or launch effects.
         * </summary>
         *
         * <param name="impulse">Velocity to add directly.</param>
         */
        public void AddImpulse(Vector3 impulse)
        {
            _context.Velocity += impulse;
        }

        #endregion

        #region Public API — Configuration

        /**
         * <summary>
         * Sets the input provider for continuous input data.
         * </summary>
         *
         * <param name="provider">The input provider implementation.</param>
         */
        public void SetInputProvider(IMotionInputProvider provider)
        {
            _inputProvider = provider;
        }

        /**
         * <summary>
         * Switches the active movement profile. Takes effect on the next fixed tick.
         * </summary>
         *
         * <param name="profile">The profile to switch to.</param>
         */
        public void SetProfile(MovementProfile profile)
        {
            _activeProfile = profile;
        }

        /**
         * <summary>
         * Registers an ability with the orchestrator. Abilities persist across frames
         * and are ticked automatically.
         * </summary>
         *
         * <param name="ability">The ability to register.</param>
         */
        public void RegisterAbility(IMotionAbility ability)
        {
            _abilities.Add(ability);

            // Cache wall run ability for jump interaction
            if (ability is WallRunAbility wallRun)
            {
                _wallRunAbility = wallRun;
            }
        }

        /**
         * <summary>
         * Unregisters an ability by type. Deactivates it first if active.
         * </summary>
         *
         * <typeparam name="T">The ability type to remove.</typeparam>
         * <returns>True if an ability was found and removed.</returns>
         */
        public bool UnregisterAbility<T>() where T : IMotionAbility
        {
            for (int i = 0; i < _abilities.Count; i++)
            {
                if (_abilities[i] is T)
                {
                    if (_abilities[i].IsActive) _abilities[i].Deactivate(_context);
                    if (_abilities[i] is WallRunAbility) _wallRunAbility = null;
                    _abilities.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /**
         * <summary>
         * Adds a persistent modifier that applies every tick until removed.
         * </summary>
         *
         * <param name="modifier">The modifier to add.</param>
         */
        public void AddModifier(IMotionModifier modifier)
        {
            _modifiers.Add(modifier);
            SortModifiers();
        }

        /**
         * <summary>
         * Removes a persistent modifier.
         * </summary>
         *
         * <param name="modifier">The modifier to remove.</param>
         * <returns>True if the modifier was found and removed.</returns>
         */
        public bool RemoveModifier(IMotionModifier modifier) => _modifiers.Remove(modifier);

        /**
         * <summary>
         * Adds a time-limited modifier that automatically expires.
         * </summary>
         *
         * <param name="modifier">The modifier to wrap.</param>
         * <param name="duration">Duration in seconds before expiration.</param>
         */
        public void AddTemporalModifier(IMotionModifier modifier, float duration)
        {
            _modifiers.Add(new TemporalModifier(modifier, duration));
            SortModifiers();
        }

        /**
         * <summary>
         * Adds a persistent constraint.
         * </summary>
         *
         * <param name="constraint">The constraint to add.</param>
         */
        public void AddConstraint(IMotionConstraint constraint)
        {
            _constraints.Add(constraint);
            SortConstraints();
        }

        /**
         * <summary>
         * Removes a persistent constraint.
         * </summary>
         *
         * <param name="constraint">The constraint to remove.</param>
         * <returns>True if the constraint was found and removed.</returns>
         */
        public bool RemoveConstraint(IMotionConstraint constraint) => _constraints.Remove(constraint);

        /**
         * <summary>
         * Creates a <see cref="MovementProfileBuilder"/> for composing runtime configurations.
         * </summary>
         *
         * <returns>A new builder wrapping the shared runtime config.</returns>
         */
        public MovementProfileBuilder CreateBuilder() => new MovementProfileBuilder(_config);

        #endregion

        #region Internal Methods

        private void QueueRequest(MotionRequest request)
        {
            if (_requestCount < _requestBuffer.Length)
            {
                _requestBuffer[_requestCount++] = request;
            }
        }

        private void TickAbilities()
        {
            for (int i = 0; i < _abilities.Count; i++)
            {
                var ability = _abilities[i];

                if (ability.IsActive)
                {
                    ability.Tick(_context, _context.DeltaTime);
                }
                else
                {
                    if (ability.CanActivate(_context, _requestBuffer, _requestCount))
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

            // Add persistent modifiers
            for (int i = 0; i < _modifiers.Count; i++)
            {
                _config.ActiveModifiers.Add(_modifiers[i]);
            }

            // Add persistent constraints
            for (int i = 0; i < _constraints.Count; i++)
            {
                _config.ActiveConstraints.Add(_constraints[i]);
            }

            // Add active abilities
            for (int i = 0; i < _abilities.Count; i++)
            {
                _config.ActiveAbilities.Add(_abilities[i]);
            }
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
