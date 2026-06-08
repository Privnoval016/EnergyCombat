using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using StateMachine;

/**
 * <summary>
 * Animation activity for the quick-turn state. Plays the chosen turn clip (left or right) and,
 * on deactivation, snaps the rigidbody rotation to the character's current desired facing direction
 * so the physical orientation matches the animation's end pose seamlessly.
 * </summary>
 *
 * <remarks>
 * Rotation is frozen during the clip by <see cref="MotionOrchestrator"/>'s
 * <c>ApplyRotation</c> early-return guard (active while <c>MotionTag.QuickTurning</c> is set).
 * The snap here completes the handoff: frozen → animation plays → snap → normal Lerp resumes.
 * </remarks>
 */
public class QuickTurnAnimationActivity : AnimationActivityBase
{
    private readonly Func<OneShotAnimDef> _getDef;
    private readonly PlayerController _host;

    /** <summary>Dynamic overload: def is evaluated at activation time to choose the L/R clip.</summary> */
    public QuickTurnAnimationActivity(
        PlayerAnimationController ctrl,
        Func<OneShotAnimDef> getDef,
        PlayerController host)
        : base(ctrl)
    {
        _getDef = getDef;
        _host   = host;
    }

    /** <inheritdoc /> */
    public override UniTask ActivateAsync(CancellationToken token)
    {
        if (Mode != ActivityMode.Inactive) return UniTask.CompletedTask;
        Mode = ActivityMode.Activating;

        var def = _getDef();
        if (def?.IsValid == true)
            _ = Ctrl.GetLayer(0).Play(def.Clip, def.FadeDuration);

        Mode = ActivityMode.Active;
        return UniTask.CompletedTask;
    }

    /** <inheritdoc />
     * <remarks>
     * Calls <see cref="MotionOrchestrator.SnapToDesiredFacing"/> before yielding the state so the
     * character is already facing the correct direction when the next state activates.
     * </remarks>
     */
    public override UniTask DeactivateAsync(CancellationToken token)
    {
        if (Mode == ActivityMode.Inactive) return UniTask.CompletedTask;
        Mode = ActivityMode.Deactivating;
        _host.MotionOrchestrator.SnapToDesiredFacing();
        Mode = ActivityMode.Inactive;
        return UniTask.CompletedTask;
    }
}
