using System;
using Animancer;
using UnityEngine;

/**
 * <summary>
 * Animation data for a looping locomotion state.
 * All three clips are optional — absent fields are silent no-ops.
 * </summary>
 *
 * <remarks>
 * Typical usage:
 * <list type="bullet">
 *   <item><b>Enter</b> — ramp-up or start clip played once before the loop begins (e.g. run-start).</item>
 *   <item><b>Loop</b> — main cycle. Supports a single clip (<see cref="ClipLoopDef"/>) or a 2D
 *     blend tree (<see cref="MixerLoopDef"/>). Assign via right-click → Managed Reference.</item>
 *   <item><b>Exit</b> — stop or deceleration clip played once when the state exits (e.g. run-stop).</item>
 * </list>
 * Use <see cref="OneShotAnimDef"/> for states that play a single clip with no loop (Jump, Fall, etc.).
 * </remarks>
 */
[Serializable]
public class LoopAnimDef
{
    public ClipTransition Enter;
    [SerializeReference] public ILoopDefinition Loop;
    public ClipTransition Exit;
    public float FadeDuration = 0.15f;

    /** <summary>True if an Enter clip is assigned and valid.</summary> */
    public bool HasEnter => Enter?.IsValid == true;
    /** <summary>True if a Loop definition is assigned and valid.</summary> */
    public bool HasLoop  => Loop?.IsValid  == true;
    /** <summary>True if an Exit clip is assigned and valid.</summary> */
    public bool HasExit  => Exit?.IsValid  == true;
}
