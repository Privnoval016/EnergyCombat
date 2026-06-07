using System;
using Animancer;
using UnityEngine;

/**
 * <summary>
 * Animation data for a one-shot clip: plays once and holds the last frame until the state exits.
 * </summary>
 *
 * <remarks>
 * Use this for states with a single non-looping animation (Jump, Fall, Land, QuickTurn, WallKick,
 * LedgeGrab, LedgeClimb). The clip must be set to <b>non-looping</b> in Unity's animation import
 * settings — a looping clip will cycle indefinitely instead of holding the last frame.
 *
 * For states with a start ramp, main loop, and optional stop, use <see cref="LoopAnimDef"/> instead.
 * </remarks>
 */
[Serializable]
public class OneShotAnimDef
{
    public ClipTransition Clip;
    public float FadeDuration = 0.15f;

    /** <summary>True if a Clip is assigned and valid.</summary> */
    public bool IsValid => Clip?.IsValid == true;
}
