using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using StateMachine;

/**
 * <summary>
 * Non-blocking animation activity for one-shot clips (<see cref="OneShotAnimDef"/>).
 * Plays the assigned clip on activation and holds the last frame until the state exits.
 * Both <see cref="ActivateAsync"/> and <see cref="DeactivateAsync"/> return immediately —
 * no blocking occurs and no exit clip is played.
 * </summary>
 *
 * <remarks>
 * A <c>Func&lt;OneShotAnimDef&gt;</c> overload is available for states that select their
 * animation at activation time based on runtime context (e.g. landing speed, turn direction).
 * </remarks>
 */
public class OneShotAnimActivity : AnimationActivityBase
{
    private readonly Func<OneShotAnimDef> _getDef;

    /** <summary>Static overload: always uses the same def.</summary> */
    public OneShotAnimActivity(PlayerAnimationController ctrl, OneShotAnimDef def)
        : this(ctrl, () => def) { }

    /** <summary>Dynamic overload: def is evaluated at activation time.</summary> */
    public OneShotAnimActivity(PlayerAnimationController ctrl, Func<OneShotAnimDef> getDef)
        : base(ctrl)
    {
        _getDef = getDef;
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

    /** <inheritdoc /> */
    public override UniTask DeactivateAsync(CancellationToken token)
    {
        if (Mode == ActivityMode.Inactive) return UniTask.CompletedTask;
        Mode = ActivityMode.Deactivating;
        Mode = ActivityMode.Inactive;
        return UniTask.CompletedTask;
    }
}
