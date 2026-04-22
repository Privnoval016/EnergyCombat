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
        var active = new ActiveState().WithParent(root);

        var idle = new IdleState().AsInitialState(active);
        
        builder.WithState(active).WithState(idle);
    }
}