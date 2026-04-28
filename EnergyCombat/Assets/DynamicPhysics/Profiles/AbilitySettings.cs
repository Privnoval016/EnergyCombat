using UnityEngine;

namespace DynamicPhysics
{
    [System.Serializable]
    public class JumpSettings
    {
        [Tooltip("The maximum height the character will reach when performing a jump. This is used to calculate the initial jump velocity.")]
        public float JumpHeight;
        
        [Tooltip("The amount of time after leaving a platform during which a jump can still be initiated. This allows for more forgiving jump timing.")]
        public float CoyoteTime;
        
        [Tooltip("The amount of time before landing during which a jump input will be buffered and automatically performed upon landing. This allows for more forgiving jump timing.")]
        public float JumpBuffer;
        
        [Tooltip("The multiplier applied to vertical velocity when the jump button is released early, allowing for variable jump height based on input duration.")]
        public float JumpCutMultiplier;
        
        [Tooltip("The vertical velocity threshold below which the character is considered to be at the apex of the jump. Below this threshold, gravity will be reduced to create a floaty apex hang effect.")]
        public float ApexThreshold;
        
        [Tooltip("The multiplier applied to gravity when the character's vertical velocity is below the apex threshold, creating a floaty hang at the top of the jump.")]
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
        [Tooltip("The speed at which the character will dash when the dash ability is activated.")]
        public float DashSpeed;
        
        [Tooltip("The duration, in seconds, that the dash will last. During this time, the character's horizontal velocity will be overridden to maintain a consistent dash speed.")]
        public float DashDuration;
        
        [Tooltip("The cooldown time, in seconds, after a dash is performed before the dash ability can be activated again.")]
        public float DashCooldown;
        
        [Tooltip("The multiplier applied to gravity during the dash. Lower values will reduce the effect of gravity, allowing for a more horizontal dash trajectory.")]
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
        [Tooltip("The multiplier applied to the character's horizontal speed when initiating a slide. Higher values will result in a faster initial slide speed.")]
        public float SlideBoostMultiplier;
        
        [Tooltip("The friction applied to the character while sliding. Higher values will cause the character to slow down more quickly.")]
        public float SlideFriction;
        
        [Tooltip("The minimum horizontal speed required to be considered sliding. If the character's horizontal speed is below this threshold, the slide ends.")]
        public float SlideMinSpeed;
        
        [Tooltip("The maximum duration, in seconds, that a slide can last. After this time has elapsed, the slide will end and normal movement will resume.")]
        public float SlideMaxDuration;
        
        [Tooltip("The minimum horizontal speed a slide gives to the player.")]
        public float SlideMinEntrySpeed;

        public static SlideSettings Default => new()
        {
            SlideBoostMultiplier = 1.3f,
            SlideFriction = 12f,
            SlideMinSpeed = 2f,
            SlideMaxDuration = 1.5f,
            SlideMinEntrySpeed = 5f,
        };
    }
}