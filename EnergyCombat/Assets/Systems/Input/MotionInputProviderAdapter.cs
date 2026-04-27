using DynamicPhysics;
using UnityEngine;

namespace Systems.Input
{
    public sealed class MotionInputProviderAdapter : IMotionInputProvider
    {
        private readonly IPlayerInputSource _source;
        private readonly Transform _orientationTransform;

        public MotionInputProviderAdapter(IPlayerInputSource source, Transform orientationTransform)
        {
            _source = source;
            _orientationTransform = orientationTransform;
        }

        public MotionInputData GetInput()
        {
            var snapshot = _source.Snapshot;

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (_orientationTransform != null)
            {
                forward = Vector3.ProjectOnPlane(_orientationTransform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(_orientationTransform.right, Vector3.up).normalized;
            }

            return new MotionInputData
            {
                MoveInput = snapshot.Move,
                JumpHeld = snapshot.JumpHeld,
                CameraForward = forward,
                CameraRight = right
            };
        }
    }
}
