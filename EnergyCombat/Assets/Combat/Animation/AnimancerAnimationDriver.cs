using System.Threading;
using Animancer;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Combat
{
    [AddComponentMenu("Combat/Animation/Animancer Animation Driver")]
    public class AnimancerAnimationDriver : MonoBehaviour, IAnimationDriver
    {
        [SerializeField] private AnimancerComponent _animancer;

        private AnimancerState _currentState;

        private void Awake()
        {
            if (_animancer == null) _animancer = GetComponent<AnimancerComponent>();
        }

        public AnimationHandle Play(AnimationRequest request, CancellationToken token)
        {
            var handle = new AnimationHandle();

            if (_animancer == null || request?.Clip == null)
            {
                handle.NotifyComplete();
                return handle;
            }

            _currentState = _animancer.Layers[0].Play(request.Clip, request.FadeInDuration);
            _currentState.Speed = request.Speed;

            if (request.EventNames != null)
                foreach (var name in request.EventNames)
                {
                    string n = name;
                    _currentState.OwnedEvents.AddCallback(n, () => handle.TriggerEvent(n));
                }

            _currentState.OwnedEvents.OnEnd = () => handle.NotifyComplete();

            TrackNormalizedTimeAsync(handle, _currentState, token).Forget();
            return handle;
        }

        public void Stop()
        {
            if (_currentState == null) return;
            _currentState.OwnedEvents.OnEnd = null;
            _currentState = null;
        }

        private static async UniTaskVoid TrackNormalizedTimeAsync(
            AnimationHandle handle, AnimancerState state, CancellationToken token)
        {
            while (state != null && !handle.IsComplete && !token.IsCancellationRequested)
            {
                handle.UpdateNormalizedTime(state.NormalizedTime);
                await UniTask.NextFrame(token);
            }
        }
    }
}
