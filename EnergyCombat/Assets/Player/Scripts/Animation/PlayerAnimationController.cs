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

    private void Awake()
    {
        _animancer = GetComponent<AnimancerComponent>();
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
