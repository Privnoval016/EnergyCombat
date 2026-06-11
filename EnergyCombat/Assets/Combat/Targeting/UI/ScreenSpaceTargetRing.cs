using UnityEngine;

namespace Combat.Targeting.UI
{
    /**
     * <summary>
     * Screen-space canvas ring that projects over the current target.
     * Implements <see cref="ITargetIndicator"/> using a <see cref="RectTransform"/>
     * child of a Canvas in Screen Space Overlay or Camera mode.
     * </summary>
     *
     * <remarks>
     * Assign the ring's own <see cref="RectTransform"/> (or a child) to <see cref="_ring"/>,
     * the scene camera to <see cref="_camera"/>, and the parent Canvas to <see cref="_canvas"/>.
     * The GameObject is deactivated when hidden to avoid draw calls.
     * </remarks>
     */
    [AddComponentMenu("Combat/Targeting/UI/Screen Space Target Ring")]
    [RequireComponent(typeof(RectTransform))]
    public class ScreenSpaceTargetRing : MonoBehaviour, ITargetIndicator
    {
        /** <summary>The RectTransform to reposition. Defaults to this component's own RectTransform.</summary> */
        [Tooltip("RectTransform to move. Leave empty to use this object's own RectTransform.")]
        [SerializeField] private RectTransform _ring;

        /** <summary>Camera used for world-to-screen projection. Defaults to Camera.main.</summary> */
        [Tooltip("Camera used for world-to-screen projection. Leave empty to use Camera.main.")]
        [SerializeField] private Camera _camera;

        /** <summary>The parent canvas. Required for RectTransformUtility projection math.</summary> */
        [Tooltip("Parent canvas. Required for correct screen-space projection.")]
        [SerializeField] private Canvas _canvas;

        private void Awake()
        {
            if (_ring == null) _ring = GetComponent<RectTransform>();
            if (_camera == null) _camera = Camera.main;
            gameObject.SetActive(false);
        }

        /** <inheritdoc /> */
        public void ShowAt(ITargetable target)
        {
            if (target == null || _camera == null || _canvas == null || _ring == null) return;

            Vector3 screenPoint = _camera.WorldToScreenPoint(target.TargetPosition);

            // Target is behind the camera — hide the ring
            if (screenPoint.z < 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            Camera projectionCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _camera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvas.GetComponent<RectTransform>(),
                    screenPoint,
                    projectionCamera,
                    out Vector2 localPos))
            {
                _ring.anchoredPosition = localPos;
            }
        }

        /** <inheritdoc /> */
        public void Hide()
        {
            gameObject?.SetActive(false);
        }
    }
}
