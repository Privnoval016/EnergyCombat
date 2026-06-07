using StateMachine;

/**
 * <summary>
 * Parent state for all airborne sub-states (<see cref="JumpState"/>, <see cref="FallState"/>).
 * Does not define its own transitions; grounded-exit transitions are owned by the leaf states
 * so that landing always routes through <see cref="LandState"/>.
 * </summary>
 */
public class AirborneState : State<PlayerController> { }
