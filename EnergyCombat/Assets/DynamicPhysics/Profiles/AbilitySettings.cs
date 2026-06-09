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

    [System.Serializable]
    public class WallRunSettings
    {
        [Tooltip("Raycast distance from the character centre to detect a runnable wall.")]
        public float WallDetectionDistance = 0.7f;

        [Tooltip("Layers considered valid walls. Default = Everything.")]
        public LayerMask WallLayers = ~0;

        [Tooltip("Maximum seconds the wall run lasts before the character falls.")]
        public float MaxDuration = 1.2f;

        [Tooltip("Horizontal speed maintained along the wall surface.")]
        public float WallRunSpeed = 10f;

        [Tooltip("Gravity multiplier while wall running. Lower = more floaty.")]
        [Range(0f, 1f)]
        public float WallRunGravityScale = 0.15f;

        [Tooltip("Force applied perpendicular to the wall to keep the character stuck to it.")]
        public float WallStickForce = 8f;

        [Tooltip("Minimum horizontal speed required to initiate a wall run.")]
        public float MinEntrySpeed = 2f;

        [Tooltip("Minimum wall surface angle in degrees. Prevents running on shallow slopes.")]
        [Range(0f, 90f)]
        public float MinWallAngle = 45f;

        [Tooltip("If true, the wall run ends when the player stops pushing toward/along the wall.")]
        public bool RequireInputToSustain = true;

        [Tooltip("Max degrees between the player's move direction and wall-forward before the run ends. Only used when RequireInputToSustain is true.")]
        [Range(0f, 180f)]
        public float InputSustainAngleThreshold = 70f;

        [Tooltip("Seconds after the wall run ends before it can re-activate. Prevents oscillation when the player drops off without input.")]
        public float ReactivationCooldown = 0.25f;

        public static WallRunSettings Default => new();
    }

    [System.Serializable]
    public class LedgeGrabSettings
    {
        [Tooltip("Forward raycast distance to detect a climbable wall surface.")]
        public float DetectionDistance = 0.6f;

        [Tooltip("Layers considered valid ledge surfaces. Default = Everything.")]
        public LayerMask LedgeLayers = ~0;

        [Tooltip("Maximum ledge height above the character's current position.")]
        public float MaxLedgeReach = 2.2f;

        [Tooltip("Minimum ledge height above the character's feet. Prevents grabbing floor-level edges.")]
        public float MinLedgeHeight = 0.8f;

        [Tooltip("Seconds the character hangs before the auto-climb begins.")]
        [Range(0f, 1f)]
        public float GrabHoldTime = 0.15f;

        [Tooltip("Duration of the climb-up animation in seconds.")]
        [Range(0.1f, 2f)]
        public float ClimbDuration = 0.4f;

        [Tooltip("Minimum upward speed during climb (safety floor in case geometry is unusual).")]
        public float MinClimbUpSpeed = 2f;

        [Tooltip("Minimum forward speed during climb (safety floor).")]
        public float MinClimbForwardSpeed = 1.5f;

        [Tooltip("Height clearance above the ledge top to aim for during climb.")]
        public float ClimbClearanceAbove = 0.35f;

        [Tooltip("Forward clearance past the ledge edge to ensure the character clears it.")]
        public float ClimbClearanceForward = 0.8f;

        [Tooltip("Maximum upward velocity allowed at grab time. Prevents grabbing while leaping upward fast.")]
        public float MaxEntryUpwardVelocity = 2f;

        [Tooltip("Height above the character's feet for the chest-level forward raycast.")]
        public float DetectionChestHeight = 1.4f;

        [Tooltip("Total arc in degrees swept around the primary approach direction for wall detection. Allows diagonal approach angles.")]
        [Range(0f, 120f)]
        public float WallSearchArcAngle = 60f;

        [Tooltip("Fraction of horizontal entry speed restored after a successful climb. 0 = full stop, 1 = full momentum.")]
        [Range(0f, 1f)]
        public float MomentumPreservationRatio = 0.7f;

        public static LedgeGrabSettings Default => new();
    }

    [System.Serializable]
    public class WallKickSettings
    {
        [Tooltip("Raycast distance to detect a kickable wall surface.")]
        public float WallDetectionDistance = 0.8f;

        [Tooltip("Layers considered valid kick walls. Default = Everything.")]
        public LayerMask WallLayers = ~0;

        [Tooltip("Minimum wall angle in degrees. Prevents kicking shallow slopes.")]
        [Range(0f, 90f)]
        public float MinWallAngle = 30f;

        [Tooltip("Target height (metres) above kick point when kicking an opposite wall.")]
        public float KickHeight = 3f;

        [Tooltip("Horizontal distance (metres) from the wall at the kick apex.")]
        public float KickOutDistance = 2.5f;

        [Tooltip("Height multiplier applied to KickHeight when re-kicking the same wall. 0 = no upward kick; 1 = full height. Clamped to [0, 1] in code — same-wall kicks never send the character downward.")]
        [Range(0f, 1f)]
        public float SameWallHeightScale = 0.3f;

        [Tooltip("Wall normals within this angle of the last kick are considered the same wall.")]
        [Range(0f, 90f)]
        public float SameWallAngleThreshold = 80f;

        [Tooltip("Minimum seconds between wall kicks.")]
        public float KickCooldown = 0.3f;

        [Tooltip("Minimum time (seconds) since last grounded before a wall kick is allowed. " +
                 "Prevents co-activation with a normal grounded jump. Smaller than CoyoteTime is fine " +
                 "because the grounded-tag gap is already < one FixedUpdate tick.")]
        public float MinAirborneTime = 0.05f;

        public static WallKickSettings Default => new();
    }
}