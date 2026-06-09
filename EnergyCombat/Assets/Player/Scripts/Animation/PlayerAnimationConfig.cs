using UnityEngine;

/**
 * <summary>
 * ScriptableObject that holds all player animation data, grouped by state type.
 * Looping locomotion states use <see cref="LoopAnimDef"/> (Enter ramp-up, Loop cycle, Exit stop).
 * One-shot states use <see cref="OneShotAnimDef"/> (single Clip that plays once and holds the last frame).
 * </summary>
 */
[CreateAssetMenu(menuName = "Player/Animation Config")]
public class PlayerAnimationConfig : ScriptableObject
{
    [Header("Ground — Loop (Enter = start ramp, Loop = cycle, Exit = stop)")]
    public LoopAnimDef Idle;
    public LoopAnimDef Walk;
    public LoopAnimDef Sprint;
    public LoopAnimDef Dash;
    public LoopAnimDef Slide;

    [Header("Walk Transition Timing")]
    [Tooltip("Seconds of no directional input required before the walk exit animation triggers. " +
             "Prevents the exit clip from firing during rapid direction changes.")]
    [Range(0f, 0.5f)]
    public float WalkIdleGracePeriod = 0.15f;

    [Tooltip("If the player re-enters walk within this many seconds of last leaving it, " +
             "the walk-enter animation is skipped and the loop resumes immediately.")]
    [Range(0f, 1f)]
    public float WalkEnterSkipWindow = 0.35f;

    [Header("Sprint Transition Timing")]
    [Tooltip("Seconds of sustained !ShouldSprint before transitioning out of sprint. " +
             "Prevents the exit clip from firing during rapid direction changes while sprinting.")]
    [Range(0f, 0.5f)]
    public float SprintIdleGracePeriod = 0.1f;

    [Tooltip("If MoveState exited within this many seconds, the sprint-enter animation is skipped. " +
             "Prevents a deceleration-style enter clip from playing when transitioning directly from walk.")]
    [Range(0f, 1f)]
    public float SprintEnterSkipWindow = 0.5f;

    [Header("Airborne — One Shot")]
    public OneShotAnimDef Jump;
    public OneShotAnimDef Fall;

    [Header("Landing — One Shot")]
    public OneShotAnimDef LandIdle;
    public OneShotAnimDef LandMotion;

    [Tooltip("Minimum horizontal speed to play LandMotion instead of LandIdle.")]
    public float LandMotionSpeedThreshold = 3f;

    [Tooltip("Seconds to hold the landing pose before transitioning. 0 = immediate.")]
    public float LandHoldDuration = 0f;

    [Header("Wall — Mixed")]
    public LoopAnimDef WallRunLeft;
    public LoopAnimDef WallRunRight;
    [Tooltip("Standalone wall kick (airborne, not from a wall run). Falls back to Jump if unassigned.")]
    public OneShotAnimDef WallKick;
    [Tooltip("Jump off the left side of a wall run. Falls back to WallKick, then Jump if unassigned.")]
    public OneShotAnimDef WallKickLeft;
    [Tooltip("Jump off the right side of a wall run. Falls back to WallKick, then Jump if unassigned.")]
    public OneShotAnimDef WallKickRight;

    [Header("Ledge — One Shot")]
    public OneShotAnimDef LedgeGrab;
    public OneShotAnimDef LedgeClimb;

    [Header("Quick Turn — One Shot")]
    public OneShotAnimDef QuickTurnLeft;
    public OneShotAnimDef QuickTurnRight;
}
