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

        [Header("Wall Run")]
        public WallRunSettings WallRun = WallRunSettings.Default;

        [Header("Ledge Grab")]
        public LedgeGrabSettings LedgeGrab = LedgeGrabSettings.Default;

        [Header("Wall Kick")]
        public WallKickSettings WallKick = WallKickSettings.Default;

        [Header("Post-State Boost")]
        public PostStateBoostSettings PostStateBoost = PostStateBoostSettings.Default;
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

    [System.Serializable]
    public struct PostStateBoostSettings
    {
        [Tooltip("Enable the automatic dash boost on state exit.")]
        public bool Enabled;

        [Tooltip("Maximum angle (degrees) between the player's current forward direction and move input " +
                 "for the boost to trigger. 45° = only roughly forward input qualifies.")]
        [Range(0f, 90f)]
        public float AngleThreshold;

        public static PostStateBoostSettings Default => new()
        {
            Enabled = true,
            AngleThreshold = 45f
        };
    }
}
