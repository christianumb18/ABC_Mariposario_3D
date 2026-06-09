using UnityEngine;

/// <summary>
/// Patrullaje terrestre simple para depredadores.
/// Deambula a puntos aleatorios dentro de un radio alrededor de su posicion
/// inicial, siempre pegado al terreno mediante un raycast hacia abajo.
///
/// SETUP:
///   1. Adjunta este script a la instancia del depredador terrestre en escena.
///   2. Ajusta patrolRadius, moveSpeed y turnSpeed en el Inspector.
///   3. groundMask debe incluir la layer del Terrain (normalmente "Default").
///   4. El modelo NO debe tener un Collider en su raiz, o el raycast se
///      detectaria a si mismo. Si lo tiene, ponlo en una layer aparte y
///      excluyela de groundMask.
/// </summary>
public class PredatorPatrol : MonoBehaviour
{
    [Header("Patrullaje")]
    [Tooltip("Radio alrededor del punto inicial donde deambula")]
    public float patrolRadius = 3f;
    [Tooltip("Velocidad de desplazamiento (m/s)")]
    public float moveSpeed = 1f;
    [Tooltip("Velocidad de giro hacia la direccion de avance")]
    public float turnSpeed = 4f;
    [Tooltip("Distancia a la que considera que llego al punto")]
    public float arrivalRadius = 0.4f;

    [Header("Pausa al llegar (segundos)")]
    public float minPause = 0.5f;
    public float maxPause = 2.5f;

    [Header("Anclaje al terreno")]
    [Tooltip("Layer del suelo/terreno para el raycast")]
    public LayerMask groundMask = 1;   // 1 = "Default"
    [Tooltip("Altura extra sobre el terreno (ajusta si flota o se hunde)")]
    public float groundOffset = 0f;

    // ── Estado interno ─────────────────────────────────────────────
    private Vector3 _home;
    private Vector3 _target;
    private float _pauseUntil;

    private void Start()
    {
        _home = transform.position;
        PickNewTarget();
    }

    private void Update()
    {
        // Pausa entre tramos: se queda quieto pero sigue pegado al suelo
        if (Time.time < _pauseUntil)
        {
            StickToGround();
            return;
        }

        // Trabaja solo en horizontal; la altura la resuelve StickToGround
        Vector3 flatPos    = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatTarget = new Vector3(_target.x, 0f, _target.z);

        // ¿Llego al punto?
        if (Vector3.Distance(flatPos, flatTarget) < arrivalRadius)
        {
            _pauseUntil = Time.time + Random.Range(minPause, maxPause);
            PickNewTarget();
            return;
        }

        // Avanza en el plano
        Vector3 dir = (flatTarget - flatPos).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;

        // Gira suave hacia donde camina (solo yaw)
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        StickToGround();
    }

    // ── Nuevo punto aleatorio dentro del radio ────────────────────
    private void PickNewTarget()
    {
        Vector2 r = Random.insideUnitCircle * patrolRadius;
        _target = _home + new Vector3(r.x, 0f, r.y);
    }

    // ── Pega el depredador a la superficie del terreno ────────────
    private void StickToGround()
    {
        Vector3 origin = transform.position + Vector3.up * 5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20f, groundMask))
        {
            Vector3 p = transform.position;
            p.y = hit.point.y + groundOffset;
            transform.position = p;
        }
    }

    // ── Gizmo: radio de patrullaje y punto objetivo ───────────────
    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? _home : transform.position;
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.3f);
        Gizmos.DrawWireSphere(center, patrolRadius);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_target, 0.3f);
            Gizmos.DrawLine(transform.position, _target);
        }
    }
}