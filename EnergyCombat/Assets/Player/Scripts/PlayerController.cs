using Combat;
using Combat.Targeting;
using DynamicPhysics;
using Player.Config;
using StateMachine;
using Systems.Input;
using UnityEngine;

public class PlayerController : MonoBehaviour, ILocomotionState
{
    #region Variables

    #region Components

    [Header("References")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private MotionOrchestrator motionOrchestrator;
    [SerializeField] private Transform movementOrientation;
    [SerializeField] private PlayerLocomotionConfig locomotionConfig;

    [Header("Combat")]
    [SerializeField] private CombatController combatController;

    /**
     * <summary>
     * Optional targeting system. When assigned, the player will automatically
     * soft-target nearby enemies and pass the selected target to the combat controller.
     * </summary>
     */
    [Tooltip("SoftTargetingSystem on this GameObject. Wires targeting into combat on startup.")]
    [SerializeField] private SoftTargetingSystem _softTargeting;

    [Header("Animation")]
    [SerializeField] private PlayerAnimationController _animationController;

    private PlayerInputAdapter _playerInputAdapter;
    private MotionInputProviderAdapter _motionInputProvider;
    private bool _jumpPressed;
    private bool _dodgePressed;
    private bool _sprintPressed;
    private bool _landingBoostPending;

    // Chord detection — tracks when each combat button was last pressed so PlayerController
    // can synthesise a LightHeavyChord event when both arrive within ChordDetectionWindow.
    private float _lastLightPressTime = float.NegativeInfinity;
    private float _lastHeavyPressTime = float.NegativeInfinity;
    private bool  _chordActive        = false;

    // Sprint-light deferred push — when L is pressed while sprinting we can't know at press
    // time whether it's a tap (basic attack) or hold (running attack). The Started event is
    // held here and pushed only once intent is clear: on release = tap, after threshold = hold.
    private bool  _pendingSprintLight   = false;
    private float _sprintLightPressTime = float.NegativeInfinity;

    public StateMachine<PlayerController> StateMachine { get; private set; }
    public MotionOrchestrator MotionOrchestrator => motionOrchestrator;
    public CameraController CameraController => cameraController;
    public CombatController CombatController => combatController;
    public PlayerAnimationController AnimationController => _animationController;

    #endregion

    #endregion

    #region Locomotion State Queries

    public bool IsGrounded => motionOrchestrator?.IsGrounded ?? false;
    public bool IsAirborne => !(motionOrchestrator?.IsGrounded ?? true);
    public bool IsSprinting => motionOrchestrator?.IsSprinting ?? false;
    public bool IsDodging => motionOrchestrator?.IsDashing ?? false;

    public float VerticalVelocity => motionOrchestrator?.Velocity.y ?? 0f;
    public Vector2 MoveInput => _playerInputAdapter?.Snapshot.Move ?? Vector2.zero;
    public float MoveMagnitude => MoveInput.magnitude;
    public bool IsSprintToggled => IsSprinting;

    public bool IsSliding => motionOrchestrator?.IsSlidingCrouch ?? false;
    public bool IsWallRunning => motionOrchestrator?.Context?.HasTag(MotionTag.WallRunning) ?? false;
    public Vector3 WallNormal => motionOrchestrator?.Context?.WallNormal ?? Vector3.zero;
    public bool IsLedgeGrabbing => motionOrchestrator?.Context?.HasTag(MotionTag.LedgeGrabbing) ?? false;
    public bool IsWallKicking => motionOrchestrator?.Context?.HasTag(MotionTag.WallKicking) ?? false;

    /**
     * <summary>
     * <c>true</c> while the physics layer is executing or has recently completed a hard-stop
     * momentum reversal. Remains active for <see cref="SteeringSettings.QuickTurnHoldDuration"/>
     * seconds after the physics condition clears so the full turn animation can play.
     * </summary>
     */
    public bool IsQuickTurning => motionOrchestrator?.Context?.HasTag(MotionTag.QuickTurning) ?? false;

    /**
     * <summary>
     * Signed direction of the most recent quick turn.
     * Positive values indicate a right turn; negative values indicate a left turn.
     * Only meaningful while <see cref="IsQuickTurning"/> is <c>true</c>.
     * </summary>
     */
    public float QuickTurnSign => motionOrchestrator?.Context?.QuickTurnSign ?? 0f;

    /**
     * <summary>
     * <c>true</c> while <c>CombatController</c> is actively executing an ability pipeline.
     * Used by the state machine to enter and remain in <c>AttackingState</c>.
     * </summary>
     */
    public bool IsAttacking => combatController?.IsExecuting ?? false;

    /** <summary><c>true</c> when the soft targeting system has an active target.</summary> */
    public bool HasTarget => _softTargeting?.HasTarget ?? false;

    /** <summary>The currently soft-targeted point, or <c>null</c>.</summary> */
    public ITargetable CurrentTarget => _softTargeting?.CurrentTarget;

    public bool HasDirectionalMoveInput => MoveMagnitude >= InputThresholds.MoveInputThreshold;

    public bool IsFullThrottleMove => MoveMagnitude >= InputThresholds.FullThrottleThreshold;

    public bool ShouldSprint => IsSprintToggled && HasDirectionalMoveInput && IsFullThrottleMove;

    public bool ConsumeJumpPressed()
    {
        bool pressed = _jumpPressed;
        _jumpPressed = false;
        return pressed;
    }

    public bool ConsumeDodgePressed()
    {
        bool pressed = _dodgePressed;
        _dodgePressed = false;
        return pressed;
    }

    public bool ConsumeSprintPressed()
    {
        bool pressed = _sprintPressed;
        _sprintPressed = false;
        return pressed;
    }

    public bool ShouldSlideFromDodgeIntent()
    {
        return !HasDirectionalMoveInput || ShouldSprint;
    }

    /** <summary>Marks that a landing dash boost should fire on the next grounded landing.</summary> */
    public void SetLandingBoostPending() => _landingBoostPending = true;

    /** <summary>Returns <c>true</c> and clears the flag if a landing boost was pending.</summary> */
    public bool ConsumeLandingBoostPending()
    {
        bool v = _landingBoostPending;
        _landingBoostPending = false;
        return v;
    }

    /**
     * <summary>
     * Returns <c>true</c> if post-state dash boost conditions are met: boost is enabled,
     * the player has directional input, and that input is within
     * <see cref="PostStateBoostSettings.AngleThreshold"/> degrees of the character's forward.
     * </summary>
     */
    public bool ShouldApplyPostBoost()
    {
        if (locomotionConfig == null) return false;
        var cfg = locomotionConfig.PostStateBoost;
        if (!cfg.Enabled) return false;
        if (!HasDirectionalMoveInput) return false;
        return Vector3.Angle(transform.forward, ComputeWorldMoveDirection(MoveInput)) <= cfg.AngleThreshold;
    }

    public void RequestSprint()
    {
        motionOrchestrator?.Request(MotionRequestType.Sprint);
    }

    public void RequestJump()
    {
        motionOrchestrator?.Request(MotionRequestType.Jump);
    }

    public void RequestDashFromMoveInput()
    {
        motionOrchestrator?.Request(MotionRequestType.Dash, ComputeWorldMoveDirection(MoveInput));
    }

    public void RequestSlide()
    {
        motionOrchestrator?.Request(MotionRequestType.Slide);
    }

    public TState GetState<TState>() where TState : State<PlayerController>
    {
        return StateMachine.GetState<TState>() as TState;
    }

    #endregion

    #region Monobehaviour Callbacks

    private void Awake()
    {
        if (motionOrchestrator == null)
        {
            motionOrchestrator = GetComponent<MotionOrchestrator>();
        }
        
        if (cameraController == null)
        {
            cameraController = Camera.main?.GetComponent<CameraController>();
        }

        if (movementOrientation == null)
        {
            movementOrientation = cameraController?.transform;
        }

        InitializeInputAndOrchestration();
        StateMachine = new PlayerStateConstructor(this).Construct();
        
        StateMachine.Start();
    }

    private void Update()
    {
        TickSprintLightBuffer();
    }

    private void OnEnable()
    {
        _playerInputAdapter?.Enable();
    }

    private void OnDisable()
    {
        _playerInputAdapter?.Disable();
    }

    private void OnDestroy()
    {
        if (_playerInputAdapter != null)
        {
            _playerInputAdapter.ButtonEvent -= HandleButtonEvent;
            _playerInputAdapter.Dispose();
            _playerInputAdapter = null;
        }
    }

    #endregion

    #region Internal Initialization

    private void InitializeInputAndOrchestration()
    {
        if (motionOrchestrator == null)
        {
            Debug.LogError("PlayerController requires a MotionOrchestrator reference.");
            return;
        }

        _playerInputAdapter = new PlayerInputAdapter();
        _motionInputProvider = new MotionInputProviderAdapter(_playerInputAdapter, movementOrientation);

        _playerInputAdapter.ButtonEvent += HandleButtonEvent;
        motionOrchestrator.SetInputProvider(_motionInputProvider);
        if (locomotionConfig != null && locomotionConfig.MovementProfile != null)
        {
            motionOrchestrator.SetProfile(locomotionConfig.MovementProfile);
        }

        RegisterMovementAbilities();

        // Wire targeting into combat and physics
        if (_softTargeting != null && combatController != null)
            combatController.SetTargetProvider(_softTargeting);

        if (combatController != null)
        {
            motionOrchestrator.RegisterAbility(new CombatMovementAbility(combatController, motionOrchestrator));
            motionOrchestrator.RegisterAbility(new AerialCombatAbility(combatController, motionOrchestrator));
        }

        if (_softTargeting != null)
            motionOrchestrator.AddPipelineStage(new CombatTargetFacingStage(_softTargeting, this,
                combatController?.InputSettings));
    }

    private void RegisterMovementAbilities()
    {

        motionOrchestrator.RegisterAbility(new JumpAbility(locomotionConfig.Jump, locomotionConfig.MovementProfile));
        motionOrchestrator.RegisterAbility(new DashAbility(locomotionConfig.Dash));
        motionOrchestrator.RegisterAbility(new SlideAbility(locomotionConfig.Slide));
        motionOrchestrator.RegisterAbility(new SprintAbility());
        motionOrchestrator.RegisterAbility(new WallRunAbility(locomotionConfig.WallRun));
        motionOrchestrator.RegisterAbility(new WallKickAbility(locomotionConfig.WallKick, locomotionConfig.Jump, locomotionConfig.MovementProfile));
        motionOrchestrator.RegisterAbility(new LedgeGrabAbility(locomotionConfig.LedgeGrab));
    }

    private void HandleButtonEvent(PlayerInputButtonEvent buttonEvent)
    {
        HandleCombatInput(buttonEvent);

        if (buttonEvent.Phase != PlayerInputPhase.Performed)
        {
            return;
        }

        if (buttonEvent.Button == PlayerInputButton.Jump)
        {
            _jumpPressed = true;
        }
        else if (buttonEvent.Button == PlayerInputButton.Dodge)
        {
            _dodgePressed = true;
        }
        else if (buttonEvent.Button == PlayerInputButton.Sprint)
        {
            _sprintPressed = true;
        }
    }

    private void HandleCombatInput(PlayerInputButtonEvent buttonEvent)
    {
        if (combatController == null) return;

        CombatInputButton? combatButton = buttonEvent.Button switch
        {
            PlayerInputButton.LightAttack => CombatInputButton.LightAttack,
            PlayerInputButton.HeavyAttack => CombatInputButton.HeavyAttack,
            _ => null
        };

        if (combatButton == null) return;

        CombatInputPhase phase = buttonEvent.Phase switch
        {
            PlayerInputPhase.Started   => CombatInputPhase.Started,
            PlayerInputPhase.Performed => CombatInputPhase.Performed,
            PlayerInputPhase.Canceled  => CombatInputPhase.Canceled,
            _ => CombatInputPhase.Performed
        };

        float now = UnityEngine.Time.unscaledTime;

        // Record press times for chord detection — but ONLY when L is not about to be deferred.
        // If _lastLightPressTime were set for a deferred L, a subsequent H within the chord window
        // would falsely fire a chord even though L was never pushed to the combat buffer.
        if (phase == CombatInputPhase.Started)
        {
            bool lightWillBeDeferred = combatButton == CombatInputButton.LightAttack && IsSprinting;

            if (combatButton == CombatInputButton.LightAttack && !lightWillBeDeferred)
                _lastLightPressTime = now;
            if (combatButton == CombatInputButton.HeavyAttack)
                _lastHeavyPressTime = now;

            // Chord detection is skipped when L is being deferred. Sprint+L then H within the
            // window is treated as a solo H attack, not a chord.
            if (!lightWillBeDeferred)
            {
                float window = combatController.InputSettings?.ChordDetectionWindow ?? 0.08f;
                if (!_chordActive
                    && now - _lastLightPressTime < window
                    && now - _lastHeavyPressTime < window)
                {
                    _chordActive        = true;
                    _pendingSprintLight = false;

                    combatController.InputBuffer.ConsumeInput(CombatInputButton.LightAttack);
                    combatController.InputBuffer.ConsumeInput(CombatInputButton.HeavyAttack);

                    if (combatController.IsExecuting)
                    {
                        var runningInput = combatController.ActiveContext?.Ability?.PrimaryInput;
                        if (runningInput == CombatInputButton.LightAttack
                            || runningInput == CombatInputButton.HeavyAttack)
                            combatController.CancelCurrentAbility();
                    }

                    combatController.PushInput(new CombatInputEvent(
                        CombatInputButton.LightHeavyChord,
                        CombatInputPhase.Started,
                        now));
                    return; // Prevent the individual button press from also reaching the buffer.
                }
            }
        }

        // Suppress Performed hold-continuations for constituent buttons while chord is active.
        // If a constituent's Started was blocked by the chord return, the buffer's IsHeld stays
        // false. A Performed from the Input System would then be treated as a fresh press
        // (Performed when !IsHeld resets ConsumedTime = -inf), making HasRecentInput true for a
        // follow-up attack after the chord completes.
        if (phase == CombatInputPhase.Performed && _chordActive
            && (combatButton == CombatInputButton.LightAttack
                || combatButton == CombatInputButton.HeavyAttack))
            return;

        // Mirror release: cancel the chord the moment either constituent button is released so
        // GetHoldDuration(LightHeavyChord) stops accumulating, enabling hold-chord abilities.
        if (phase == CombatInputPhase.Canceled && _chordActive)
        {
            _chordActive = false;
            combatController.PushInput(new CombatInputEvent(
                CombatInputButton.LightHeavyChord,
                CombatInputPhase.Canceled,
                now));
        }

        // Sprint-light disambiguation: defer pushing LightAttack.Started while sprinting until
        // we know if it's a tap (basic attack) or hold (running attack).
        if (combatButton == CombatInputButton.LightAttack)
        {
            if (phase == CombatInputPhase.Started && IsSprinting)
            {
                _pendingSprintLight   = true;
                _sprintLightPressTime = now;
                return; // Pushed later by TickSprintLightBuffer or on release.
            }

            // Suppress hold-continuation events while the tap/hold decision is pending.
            // Without this, the Performed event would register an immediate press in the buffer
            // and CombatController.Update would resolve LAtk_1 before the threshold elapses.
            if (phase == CombatInputPhase.Performed && _pendingSprintLight)
                return;

            if (phase == CombatInputPhase.Canceled && _pendingSprintLight)
            {
                // Released before hold threshold — it was a tap. Resolve basic attack.
                _pendingSprintLight = false;
                _lastLightPressTime = _sprintLightPressTime; // Record so future chords see the real press time.
                combatController.PushInput(new CombatInputEvent(
                    CombatInputButton.LightAttack, CombatInputPhase.Started, _sprintLightPressTime));
                // Fall through to also push Canceled so IsHeld is cleared correctly.
            }
        }

        combatController.PushInput(new CombatInputEvent(combatButton.Value, phase, now));
    }

    private void TickSprintLightBuffer()
    {
        if (!_pendingSprintLight || combatController == null) return;
        float threshold = combatController.InputSettings?.SprintLightHoldThreshold ?? 0.2f;
        if (Time.unscaledTime - _sprintLightPressTime >= threshold)
        {
            // Held long enough — push with original timestamp so GetHoldDuration reflects actual
            // hold duration and the running attack's RequireHold check evaluates correctly.
            _lastLightPressTime = _sprintLightPressTime; // Record so future chords see the real press time.
            combatController.PushInput(new CombatInputEvent(
                CombatInputButton.LightAttack, CombatInputPhase.Started, _sprintLightPressTime));
            _pendingSprintLight = false;
        }
    }

    private InputThresholdSettings InputThresholds =>
        locomotionConfig != null ? locomotionConfig.InputThresholds : InputThresholdSettings.Default;

    private Vector3 ComputeWorldMoveDirection(Vector2 moveInput)
    {
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        if (movementOrientation != null)
        {
            forward = Vector3.ProjectOnPlane(movementOrientation.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(movementOrientation.right, Vector3.up).normalized;
        }

        Vector3 worldDirection = (forward * moveInput.y) + (right * moveInput.x);
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude > 0.0001f)
        {
            worldDirection.Normalize();
        }

        return worldDirection;
    }

    #endregion
}