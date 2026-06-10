using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using StateMachine;

/**
 * <summary>
 * Switches the camera to a target mode on activation and restores the previous mode on deactivation.
 * Both phases blend over <c>_duration</c> seconds, blocking Phase 3 / Phase 1 sequencer completion
 * for that duration so downstream states do not start until the camera has settled.
 *
 * <b>Cancellation safety:</b> both methods wrap the timed wait in a try-catch so that a
 * pre-cancelled token (e.g. from the exit-skip policy) or a mid-wait cancellation never leaves
 * Mode stuck at <c>Activating</c> or <c>Deactivating</c>. If cancelled the camera command has
 * already been issued; the wait is simply skipped and Mode is set to its terminal value.
 * </summary>
 */
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

    /** <inheritdoc /> */
    public override async UniTask ActivateAsync(CancellationToken cancellationToken)
    {
        if (Mode != ActivityMode.Inactive) return;
        Mode = ActivityMode.Activating;

        _cameraController.SwitchCamera(_targetCameraMode);

        try { await UniTask.WaitForSeconds(_duration, cancellationToken: cancellationToken); }
        catch (OperationCanceledException) { }

        Mode = ActivityMode.Active;
    }

    /** <inheritdoc /> */
    public override async UniTask DeactivateAsync(CancellationToken cancellationToken)
    {
        if (Mode == ActivityMode.Inactive) return;
        Mode = ActivityMode.Deactivating;

        _cameraController.ResumeLastCamera();

        try { await UniTask.WaitForSeconds(_duration, cancellationToken: cancellationToken); }
        catch (OperationCanceledException) { }

        Mode = ActivityMode.Inactive;
    }
}
