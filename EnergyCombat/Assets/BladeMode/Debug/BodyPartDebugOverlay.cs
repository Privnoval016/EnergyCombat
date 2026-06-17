using BladeMode.BodyParts;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BladeMode.Debug
{
    // Attach alongside BodyPartRegistry to see brain bone and part markers in the Scene view.
    [RequireComponent(typeof(BodyPartRegistry))]
    [DisallowMultipleComponent]
    public sealed class BodyPartDebugOverlay : MonoBehaviour
    {
        [SerializeField] float _sphereRadius = 0.05f;

        void OnDrawGizmos()
        {
            var registry = GetComponent<BodyPartRegistry>();
            if (registry == null) return;

            // Brain bone: large cyan sphere + upward ray
            if (registry.BrainBone != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(registry.BrainBone.position, _sphereRadius * 2f);
                Gizmos.DrawRay(registry.BrainBone.position, Vector3.up * 0.2f);
            }

            // Part markers: yellow = alive, grey = detached
            foreach (var part in GetComponentsInChildren<SliceableBodyPart>())
            {
                Gizmos.color = part.IsDetached ? Color.gray : Color.yellow;
                Gizmos.DrawWireSphere(part.transform.position, _sphereRadius);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            var labelStyle = new GUIStyle { fontSize = 10 };
            foreach (var part in GetComponentsInChildren<SliceableBodyPart>())
            {
                labelStyle.normal.textColor = part.IsDetached ? Color.gray : Color.yellow;
                Handles.Label(part.transform.position + Vector3.up * 0.1f,
                              part.PartType.ToString(), labelStyle);
            }
        }
#endif
    }
}
