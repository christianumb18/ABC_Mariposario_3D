using UnityEngine;

/// <summary>
/// Vuelo autónomo de una mariposa NPC: wander + boundary avoidance + bob vertical.
/// Usa ButterflyData (assets existentes en Characters/) — sin Rigidbody physics.
///
/// SETUP: No adjuntar manualmente. ButterflyNPCManager lo agrega vía AddComponent
///        y llama a Initialize() después de instanciar el prefab.
/// </summary>
[DisallowMultipleComponent]
public class ButterflyNPCBehavior : MonoBehaviour
{
    // ── Datos de especie (los assets existentes ButterflyData) ─────
    public ButterflyData Data { get; private set; }

    // ── Límites del mariposario ────────────────────────────────────
    private Bounds _bounds;

    // ── Wander ────────────────────────────────────────────────────
    private Vector3 _targetWaypoint;
    private Vector3 _velocity;
    private float   _currentSpeed;
    private float   _nextSpeedChangeTime;
    private const float SPEED_VARIANCE  = 1.0f;

    private const float ARRIVAL_RADIUS  = 2f;
    private const float BOUNDARY_MARGIN = 2.5f;

    // ── Bob vertical ──────────────────────────────────────────────
    private float _bobPhase;
    private float _prevBobY;

    // ── Animación ─────────────────────────────────────────────────
    private ButterflyAnimator _animator;

    // ═══════════════════════════════════════════════════════════════

    /// <summary>Llamado por ButterflyNPCManager justo después de instanciar el prefab.</summary>
    public void Initialize(ButterflyData data, Bounds bounds)
    {
        Data   = data;
        _bounds = bounds;

        transform.localScale = Vector3.one * data.scale;

        _currentSpeed        = data.flightSpeed;
        _nextSpeedChangeTime  = Time.time + Random.Range(2f, 6f);
        _bobPhase            = Random.Range(0f, Mathf.PI * 2f);

        DisablePlayerComponents();

        _animator = GetComponentInChildren<ButterflyAnimator>();
        _animator?.PlayAnimation(ButterflyAnimator.ButterflyAnimation.Flying);

        _targetWaypoint = PickWaypoint();
        _velocity       = (_targetWaypoint - transform.position).normalized * _currentSpeed;
    }

    // ═══════════════════════════════════════════════════════════════

    private void Update()
    {
        if (Data == null) return;

        UpdateSpeed();
        Steer();
        ApplyBob();
    }

    // ── Varía la velocidad suavemente ─────────────────────────────
    private void UpdateSpeed()
    {
        if (Time.time < _nextSpeedChangeTime) return;

        _currentSpeed = Mathf.Max(0.5f,
            Data.flightSpeed + Random.Range(-SPEED_VARIANCE * 0.5f, SPEED_VARIANCE * 0.5f));
        _nextSpeedChangeTime = Time.time + Random.Range(2f, 6f);
    }

    // ── Steering hacia el waypoint con avoidance de bordes ────────
    private void Steer()
    {
        float dt = Time.deltaTime;

        if (Vector3.Distance(transform.position, _targetWaypoint) < ARRIVAL_RADIUS)
            _targetWaypoint = PickWaypoint();

        Vector3 desired = (_targetWaypoint - transform.position).normalized * _currentSpeed;

        Vector3 pos    = transform.position;
        Vector3 center = _bounds.center;

        if (pos.x < _bounds.min.x + BOUNDARY_MARGIN) desired.x += center.x - pos.x;
        if (pos.x > _bounds.max.x - BOUNDARY_MARGIN) desired.x += center.x - pos.x;
        if (pos.y < _bounds.min.y + BOUNDARY_MARGIN) desired.y += center.y - pos.y;
        if (pos.y > _bounds.max.y - BOUNDARY_MARGIN) desired.y += center.y - pos.y;
        if (pos.z < _bounds.min.z + BOUNDARY_MARGIN) desired.z += center.z - pos.z;
        if (pos.z > _bounds.max.z - BOUNDARY_MARGIN) desired.z += center.z - pos.z;

        _velocity = Vector3.Lerp(_velocity,
                                  desired.normalized * _currentSpeed,
                                  Data.turnSpeed * dt);
        _velocity = _velocity.normalized * _currentSpeed;

        transform.position = ClampToBounds(transform.position + _velocity * dt);

        if (_velocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(_velocity, Vector3.up),
                Data.turnSpeed * dt);
        }
    }

    // ── Oscilación vertical pasiva ─────────────────────────────────
    private void ApplyBob()
    {
        _bobPhase += Time.deltaTime * Data.bobFrequency * Mathf.PI * 2f;
        float newBobY = Mathf.Sin(_bobPhase) * Data.bobAmplitude;
        float delta   = newBobY - _prevBobY;
        _prevBobY     = newBobY;

        Vector3 pos = transform.position;
        pos.y += delta;
        transform.position = ClampToBounds(pos);
    }

    // ── Waypoint aleatorio dentro del volumen ─────────────────────
    private Vector3 PickWaypoint()
    {
        float m = BOUNDARY_MARGIN;
        return new Vector3(
            Random.Range(_bounds.min.x + m, _bounds.max.x - m),
            Random.Range(_bounds.min.y + m, _bounds.max.y - m),
            Random.Range(_bounds.min.z + m, _bounds.max.z - m));
    }

    private Vector3 ClampToBounds(Vector3 p)
    {
        p.x = Mathf.Clamp(p.x, _bounds.min.x, _bounds.max.x);
        p.y = Mathf.Clamp(p.y, _bounds.min.y, _bounds.max.y);
        p.z = Mathf.Clamp(p.z, _bounds.min.z, _bounds.max.z);
        return p;
    }

    // ── Desactiva componentes de gameplay del jugador del prefab ───
    private void DisablePlayerComponents()
    {
        var ctrl = GetComponent<ButterflyController>();
        if (ctrl != null) ctrl.enabled = false;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        // Garantiza que haya un Collider para el raycast de selección táctil
        if (GetComponent<Collider>() == null)
        {
            var box  = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(0.5f, 0.5f, 0.5f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_targetWaypoint, 0.3f);
        Gizmos.DrawLine(transform.position, _targetWaypoint);
    }
}
