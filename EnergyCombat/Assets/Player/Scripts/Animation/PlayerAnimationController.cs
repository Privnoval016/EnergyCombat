using Animancer;
using UnityEngine;

[RequireComponent(typeof(AnimancerComponent))]
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private PlayerAnimationConfig _config;
    [SerializeField] private PlayerController _player;

    private AnimancerComponent _animancer;
    private Vector2MixerState _activeMixer;
    private Vector2 _smoothedDir;

    public PlayerAnimationConfig Config => _config;

    /// <summary>Layer access for activities and AnimancerAnimationDriver.</summary>
    public AnimancerLayer GetLayer(int index) => _animancer.Layers[index];

    public void SetActiveMixer(Vector2MixerState mixer) => _activeMixer = mixer;
    public void ClearActiveMixer() => _activeMixer = null;

    /**
     * <summary>
     * Enables or disables root motion. When active, <c>Animator.applyRootMotion</c> is set
     * to <c>true</c>, which causes Unity to invoke <c>OnAnimatorMove</c> — picked up by
     * the <c>RedirectRootMotionToRigidbody</c> component on this GameObject to drive the
     * Rigidbody instead of the Transform.
     * </summary>
     */
    public void SetRootMotionActive(bool active)
    {
        _animancer.Animator.applyRootMotion = active;
    }

    private void Awake()
    {
        _animancer = GetComponent<AnimancerComponent>();
        // Prevent Animancer from applying root-motion data to the transform — physics owns movement and rotation.
        // Root motion is re-enabled per-ability via SetRootMotionActive and redirected by RedirectRootMotionToRigidbody.
        _animancer.Animator.applyRootMotion = false;
        if (_player == null) _player = GetComponent<PlayerController>();
    }

    private void LateUpdate()
    {
        if (_activeMixer == null) return;

        var vel = _player.MotionOrchestrator.Velocity;
        vel.y = 0f;

        var raw = vel.sqrMagnitude > 0.01f
            ? new Vector2(
                Vector3.Dot(vel.normalized, transform.right),
                Vector3.Dot(vel.normalized, transform.forward))
            : Vector2.zero;

        _smoothedDir = Vector2.Lerp(_smoothedDir, raw, Time.deltaTime * 12f);
        _activeMixer.Parameter = _smoothedDir;
    }
}
