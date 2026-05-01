using StateMachine;

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
        var dash = new DashState().WithParent(grounded);

        var idle = new IdleState().AsInitialState(grounded);
        var move = new MoveState().WithParent(grounded);
        var sprint = new SprintState().WithParent(grounded);
        var slide = new SlideState().WithParent(grounded)
            .WithActivity(new CameraActivity(_host.CameraController, CamMode.OverRightShoulder, 0.2f));
        
        var jump = new JumpState().AsInitialState(airborne);
        var fall = new FallState().WithParent(airborne);

        _builder
            .WithState(active)
            .WithState(grounded)
            .WithState(airborne)
            .WithState(dash)
            .WithState(idle)
            .WithState(move)
            .WithState(sprint)
            .WithState(slide)
            .WithState(jump)
            .WithState(fall);
    }
}