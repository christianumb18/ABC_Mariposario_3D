using UnityEngine;

/// <summary>
/// Cámara en tercera persona que sigue a la mariposa suavemente.
/// Adjunta este script a la Main Camera.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;               // Arrastra el GameObject de la mariposa

    [Header("Posición relativa")]
    public Vector3 offset = new Vector3(0f, 2f, -5f);
    [Range(1f, 20f)]
    public float followSpeed = 8f;    // Qué tan rápido sigue al objetivo
    [Range(1f, 20f)]
    public float rotationSpeed = 5f;    // Qué tan rápido gira hacia el objetivo

    [Header("Colisión")]
    public LayerMask collisionMask;        // Capas con las que puede chocar la cámara
    public float collisionRadius = 0.3f;

    // ── Interno ────────────────────────────────────────────────────
    private Vector3 _currentVelocity;

    // Usamos LateUpdate para que se ejecute después de Update de la mariposa
    private void LateUpdate()
    {
        if (target == null) return;

        // Posición deseada en espacio mundo
        Vector3 desiredPos = target.TransformPoint(offset);

        // Evita que la cámara atraviese geometría
        desiredPos = ResolveCollision(target.position, desiredPos);

        // Suaviza la posición con SmoothDamp
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPos,
            ref _currentVelocity,
            1f / followSpeed
        );

        // Rota hacia el objetivo
        Quaternion lookRot = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot,
                                               Time.deltaTime * rotationSpeed);
    }

    // ── Evita que la cámara cruce paredes ──────────────────────────
    private Vector3 ResolveCollision(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float distance = dir.magnitude;

        if (Physics.SphereCast(from, collisionRadius, dir.normalized,
                               out RaycastHit hit, distance, collisionMask))
        {
            return from + dir.normalized * (hit.distance - collisionRadius);
        }
        return to;
    }

    /// <summary>
    /// Llama esto cuando cambies de mariposa para reasignar el target.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}