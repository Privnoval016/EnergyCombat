namespace Combat
{
    /**
     * <summary>
     * Exposes locomotion state queries used by combat conditions to gate ability execution.
     * Implement this on <c>PlayerController</c> to allow conditions like
     * <see cref="GroundedCondition"/> to query movement state without a hard dependency
     * on the player controller type.
     * </summary>
     */
    public interface ILocomotionState
    {
        /** <summary>True when the character is standing on a walkable surface.</summary> */
        bool IsGrounded { get; }

        /** <summary>True when the character is not grounded (in the air).</summary> */
        bool IsAirborne { get; }

        /** <summary>True when the character is actively sprinting.</summary> */
        bool IsSprinting { get; }

        /** <summary>True when the character is performing a dodge or dash.</summary> */
        bool IsDodging { get; }
    }
}
