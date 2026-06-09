using UnityEngine;

/// <summary>
/// Vuelo en linea recta de ida y vuelta a partir de la posicion inicial.
/// El punto A es donde colocas el ave; el punto B se calcula con una
/// distancia en metros y una direccion. El aleteo lo da el Animator;
/// este script solo desplaza el modelo.
///
/// SETUP:
///   1. Coloca el ave donde quieres que arranque el recorrido (punto A).
///   2. Adjunta este script (Add Component).
///   3. Ajusta distance (metros) y direction (hacia donde vuela).
/// </summary>
public class FlyingPath : MonoBehaviour
{
    public enum FlyDirection { Forward, Back, Right, Left }

    [Header("Recorrido")]
    [Tooltip("Largo del recorrido en metros, desde el punto inicial")]
    public float distance = 15f;
    [Tooltip("Direccion del recorrido respecto al mundo")]
    public FlyDirection direction = FlyDirection.Forward;

    [Header("Vuelo")]
    [Tooltip("Velocidad de vuelo (m/s)")]
    public float moveSpeed = 3f;
    [Tooltip("Velocidad de giro al darse la vuelta en los extremos")]
    public float turnSpeed = 3f;
    [Tooltip("Distancia a la que considera que llego al extremo")]
    public float arrivalRadius = 0.5f;

    [Header("Pausa en cada extremo (segundos)")]
    [Tooltip("0 = se devuelve de inmediato sin detenerse")]
    public float pauseAtEnds = 0f;

    [Header("Flotacion (movimiento sutil)")]
    [Tooltip("Oscilacion vertical leve. 0 = vuelo perfectamente recto")]
    public float bobAmplitude = 0.1f;
    public float bobFrequency = 1f;

    // -- Estado interno --------------------------------------------
    private Vector3 _a, _b;
    private Vector3 _flightPos;   // posicion de vuelo sin el bob
    private Vector3 _target;
    private bool _goingToB = true;
    private float _pauseUntil;
    private float _bobPhase;

    private void Start()
    {
        _a = transform.position;
        _b = _a + DirectionVector() * distance;

        _flightPos = _a;
        _target    = _b;   // arranca volando hacia B
        _bobPhase  = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        // Oscilacion vertical suave encima del vuelo
        _bobPhase += Time.deltaTime * bobFrequency * Mathf.PI * 2f;
        float bob = Mathf.Sin(_bobPhase) * bobAmplitude;

        // Pausa en el extremo
        if (Time.time < _pauseUntil)
        {
            transform.position = _flightPos + Vector3.up * bob;
            return;
        }

        // Llego al extremo? Cambia de destino
        if (Vector3.Distance(_flightPos, _target) < arrivalRadius)
        {
            _pauseUntil = Time.time + pauseAtEnds;
            _goingToB   = !_goingToB;
            _target     = _goingToB ? _b : _a;
            transform.position = _flightPos + Vector3.up * bob;
            return;
        }

        // Vuela recto hacia el extremo
        Vector3 dir = (_target - _flightPos).normalized;
        _flightPos += dir * moveSpeed * Time.deltaTime;
        transform.position = _flightPos + Vector3.up * bob;

        // Gira suave hacia donde vuela (curva al dar la vuelta)
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, look, turnSpeed * Time.deltaTime);
        }
    }

    // -- Convierte el enum de direccion en un vector del mundo -----
    private Vector3 DirectionVector()
    {
        switch (direction)
        {
            case FlyDirection.Back:  return Vector3.back;
            case FlyDirection.Right: return Vector3.right;
            case FlyDirection.Left:  return Vector3.left;
            default:                 return Vector3.forward;
        }
    }

    // -- Gizmo: linea del recorrido visible en el editor -----------
    private void OnDrawGizmosSelected()
    {
        Vector3 a = transform.position;
        Vector3 b = a + DirectionVector() * distance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawWireSphere(a, 0.4f);
        Gizmos.DrawWireSphere(b, 0.4f);
    }
}