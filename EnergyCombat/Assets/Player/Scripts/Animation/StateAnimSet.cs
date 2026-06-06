using System;
using Animancer;
using UnityEngine;

/// <summary>
/// All animation data for one locomotion state. Every field is optional — absent = silent no-op.
/// Enter and Exit are single clips; Loop is any ILoopDefinition (clip or blend tree).
/// </summary>
[Serializable]
public class StateAnimSet
{
    public ClipTransition Enter;
    [SerializeReference] public ILoopDefinition Loop;
    public ClipTransition Exit;
    public float FadeDuration = 0.15f;

    public bool HasEnter => Enter?.IsValid == true;
    public bool HasLoop  => Loop?.IsValid  == true;
    public bool HasExit  => Exit?.IsValid  == true;
}
