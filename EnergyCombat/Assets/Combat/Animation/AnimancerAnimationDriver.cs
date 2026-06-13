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

        /**
         * <summary>
         * Input settings asset. Used to read <see cref="CombatInputSettings.CombatLayerFadeOutDuration"/>
         * when fading the combat layer back to zero at ability end.
         * </summary>
         */
        [Tooltip("Combat input settings asset. Drives animation blending durations.")]
        [SerializeField] private CombatInputSettings _inputSettings;

        private AnimancerState _currentState;
        private int _currentLayer;

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

            _currentLayer = request.Layer;
            _currentState = _animancer.Layers[_currentLayer].Play(request.Clip, request.FadeInDuration);
            _currentState.Speed = request.Speed;

            if (request.EventNames != null)
                foreach (var name in request.EventNames)
                {
                    string n = name;
                    _currentState.OwnedEvents.AddCallback(n, () => handle.TriggerEvent(n));
                }

            // Capture a local reference so the closure does not hold onto the mutable field.
            // Nulling OnEnd inside the callback prevents Animancer from re-firing it every
            // frame after the clip reaches its end time (OptionalWarning.EndEventInterrupt).
            var capturedState = _currentState;
            _currentState.OwnedEvents.OnEnd = () =>
            {
                capturedState.OwnedEvents.OnEnd = null;
                handle.NotifyComplete();
            };

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
            // Disable root motion first so no delta is applied during the fade-out frame.
            _animCtrl?.SetRootMotionActive(false);
            _motionOrchestrator?.Context.RemoveTag(MotionTag.RootMotionDriven);

            if (_currentState != null)
            {
                _currentState.OwnedEvents.OnEnd = null;
                _currentState = null;

                // Fade the layer weight smoothly to zero so locomotion blends back in
                // rather than snapping. When a new ability Play() call immediately follows
                // (combo chain), the new clip's fade-in overrides this automatically.
                float fadeOut = _inputSettings != null ? _inputSettings.CombatLayerFadeOutDuration : 0.15f;
                _animancer?.Layers[_currentLayer].StartFade(0f, fadeOut);
            }
        }

        private static async UniTaskVoid TrackNormalizedTimeAsync(
            AnimationHandle handle, AnimancerState state, CancellationToken token)
        {
            while (state != null && !handle.IsComplete && !token.IsCancellationRequested)
            {
                // NormalizedTime triggers Animancer's internal AssertPlayable validation,
                // which can throw ArgumentException when a new animation preempts this one
                // and Animancer destroys the underlying Playable before this loop exits.
                // InvalidOperationException is thrown when the PlayableGraph itself is
                // destroyed (character death, scene unload, or immediate combo replacement).
                float normalizedTime;
                try
                {
                    normalizedTime = state.NormalizedTime;
                }
                catch (System.ArgumentException)
                {
                    break;
                }
                catch (System.InvalidOperationException)
                {
                    break;
                }

                handle.UpdateNormalizedTime(normalizedTime);
                await UniTask.NextFrame(token);
            }
        }
    }
}
