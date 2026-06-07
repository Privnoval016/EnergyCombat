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
    public OneShotAnimDef WallKick;

    [Header("Ledge — One Shot")]
    public OneShotAnimDef LedgeGrab;
    public OneShotAnimDef LedgeClimb;

    [Header("Quick Turn — One Shot")]
    public OneShotAnimDef QuickTurnLeft;
    public OneShotAnimDef QuickTurnRight;
}
