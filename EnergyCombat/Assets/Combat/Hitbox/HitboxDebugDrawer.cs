using UnityEngine;

namespace Combat
{
    /**
     * <summary>
     * Draws Gizmos in the Unity Scene view showing the current hitbox shape and
     * activation state of an attached <see cref="WeaponHitboxController"/>.
     * </summary>
     *
     * <remarks>
     * Attach this to the same <c>GameObject</c> as <see cref="WeaponHitboxController"/>.
     * Green = active (hitbox live), red = inactive (hitbox off).
     * Gizmos are only drawn in the Editor — this component has no runtime cost.
     * </remarks>
     */
    [AddComponentMenu("Combat/Hitbox Debug Drawer")]
    [RequireComponent(typeof(WeaponHitboxController))]
    public class HitboxDebugDrawer : MonoBehaviour
    {
        /** <summary>Colour used when the hitbox is active.</summary> */
        [Header("Debug Colours")]
        public Color ActiveColor = new Color(0f, 1f, 0f, 0.4f);

        /** <summary>Colour used when the hitbox is inactive.</summary> */
        public Color InactiveColor = new Color(1f, 0f, 0f, 0.15f);

        private WeaponHitboxController _controller;

        private void Awake()
        {
            _controller = GetComponent<WeaponHitboxController>();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_controller == null)
                _controller = GetComponent<WeaponHitboxController>();

            if (_controller == null) return;

            Color col = _controller.IsActive ? ActiveColor : InactiveColor;
            Gizmos.color = col;
            Gizmos.DrawSphere(transform.position, 0.15f);

            if (_controller.IsActive)
            {
                Gizmos.color = new Color(col.r, col.g, col.b, 1f);
                Gizmos.DrawWireSphere(transform.position, 0.15f);
            }
        }
#endif
    }
}
