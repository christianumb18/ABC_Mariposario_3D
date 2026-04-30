using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class ButterflyController : MonoBehaviour
{
    // ── Input ──────────────────────────────────────────────────────
    [HideInInspector] public float Hinput;
    [HideInInspector] public float Vinput;
    [HideInInspector] public float VertInput;

    private ButterflyData data;

    // ── Componentes ────────────────────────────────────────────────
    private Rigidbody _rb;
    private SphereCollider _sphC;

    // ── Estado interno ─────────────────────────────────────────────
    private Vector3 _moveDir = Vector3.forward;

    // ── Colision ───────────────────────────────────────────────────
    private const float COLLISION_RAY = 1.5f;
    private const float COLLISION_HOVER = 0.5f;
    private const float COLLISION_PUSH = 8f;

    // Solo direcciones que tienen sentido para una mariposa volando:
    // quitamos Vector3.up porque no hay techo en un mariposario abierto
    // y era la causa del "techo fantasma" al subir.
    // Si tu escena tiene techo real, vuelve a agregarlo.
    private static readonly Vector3[] RAY_DIRECTIONS =
    {
        Vector3.down,
        Vector3.up,
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right,
        new Vector3( 1f, -1f,  0f).normalized,
        new Vector3(-1f, -1f,  0f).normalized,
        new Vector3( 0f, -1f,  1f).normalized,
        new Vector3( 0f, -1f, -1f).normalized,
    };

    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.linearDamping = 2f;
        _rb.angularDamping = 8f;
        _rb.freezeRotation = true;

        _sphC = GetComponent<SphereCollider>();
        _sphC.radius = 2.2f;
        _sphC.center = new Vector3(0f, 1.77f, -0.54f);

        // El collider propio nunca debe activar los rayos de colision.
        // Asigna la mariposa a su propia layer (ej. "Butterfly") y
        // excluye esa layer del raycast para evitar auto-colision.
        // Si no usas layers, Physics.IgnoreCollision es la alternativa:
        // se configura en Initialize() cuando ya tenemos los datos.
    }

    private void Start()
    {
        if (data == null)
            Debug.LogWarning("[ButterflyController] No hay ButterflyData asignada.", this);
    }

    // Todo el movimiento va en FixedUpdate para ser consistente con la fisica
    private void FixedUpdate()
    {
        if (data == null) return;
        HandleMovement();
        BobVertically();
        PreventCollisions();
    }

    // ───────────────────────────────────────────────────────────────
    // MOVIMIENTO 3D
    // ───────────────────────────────────────────────────────────────
    private void HandleMovement()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;

        Vector3 input = camForward * Vinput
                      + camRight * Hinput
                      + Vector3.up * VertInput;

        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 inputDir = input.normalized;

            _moveDir = Vector3.Lerp(_moveDir, inputDir,
                                    Time.fixedDeltaTime * data.turnSpeed);

            Quaternion targetRot = Quaternion.LookRotation(_moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                                                     Time.fixedDeltaTime * data.turnSpeed);

            // Usa MovePosition en lugar de transform.position para que
            // la fisica de Unity sepa a donde va el objeto este frame
            _rb.MovePosition(transform.position
                             + _moveDir * (data.flightSpeed * Time.fixedDeltaTime));
        }
    }

    // ───────────────────────────────────────────────────────────────
    // BOB VERTICAL PASIVO
    // ───────────────────────────────────────────────────────────────
    private void BobVertically()
    {
        if (Mathf.Abs(Hinput) > 0.05f || Mathf.Abs(Vinput) > 0.05f) return;

        float bob = Mathf.Sin(Time.time * data.bobFrequency * Mathf.PI * 2f)
                  * data.bobAmplitude;
        _rb.MovePosition(transform.position + Vector3.up * (bob * Time.fixedDeltaTime));
    }

    // ───────────────────────────────────────────────────────────────
    // PREVENCION DE COLISIONES
    // ───────────────────────────────────────────────────────────────
    private void PreventCollisions()
    {
        int butterflyLayer = LayerMask.GetMask("Butterfly");

        foreach (Vector3 dir in RAY_DIRECTIONS)
        {
            // Ignora la propia layer de la mariposa en el raycast
            // para que el SphereCollider no se detecte a si mismo
            int mask = ~(1 << butterflyLayer);

            if (!Physics.Raycast(transform.position, dir, out RaycastHit hit,
                                 COLLISION_RAY, mask))
                continue;

            if (hit.distance >= COLLISION_HOVER) continue;

            // Empuja en direccion opuesta al obstaculo
            Vector3 safePos = hit.point + (-dir) * COLLISION_HOVER;
            _rb.MovePosition(Vector3.Lerp(transform.position, safePos,
                                          Time.fixedDeltaTime * COLLISION_PUSH));

            // Cancela velocidad hacia el obstaculo
            float velToward = Vector3.Dot(_rb.linearVelocity, dir);
            if (velToward > 0f)
                _rb.linearVelocity -= dir * velToward;
        }
    }

    // ───────────────────────────────────────────────────────────────
    // API PUBLICA
    // ───────────────────────────────────────────────────────────────
    public void Initialize(ButterflyData newData)
    {
        data = newData;
        transform.localScale = Vector3.one * data.scale;
    }

    // ───────────────────────────────────────────────────────────────
    // GIZMOS
    // ───────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        int mask = ~(1 << gameObject.layer);
        foreach (Vector3 dir in RAY_DIRECTIONS)
        {
            bool hit = Physics.Raycast(transform.position, dir, COLLISION_RAY, mask);
            Gizmos.color = hit ? Color.red : Color.green;
            Gizmos.DrawRay(transform.position, dir * COLLISION_RAY);
        }
    }

    /// <summary>Devuelve los datos de especie. Usado por HostPlant para validar.</summary>
    public ButterflyData GetData() => data;
}