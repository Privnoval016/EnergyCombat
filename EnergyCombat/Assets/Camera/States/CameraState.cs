using System;
using Extensions.PushdownAutomata;
using Unity.Cinemachine;
using UnityEngine;

[Serializable]
public class CameraState : PushdownState<CameraController>
{
    private const int ActivePriority = 10;
    private const int InactivePriority = 0;
    
    public readonly CinemachineCamera cineCam;
    public readonly CamMode mode;
    
    public CameraState(CinemachineCamera cineCam, CamMode cameraMode, bool baseState)
    {
        this.cineCam = cineCam;
        mode = cameraMode;
        doNotRemove = baseState;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        
        cineCam.Priority = ActivePriority;
    }

    public override void OnExit()
    {
        base.OnExit();
        cineCam.Priority = InactivePriority;
    }
}