using UnityEngine;

namespace Systems.Input
{
    public enum PlayerInputButton
    {
        Jump,
        Dodge,
        Sprint,
        Crouch,
        LightAttack,
        HeavyAttack
    }

    public enum PlayerInputPhase
    {
        Started,
        Performed,
        Canceled
    }

    public readonly struct PlayerInputButtonEvent
    {
        public PlayerInputButtonEvent(PlayerInputButton button, PlayerInputPhase phase)
        {
            Button = button;
            Phase = phase;
        }

        public PlayerInputButton Button { get; }

        public PlayerInputPhase Phase { get; }
    }

    public struct PlayerInputSnapshot
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool JumpHeld;
        public bool SprintToggled;

        public readonly bool HasMoveInput => Move.sqrMagnitude > 0.0001f;
    }
}
