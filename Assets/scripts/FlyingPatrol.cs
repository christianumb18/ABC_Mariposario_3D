using UnityEngine;

/// <summary>
/// Patrullaje aereo simple para depredadores voladores (avispa, pajaro, etc).
/// Deambula en 3D a puntos aleatorios dentro de una zona alrededor de su
/// posicion inicial. NO toca el suelo: se mantiene en el aire a la altura
/// donde lo coloques, con una flotacion suave para que el vuelo se vea natural.
///
/// SETUP:
///   1. Coloca el modelo en el aire, a la altura de vuelo que quieras.
///   2. Adjunta este script a la instancia (Add Component).
///   3. Ajusta patrolRadius (que tan lejos vuela), verticalRange (cuanto sube
///      y baja) y moveSpeed en el Inspector.
/// </summary>
public class FlyingPatrol : MonoBehaviour
{
    [Header("Patrullaje aereo")]
    [Tooltip("Radio horizontal alrededor del punto inicial donde vuela")]
    public float patrolRadius = 5f;
    [Tooltip("Cuanto sube y baja respecto a su altura inicial")]
    public float verticalRange = 2f;
    [Tooltip("Velocidad de vuelo (m/s)")]
    public float moveSpeed = 2f;
    [Tooltip("Velocidad de giro hacia la direccion de vuelo")]
    public float turnSpeed = 3f;
    [Tooltip("Distancia a la que considera que llego al punto")]
    public float arrivalRadius = 0.5f;

    [Header("Pausa al llegar (segundos)")]
    [Tooltip("Pequena pausa flotando al alcanzar un punto. 0 = nunca se detiene")]
    public float minPause = 0f;
    public float maxPause = 1f;

    [Header("Flotacion (movimiento disimulado)")]
    [Tooltip("Altura de la oscilacion suave. Mantener bajo para que sea sutil")]
    public float bobAmplitude = 0.15f;
    [Tooltip("Frecuencia de la oscilacion")]
    public float bobFrequency = 1f;

    // ── Estado interno ─────────────────────────────────────────────
    private Vector3 _home;       // posicion inicial = centro de la zona
    private Vector3 _flightPos;  // posicion real de vuelo (sin el bob)
    private Vector3 _target;
    private float _pauseUntil;
    private float _bobPhase;

    private void Start()
    {
        _home      = transform.position;
        _flightPos = _home;
        _bobPhase  = Random.Range(0f, Mathf.PI * 2f);
        PickNewTarget();
    }

    private void Update()
    {
        // Oscilacion vertical suave, se suma encima de la posicion de vuelo
        _bobPhase += Time.deltaTime * bobFrequency * Mathf.PI * 2f;
        float bob = Mathf.Sin(_bobPhase) * bobAmplitude;

        // Pausa flotando entre tramos
        if (Time.time < _pauseUntil)
        {
            transform.position = _flightPos + Vector3.up * bob;
            return;
        }

        // ¿Llego al punto?
        if (Vector3.Distance(_flightPos, _target) < arrivalRadius)
        {
            _pauseUntil = Time.time + Random.Range(minPause, maxPause);
            PickNewTarget();
            transform.position = _flightPos + Vector3.up * bob;
            return;
        }

        // Vuela hacia el punto en 3D
        Vector3 dir = (_target - _flightPos).normalized;
        _flightPos += dir * moveSpeed * Time.deltaTime;
        transform.position = _flightPos + Vector3.up * bob;

        // Gira suave hacia donde vuela
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, look, turnSpeed * Time.deltaTime);
        }
    }

    // ── Nuevo punto aleatorio dentro de la zona de vuelo ──────────
    private void PickNewTarget()
    {
        Vector2 r = Random.insideUnitCircle * patrolRadius;
        float y = _home.y + Random.Range(-verticalRange, verticalRange);
        _target = new Vector3(_home.x + r.x, y, _home.z + r.y);
    }

    // ── Gizmo: zona de vuelo y punto objetivo ─────────────────────
    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? _home : transform.position;
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
        Gizmos.DrawWireSphere(center, patrolRadius);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_target, 0.3f);
            Gizmos.DrawLine(transform.position, _target);
        }
    }
}