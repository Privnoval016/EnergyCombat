namespace DynamicPhysics
{
    [System.Serializable]
    public class JumpSettings
    {
        public float JumpHeight;
        public float CoyoteTime;
        public float JumpBuffer;
        public float JumpCutMultiplier;
        public float ApexThreshold;
        public float ApexGravityMultiplier;

        public static JumpSettings Default => new()
        {
            JumpHeight = 2.5f,
            CoyoteTime = 0.12f,
            JumpBuffer = 0.1f,
            JumpCutMultiplier = 0.5f,
            ApexThreshold = 1.5f,
            ApexGravityMultiplier = 0.4f
            
        };
    }

    [System.Serializable]
    public class DashSettings
    {
        public float DashSpeed;
        public float DashDuration;
        public float DashCooldown;
        public float DashGravityScale;

        public static DashSettings Default => new()
        {
            DashSpeed = 25f,
            DashDuration = 0.15f,
            DashCooldown = 0.8f,
            DashGravityScale = 0.1f
        };
    }

    [System.Serializable]
    public class SlideSettings
    {
        public float SlideBoostMultiplier;
        public float SlideFriction;
        public float SlideMinSpeed;
        public float SlideMaxDuration;
        public float SlideMinEntrySpeed;
        public float SlideHeightMultiplier;

        public static SlideSettings Default => new()
        {
            SlideBoostMultiplier = 1.3f,
            SlideFriction = 12f,
            SlideMinSpeed = 2f,
            SlideMaxDuration = 1.5f,
            SlideMinEntrySpeed = 5f,
            SlideHeightMultiplier = 0.5f
        };
    }
}