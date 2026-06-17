using UnityEngine;

/// <summary>
/// Compensates for PhysicsMotor overriding rb.linearVelocity every FixedUpdate,
/// which bypasses contact forces and prevents the player from pushing objects.
/// Attach this alongside PhysicsMotor on the player root.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class RigidbodyPusher : MonoBehaviour
{
    [SerializeField] float _pushForce = 8f;

    Rigidbody _rb;

    void Awake() => _rb = GetComponent<Rigidbody>();

    void OnCollisionStay(Collision collision)
    {
        var other = collision.rigidbody;
        if (other == null || other.isKinematic) return;

        Vector3 vel = _rb.linearVelocity;
        Vector3 pushDir = new Vector3(vel.x, 0f, vel.z);
        if (pushDir.sqrMagnitude < 0.01f) return;

        other.AddForce(pushDir.normalized * _pushForce, ForceMode.Force);
    }
}
