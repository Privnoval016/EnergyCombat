using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using StateMachine;
using UnityEngine;

public class CameraActivity : Activity
{
    private readonly CameraController _cameraController;
    private readonly CamMode _targetCameraMode;
    private readonly float _duration;
    
    public CameraActivity(CameraController cameraController, CamMode targetCameraMode, float duration)
    {
        _cameraController = cameraController;
        _targetCameraMode = targetCameraMode;
        _duration = duration;
    }
    
    public override async UniTask ActivateAsync(CancellationToken cancellationToken)
    {
        if (Mode != ActivityMode.Inactive) return;

        Mode = ActivityMode.Activating;

        _cameraController.SwitchCamera(_targetCameraMode);
        
        await UniTask.WaitForSeconds(_duration, cancellationToken: cancellationToken);
        
        await UniTask.CompletedTask;
        Mode = ActivityMode.Active;
    }
    
    public override async UniTask DeactivateAsync(CancellationToken cancellationToken)
    {
        if (Mode == ActivityMode.Inactive) return;

        Mode = ActivityMode.Deactivating;

        _cameraController.ResumeLastCamera();
        
        await UniTask.WaitForSeconds(_duration, cancellationToken: cancellationToken);
        
        await UniTask.CompletedTask;
        Mode = ActivityMode.Inactive;
    }
}
