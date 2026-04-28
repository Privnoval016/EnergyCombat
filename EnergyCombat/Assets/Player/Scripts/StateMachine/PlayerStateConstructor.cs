using StateMachine;

public static class PlayerStateConstructor
{
    public static StateMachine<PlayerController> Build(PlayerController host)
    {
        var root = new RootState();
        var builder = new StateMachineBuilder<PlayerController>(root);
        
        BuildActiveStates(builder, root);
        
        
        var machine = builder.Build(host);
        return machine;
    }

    private static void BuildActiveStates(StateMachineBuilder<PlayerController> builder, State<PlayerController> root)
    {
        var active = new ActiveState().WithParent(root).AsInitialState(root);
        
        var grounded = new GroundedState().AsInitialState(active);
        var airborne = new AirborneState().WithParent(active);
        var dash = new DashState().WithParent(grounded);

        var idle = new IdleState().AsInitialState(grounded);
        var move = new MoveState().WithParent(grounded);
        var sprint = new SprintState().WithParent(grounded);
        var slide = new SlideState().WithParent(grounded);
        
        var jump = new JumpState().AsInitialState(airborne);
        var fall = new FallState().WithParent(airborne);

        builder
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