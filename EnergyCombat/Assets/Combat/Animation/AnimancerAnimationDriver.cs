using System.Threading;
using Animancer;
using Cysharp.Threading.Tasks;
using DynamicPhysics;
using UnityEngine;

namespace Combat
{
    [AddComponentMenu("Combat/Animation/Animancer Animation Driver")]
    public class AnimancerAnimationDriver : MonoBehaviour, IAnimationDriver
    {
        [SerializeField] private AnimancerComponent _animancer;

        /**
         * <summary>
         * Player animation controller. Assign when root-motion redirect to Rigidbody is needed.
         * Root motion is toggled via <see cref="PlayerAnimationController.SetRootMotionActive"/>.
         * </summary>
         */
        [Tooltip("PlayerAnimationController on the player. Required for root-motion redirect.")]
        [SerializeField] private PlayerAnimationController _animCtrl;

        /**
         * <summary>
         * Motion orchestrator. Required to set <see cref="MotionTag.RootMotionDriven"/>
         * so <see cref="InputSteeringStage"/> yields control to the animation clip.
         * </summary>
         */
        [Tooltip("MotionOrchestrator on the player. Required for root-motion tag management.")]
        [SerializeField] private MotionOrchestrator _motionOrchestrator;

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

            _currentState = _animancer.Layers[request.Layer].Play(request.Clip, request.FadeInDuration);
            _currentState.Speed = request.Speed;

            if (request.EventNames != null)
                foreach (var name in request.EventNames)
                {
                    string n = name;
                    _currentState.OwnedEvents.AddCallback(n, () => handle.TriggerEvent(n));
                }

            _currentState.OwnedEvents.OnEnd = () => handle.NotifyComplete();

            if (request.UseRootMotion)
            {
                _animCtrl?.SetRootMotionActive(true);
                _motionOrchestrator?.Context.SetTag(MotionTag.RootMotionDriven);
            }

            TrackNormalizedTimeAsync(handle, _currentState, token).Forget();
            return handle;
        }

        public void Stop()
        {
            if (_currentState != null)
            {
                _currentState.OwnedEvents.OnEnd = null;
                _currentState = null;
            }

            _animCtrl?.SetRootMotionActive(false);
            _motionOrchestrator?.Context.RemoveTag(MotionTag.RootMotionDriven);
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
