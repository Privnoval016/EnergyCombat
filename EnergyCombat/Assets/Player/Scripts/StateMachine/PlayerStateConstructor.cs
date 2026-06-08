using DynamicPhysics;
using StateMachine;
using UnityEngine;

/**
 * <summary>
 * Constructs the player's hierarchical state machine, wires all states, and attaches animation
 * activities. Looping locomotion states use <see cref="LoopAnimActivity"/>; one-shot states
 * (jump, fall, land, quick-turn, ledge, wall-kick) use <see cref="OneShotAnimActivity"/>.
 * </summary>
 */
public class PlayerStateConstructor
{
    private readonly State<PlayerController> _root;
    private readonly PlayerController _host;
    private readonly StateMachineBuilder<PlayerController> _builder;

    public PlayerStateConstructor(PlayerController host)
    {
        _host = host;
        _root = new RootState();
        _builder = new StateMachineBuilder<PlayerController>(_root);
    }

    public StateMachine<PlayerController> Construct()
    {
        ConstructActiveStates();
        return _builder.Build(_host);
    }

    private void ConstructActiveStates()
    {
        var ctrl = _host.AnimationController;
        var cfg  = ctrl?.Config;

        var active = new ActiveState().WithParent(_root).AsInitialState(_root);

        var grounded  = new GroundedState().AsInitialState(active);
        var airborne  = new AirborneState().WithParent(active);
        var attacking = new AttackingState().WithParent(active);
        var wallRun   = new WallRunState().WithParent(active);
        var wallKick  = new WallKickState().WithParent(active);
        var ledgeGrab = new LedgeGrabState().WithParent(active);
        var dash      = new DashState().WithParent(grounded);

        var idle      = new IdleState().AsInitialState(grounded);
        var moveState = new MoveState();
        var move      = moveState.WithParent(grounded);
        var sprint    = new SprintState().WithParent(grounded);
        var slide     = new SlideState().WithParent(grounded)
                            .WithActivity(new CameraActivity(_host.CameraController, CamMode.OverRightShoulder, 0.2f));
        static float ClipLen(OneShotAnimDef def) => def?.IsValid == true ? def.Clip.Clip.length : 0f;
        float landHold = cfg?.LandHoldDuration > 0f
            ? cfg.LandHoldDuration
            : Mathf.Max(ClipLen(cfg?.LandIdle), ClipLen(cfg?.LandMotion));
        var land      = new LandState(landHold).WithParent(grounded);
        var quickTurn = new QuickTurnState().WithParent(grounded);

        var jump = new JumpState().AsInitialState(airborne);
        var fall = new FallState().WithParent(airborne);

        // — Loop states —
        if (ctrl != null && cfg != null)
        {
            idle.WithActivity(new LoopAnimActivity(ctrl, cfg.Idle));
            move.WithActivity(new LoopAnimActivity(ctrl, cfg.Walk,
                onEnterStart: () => _host.MotionOrchestrator.Context.SetTag(MotionTag.WalkStarting),
                onEnterEnd:   () => _host.MotionOrchestrator.Context.RemoveTag(MotionTag.WalkStarting),
                skipEnter:    () => moveState.TimeSinceExit < cfg.WalkEnterSkipWindow));
            sprint.WithActivity(new LoopAnimActivity(ctrl, cfg.Sprint,
                skipEnter: () => moveState.TimeSinceExit < cfg.SprintEnterSkipWindow));
            dash.WithActivity(new LoopAnimActivity(ctrl, cfg.Dash));
            slide.WithActivity(new LoopAnimActivity(ctrl, cfg.Slide));

            // WallRun: Func<LoopAnimDef> resolves L/R side at activation time
            wallRun.WithActivity(new LoopAnimActivity(ctrl, () =>
            {
                bool wallOnRight = Vector3.Dot(_host.transform.right, _host.WallNormal) < 0;
                return wallOnRight ? cfg.WallRunRight : cfg.WallRunLeft;
            }));
        }

        // — One-shot states —
        if (ctrl != null && cfg != null)
        {
            jump.WithActivity(new OneShotAnimActivity(ctrl, cfg.Jump));
            fall.WithActivity(new OneShotAnimActivity(ctrl, cfg.Fall));
            wallKick.WithActivity(new OneShotAnimActivity(ctrl, cfg.WallKick));

            // Landing: state-based clip selection — sprint→LandMotion, idle→LandIdle, walk→null (no clip)
            land.WithActivity(new OneShotAnimActivity(ctrl, () =>
            {
                if (_host.ShouldSprint)             return cfg.LandMotion;
                if (!_host.HasDirectionalMoveInput) return cfg.LandIdle;
                return null;
            }));

            // Quick turn: direction-based clip selection at activation time; snaps rotation on exit
            quickTurn.WithActivity(new QuickTurnAnimationActivity(ctrl,
                () => _host.QuickTurnSign < 0f ? cfg.QuickTurnLeft : cfg.QuickTurnRight, _host));
        }

        // — LedgeGrab: two-phase hang → climb —
        var ledgeAbility = _host.MotionOrchestrator.GetAbility<LedgeGrabAbility>();
        if (ctrl != null && cfg != null)
        {
            ledgeGrab.WithActivity(new LedgeGrabAnimationActivity(
                ctrl, cfg.LedgeGrab, cfg.LedgeClimb, ledgeAbility));
        }

        // Airborne and immediate-action states suppress exit clips — transitions to these should be instant
        _builder.WithExitSkipPolicy((from, to) => to == jump || to == fall || to == dash || to == sprint || to == slide);

        _builder
            .WithState(active)
            .WithState(grounded)
            .WithState(airborne)
            .WithState(attacking)
            .WithState(wallRun)
            .WithState(wallKick)
            .WithState(ledgeGrab)
            .WithState(dash)
            .WithState(idle)
            .WithState(move)
            .WithState(sprint)
            .WithState(slide)
            .WithState(jump)
            .WithState(fall)
            .WithState(land)
            .WithState(quickTurn);
    }
}
