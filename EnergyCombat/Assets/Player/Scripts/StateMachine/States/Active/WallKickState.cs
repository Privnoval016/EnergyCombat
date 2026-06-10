using System.Threading;
using DynamicPhysics;
using StateMachine;

/**
 * <summary>
 * Active for the entire upward arc of a wall kick (while <c>IsWallKicking</c> is true).
 * Sibling of <see cref="WallRunState"/> and <see cref="LedgeGrabState"/> under <c>ActiveState</c>.
 *
 * Entry: any airborne state (FallState, JumpState, WallRunState) routes here when
 * <c>IsWallKicking</c> becomes true.
 *
 * The <see cref="MotionTag.WallKicking"/> tag lives until vertical velocity reaches 0 or the
 * player lands — not just one FixedUpdate tick. This makes the state robust to multiple
 * FixedUpdates firing between Update frames (low framerate).
 *
 * <b>Rapid consecutive kicks:</b> at certain frame rates two FixedUpdates can fire between two
 * Update frames. If a new kick fires before the state machine observes the arc ending,
 * <see cref="WallKickAbility.KickId"/> changes while the state stays resident. The
 * <see cref="OnUpdate"/> hook detects this and directly cycles the
 * <see cref="OneShotAnimActivity"/> (Deactivate → Activate) so the new kick's animation
 * selection fires with the updated <see cref="WallKickAbility.IsWallRunExit"/> and
 * <see cref="WallKickAbility.KickSign"/> values. Self-transitions are blocked by the sequencer
 * so routing through another state is not used — direct activity cycling is safe here because
 * <see cref="OneShotAnimActivity"/> methods are fully synchronous.
 *
 * On exit: sets the landing boost pending flag so a sprint burst fires on the next grounded landing.
 * </summary>
 */
public class WallKickState : State<PlayerController>
{
    private WallKickAbility _wkAbility;
    private int _lastKickId;

    public WallKickState()
    {
        WithTransition(new FuncTransition<PlayerController>((host, _) =>
        {
            if (host.IsAttacking)     return host.GetState<AttackingState>();
            if (host.IsLedgeGrabbing) return host.GetState<LedgeGrabState>();
            if (host.IsWallKicking)   return null; // arc in progress — stay
            if (host.IsGrounded)      return host.GetState<GroundedState>();
            if (host.IsWallRunning)   return host.GetState<WallRunState>(); // wall-to-wall: run activated mid-kick
            return host.GetState<FallState>(); // arc ended, descending
        }));
    }

    protected override void OnEnter()
    {
        // Lazy-resolve ability (registered after state machine construction) and snapshot KickId.
        _wkAbility ??= Host.MotionOrchestrator.GetAbility<WallKickAbility>();
        _lastKickId = _wkAbility?.KickId ?? 0;
    }

    protected override void OnUpdate(float dt)
    {
        _wkAbility ??= Host.MotionOrchestrator.GetAbility<WallKickAbility>();
        if (_wkAbility == null || _wkAbility.KickId == _lastKickId) return;

        // A new kick fired while the state is still resident. Update the snapshot first so the
        // activity lambda reads the fresh KickSign / IsWallRunExit from the new kick, then cycle
        // Deactivate → Activate to replay the animation selection with the new context.
        _lastKickId = _wkAbility.KickId;
        var none = CancellationToken.None;
        foreach (var activity in Activities)
        {
            _ = activity.DeactivateAsync(none);
            _ = activity.ActivateAsync(none);
        }
    }

    protected override void OnExit()
    {
        Host.SetLandingBoostPending();
    }
}
