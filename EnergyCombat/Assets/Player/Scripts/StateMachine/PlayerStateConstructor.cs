using DynamicPhysics;
using StateMachine;
using UnityEngine;

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

        var machine = _builder.Build(_host);
        return machine;
    }

    private void ConstructActiveStates()
    {
        var active = new ActiveState().WithParent(_root).AsInitialState(_root);

        var grounded = new GroundedState().AsInitialState(active);
        var airborne = new AirborneState().WithParent(active);
        var attacking = new AttackingState().WithParent(active);
        var wallRun = new WallRunState().WithParent(active);
        var wallKick = new WallKickState().WithParent(active);
        var ledgeGrab = new LedgeGrabState().WithParent(active);
        var dash = new DashState().WithParent(grounded);

        var idle = new IdleState().AsInitialState(grounded);
        var move = new MoveState().WithParent(grounded);
        var sprint = new SprintState().WithParent(grounded);
        var slide = new SlideState().WithParent(grounded)
            .WithActivity(new CameraActivity(_host.CameraController, CamMode.OverRightShoulder, 0.2f));

        var jump = new JumpState().AsInitialState(airborne);
        var fall = new FallState().WithParent(airborne);

        // Animation activities — all null-safe; WithActivity(null) is already a no-op
        var ctrl = _host.AnimationController;
        var cfg  = ctrl?.Config;

        LoopAnimActivity Anim(StateAnimSet set) =>
            ctrl != null && set != null ? new LoopAnimActivity(ctrl, set) : null;

        idle.WithActivity(Anim(cfg?.Idle));
        move.WithActivity(Anim(cfg?.Walk));
        sprint.WithActivity(Anim(cfg?.Sprint));
        dash.WithActivity(Anim(cfg?.Dash));
        jump.WithActivity(Anim(cfg?.Jump));
        fall.WithActivity(Anim(cfg?.Fall));
        wallKick.WithActivity(Anim(cfg?.WallKick));

        // WallRun: Func<StateAnimSet> overload resolves L/R side at activation time
        wallRun.WithActivity(ctrl != null && cfg != null ? new LoopAnimActivity(ctrl, () =>
        {
            bool wallOnRight = Vector3.Dot(_host.transform.right, _host.WallNormal) < 0;
            return wallOnRight ? cfg.WallRunRight : cfg.WallRunLeft;
        }) : null);

        // Slide: blocking exit, added alongside the existing CameraActivity
        var slideAnim = ctrl != null && cfg != null
            ? new SlideAnimationActivity(ctrl, cfg.Slide) : null;
        slide.WithActivity(slideAnim);

        // LedgeGrab: two-phase hang → climb
        var ledgeAbility = _host.MotionOrchestrator.GetAbility<LedgeGrabAbility>();
        var ledgeAnim = ctrl != null && cfg != null
            ? new LedgeGrabAnimationActivity(ctrl, cfg, ledgeAbility) : null;
        ledgeGrab.WithActivity(ledgeAnim);

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
            .WithState(fall);
    }
}
