using System;
using UnityEngine;

namespace Systems.Input
{
    public interface IPlayerInputSource
    {
        event Action<PlayerInputButtonEvent> ButtonEvent;

        PlayerInputSnapshot Snapshot { get; }

        void Enable();

        void Disable();
    }
}
