using UnityEngine;

namespace BladeMode.Input
{
    // Converts right-stick input or fixed cardinal types into a world-space cut plane normal.
    // The normal is perpendicular to the blade direction; it defines WHICH face you'd cut through.
    public sealed class CutDirectionResolver
    {
        readonly Transform _directionReference;
        readonly bool      _invertRotation;

        public CutDirectionResolver(Transform directionReference, bool invertRotation = false)
        {
            _directionReference = directionReference;
            _invertRotation     = invertRotation;
        }

        // Returns the world-space plane normal for the requested cut.
        public Vector3 Resolve(Vector2 stickValue, CutType type)
        {
            switch (type)
            {
                case CutType.Horizontal:
                    return Vector3.up;

                case CutType.Vertical:
                    return Vector3.Cross(_directionReference.forward, Vector3.up).normalized;

                case CutType.Joystick:
                default:
                    return StickToWorldNormal(stickValue);
            }
        }

        Vector3 StickToWorldNormal(Vector2 stick)
        {
            if (stick.sqrMagnitude < 0.001f) return Vector3.up;

            // Flatten camera forward onto the horizontal plane — this is the spin axis.
            var refFwd = _directionReference.forward;
            refFwd.y = 0f;
            if (refFwd.sqrMagnitude < 0.001f) refFwd = Vector3.forward;
            refFwd = refFwd.normalized;

            // Spin world-up around the camera-forward axis by the stick angle.
            // Negated Atan2(y, -x) so CW stick → CW plane rotation from the player's POV.
            //   stick up    →  -90° → normal = camera-left  → vertical plane ✓
            //   stick right →   0°  → normal = up           → horizontal plane ✓
            //   stick down  →  90°  → normal = camera-right → vertical plane ✓
            //   stick left  → 180°  → normal = down         → horizontal plane ✓
            float angle = -Mathf.Atan2(stick.y, -stick.x) * Mathf.Rad2Deg;
            if (_invertRotation) angle = -angle;
            return Quaternion.AngleAxis(angle, refFwd) * Vector3.up;
        }
    }
}
