using DynamicPhysics;
using UnityEngine;

namespace Player.Config
{
    [CreateAssetMenu(fileName = "Player Locomotion Config", menuName = "Player/Locomotion Config")]
    public class PlayerLocomotionConfig : ScriptableObject
    {
        [Header("Movement Profile")]
        public MovementProfile MovementProfile;

        [Header("Input Thresholds")]
        public InputThresholdSettings InputThresholds = InputThresholdSettings.Default;

        [Header("Jump")]
        public JumpSettings Jump = JumpSettings.Default;

        [Header("Dash")]
        public DashSettings Dash = DashSettings.Default;

        [Header("Slide")]
        public SlideSettings Slide = SlideSettings.Default;
    }

    [System.Serializable]
    public struct InputThresholdSettings
    {
        [Range(0.05f, 1f)] public float MoveInputThreshold;
        [Range(0.1f, 1f)] public float FullThrottleThreshold;

        public static InputThresholdSettings Default => new()
        {
            MoveInputThreshold = 0.15f,
            FullThrottleThreshold = 0.95f
        };
    }
}
