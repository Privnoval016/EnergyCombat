using System.Threading;
using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Stub <see cref="IAnimationDriver"/> implementation for the Animancer animation library.
     * Replace the stub body with real Animancer calls once the package is imported.
     * </summary>
     *
     * <remarks>
     * When Animancer is available:
     * 1. Import Animancer via the Package Manager.
     * 2. Add <c>using Animancer;</c> at the top of this file.
     * 3. Add an <c>AnimancerComponent</c> reference field.
     * 4. Replace the stub <see cref="Play"/> body with <c>AnimancerComponent.Play</c>.
     * 5. Subscribe Animancer events to call <see cref="AnimationHandle.TriggerEvent"/>.
     * 6. Swap <c>NullAnimationDriver</c> for this driver on <c>CombatController</c>.
     * All pipeline and executor code remains unchanged.
     * </remarks>
     */
    [AddComponentMenu("Combat/Animation/Animancer Animation Driver")]
    public class AnimancerAnimationDriver : MonoBehaviour, IAnimationDriver
    {
        /* TODO: Add AnimancerComponent reference once Animancer is imported.
         * [SerializeField] private AnimancerComponent _animancer; */

        /** <inheritdoc /> */
        public AnimationHandle Play(AnimationRequest request, CancellationToken token)
        {
            /* TODO: Implement with Animancer:
             *
             * AnimancerState state = _animancer.Play(request.Clip, request.FadeInDuration);
             * state.Speed = request.Speed;
             * var handle = new AnimationHandle();
             *
             * if (request.EventNames != null)
             * {
             *     foreach (var name in request.EventNames)
             *     {
             *         string captured = name;
             *         state.Events.Add(captured, () => handle.TriggerEvent(captured));
             *     }
             * }
             * state.Events.OnEnd += () => handle.NotifyComplete();
             * return handle;
             */

            Debug.LogWarning("[AnimancerAnimationDriver] Animancer not yet integrated — falling back to NullAnimationDriver.");
            var fallback = new AnimationHandle();
            fallback.NotifyComplete();
            return fallback;
        }

        /** <inheritdoc /> */
        public void Stop()
        {
            /* TODO: _animancer.Stop(); */
        }
    }
}
