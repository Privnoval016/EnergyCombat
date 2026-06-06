using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Combat
{
    /**
     * <summary>
     * An asynchronous handle returned by <see cref="IAnimationDriver.Play"/> that allows
     * pipeline phases to synchronise with animation state without polling.
     * </summary>
     *
     * <remarks>
     * Pipeline phases use this handle to await key moments in the animation:
     * <list type="bullet">
     * <item><description><see cref="WaitForEventAsync"/> — await a named animation event (e.g. "HitFrame").</description></item>
     * <item><description><see cref="WaitForNormalizedTimeAsync"/> — await a position in the clip (0..1).</description></item>
     * <item><description><see cref="WaitForCompletionAsync"/> — await the end of the clip.</description></item>
     * </list>
     * The driver calls <see cref="TriggerEvent"/> when animation events fire and
     * <see cref="NotifyComplete"/> when the clip finishes.
     * </remarks>
     */
    public class AnimationHandle
    {
        private readonly Dictionary<string, UniTaskCompletionSource<bool>> _eventSources = new();
        private UniTaskCompletionSource<bool> _completionSource = new();
        private bool _isComplete;
        private float _normalizedTime;

        /** <summary><c>true</c> once the animation has played to completion or been stopped.</summary> */
        public bool IsComplete => _isComplete;

        /** <summary>Normalised playback position in [0, 1]. Updated by the driver each frame.</summary> */
        public float NormalizedTime => _normalizedTime;

        /**
         * <summary>Awaits a named animation event. Returns immediately if the event already fired.</summary>
         */
        public async UniTask WaitForEventAsync(string eventName, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (!_eventSources.TryGetValue(eventName, out var tcs))
            {
                tcs = new UniTaskCompletionSource<bool>();
                _eventSources[eventName] = tcs;
            }

            using var reg = token.Register(() => tcs.TrySetCanceled());
            await tcs.Task;
        }

        /** <summary>Awaits the animation reaching a specific normalised time position.</summary> */
        public async UniTask WaitForNormalizedTimeAsync(float normalizedTime, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            while (!token.IsCancellationRequested && _normalizedTime < normalizedTime && !_isComplete)
                await UniTask.NextFrame(token);
            token.ThrowIfCancellationRequested();
        }

        /** <summary>Awaits the animation clip completing fully.</summary> */
        public async UniTask WaitForCompletionAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_isComplete) return;

            using var reg = token.Register(() => _completionSource.TrySetCanceled());
            await _completionSource.Task;
        }

        // ── Driver-facing methods ──────────────────────────────────────────────────────

        /** <summary>Called by the driver when a named animation event fires.</summary> */
        public void TriggerEvent(string name)
        {
            if (_eventSources.TryGetValue(name, out var tcs))
                tcs.TrySetResult(true);
        }

        /** <summary>Called by the driver each frame to update normalised time.</summary> */
        public void UpdateNormalizedTime(float time)
        {
            _normalizedTime = time;
        }

        /** <summary>Called by the driver when the clip has finished playing.</summary> */
        public void NotifyComplete()
        {
            _isComplete = true;
            _completionSource.TrySetResult(true);
        }
    }
}
