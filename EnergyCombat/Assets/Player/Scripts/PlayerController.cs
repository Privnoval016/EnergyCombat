using DynamicPhysics;
using Player.Config;
using StateMachine;
using Systems.Input;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region Variables

    #region Components

    [Header("References")]
    [SerializeField] private MotionOrchestrator motionOrchestrator;
    [SerializeField] private Transform movementOrientation;
    [SerializeField] private PlayerLocomotionConfig locomotionConfig;

    private PlayerInputAdapter _playerInputAdapter;
    private MotionInputProviderAdapter _motionInputProvider;
    private bool _jumpPressed;
    private bool _dodgePressed;

    public StateMachine<PlayerController> StateMachine { get; private set; }
    public MotionOrchestrator MotionOrchestrator => motionOrchestrator;

    #endregion

    #endregion

    #region Locomotion State Queries

    public bool IsGrounded => motionOrchestrator?.IsGrounded ?? false;
    public float VerticalVelocity => motionOrchestrator?.Velocity.y ?? 0f;
    public Vector2 MoveInput => _playerInputAdapter?.Snapshot.Move ?? Vector2.zero;
    public float MoveMagnitude => MoveInput.magnitude;
    public bool IsSprintToggled => _playerInputAdapter?.Snapshot.SprintToggled ?? false;

    public bool IsSliding => motionOrchestrator?.IsSlidingCrouch ?? false;

    public bool IsDodging => motionOrchestrator?.IsDashing ?? false;

    public bool HasDirectionalMoveInput => MoveMagnitude >= InputThresholds.MoveInputThreshold;

    public bool IsFullThrottleMove => MoveMagnitude >= InputThresholds.FullThrottleThreshold;

    public bool ShouldSprint => IsSprintToggled && HasDirectionalMoveInput && IsFullThrottleMove;

    public bool ConsumeJumpPressed()
    {
        bool pressed = _jumpPressed;
        _jumpPressed = false;
        Debug.Log("Jump pressed consumed: " + pressed);
        return pressed;
    }

    public bool ConsumeDodgePressed()
    {
        bool pressed = _dodgePressed;
        _dodgePressed = false;
        return pressed;
    }

    public void CancelSprintToggle()
    {
        _playerInputAdapter?.CancelSprintToggle();
    }

    public bool ShouldSlideFromDodgeIntent()
    {
        return !HasDirectionalMoveInput || ShouldSprint;
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

        if (movementOrientation == null && Camera.main != null)
        {
            movementOrientation = Camera.main.transform;
        }

        InitializeInputAndOrchestration();
        StateMachine = PlayerStateConstructor.Build(this);
        
        StateMachine.Start();
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
    }

    private void RegisterMovementAbilities()
    {

        motionOrchestrator.RegisterAbility(new JumpAbility(locomotionConfig.Jump, locomotionConfig.MovementProfile));

        motionOrchestrator.RegisterAbility(new DashAbility(locomotionConfig.Dash));

        motionOrchestrator.RegisterAbility(new SlideAbility(locomotionConfig.Slide));
    }

    private void HandleButtonEvent(PlayerInputButtonEvent buttonEvent)
    {
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