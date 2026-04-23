using UnityEngine;

/// <summary>
/// Controlador de vuelo de la mariposa integrado con la camara orbital (FixedTouchField).
///
///   Usa los 3 ejes de la camara tal como ButterflyInput los posiciona cada frame,
///   de modo que Vinput empuja en la direccion real de la camara (incluida su Y),
///   permitiendo subir y bajar naturalmente con el joystick.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ButterflyController : MonoBehaviour
{
    // ── Input (escrito por ButterflyInput cada frame) ──────────────
    [HideInInspector] public float Hinput;
    [HideInInspector] public float Vinput;
    [HideInInspector] public float VertInput;

    [Header("Datos de especie")]
    public ButterflyData data;

    // ── Estado interno ─────────────────────────────────────────────
    private Vector3 _moveDir = Vector3.forward;
    private Rigidbody _rb;

    // ── Constantes ─────────────────────────────────────────────────
    private const float GROUND_HOVER_Y = 0.5f;

    // ── Constantes de colision ─────────────────────────────────────
    private const float COLLISION_RAY = 2f;    // longitud de cada rayo
    private const float COLLISION_HOVER = 0.5f;  // margen minimo al rebotar
    private const float COLLISION_PUSH = 8f;    // velocidad de separacion

    // Direcciones que se comprueban cada FixedUpdate.
    // Combina los 6 ejes cardinales con 4 diagonales hacia abajo
    // para cubrir suelo, techo, paredes y esquinas inclinadas.
    private static readonly Vector3[] RAY_DIRECTIONS =
    {
        Vector3.down,                           // suelo
        Vector3.up,                             // techo
        Vector3.forward,                        // frente
        Vector3.back,                           // detras
        Vector3.left,                           // izquierda
        Vector3.right,                          // derecha
        new Vector3( 1f, -1f,  0f).normalized,  // diagonal abajo-derecha
        new Vector3(-1f, -1f,  0f).normalized,  // diagonal abajo-izquierda
        new Vector3( 0f, -1f,  1f).normalized,  // diagonal abajo-frente
        new Vector3( 0f, -1f, -1f).normalized,  // diagonal abajo-detras
    };
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.linearDamping = 2f;
        _rb.angularDamping = 8f;
        _rb.freezeRotation = true;
    }

    private void Start()
    {
        if (data == null)
            Debug.LogWarning("[ButterflyController] No hay ButterflyData asignada.", this);
    }

    private void Update()
    {
        if (data == null) return;
        HandleMovement();
        BobVertically();
    }

    private void FixedUpdate()
    {
        PreventCollisions();
    }

    // ───────────────────────────────────────────────────────────────
    // MOVIMIENTO 3D — incluye eje Y de la camara
    // ───────────────────────────────────────────────────────────────
    private void HandleMovement()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Tomamos los ejes de la camara SIN aplanar sobre el plano horizontal.
        // ButterflyInput posiciona la camara cada frame con una altura y angulo
        // definidos; esos ejes ya llevan la informacion vertical que necesitamos.
        Vector3 camForward = cam.transform.forward; // tiene componente Y segun inclinacion
        Vector3 camRight = cam.transform.right;   // siempre horizontal (sin roll)

        Vector3 input = camForward * Vinput
                      + camRight * Hinput
                      + Vector3.up * VertInput;

        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 inputDir = input.normalized;

            // Suaviza la direccion de movimiento
            _moveDir = Vector3.Lerp(_moveDir, inputDir, Time.deltaTime * data.turnSpeed);

            // Rota el cuerpo hacia donde vuela.
            // Usamos Vector3.up para que la mariposa no quede volcada al bajar en picado.
            // Si prefieres que incline el cuerpo en picado, usa cam.transform.up en su lugar.
            Quaternion targetRot = Quaternion.LookRotation(_moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                                                     Time.deltaTime * data.turnSpeed);
            // Traslada
            transform.position += _moveDir * (data.flightSpeed * Time.deltaTime);
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
        transform.position += Vector3.up * (bob * Time.deltaTime);
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
    // PREVENCION DE COLISIONES EN TODAS LAS DIRECCIONES
    // ───────────────────────────────────────────────────────────────
    private void PreventCollisions()
    {
        foreach (Vector3 dir in RAY_DIRECTIONS)
        {
            if (!Physics.Raycast(transform.position, dir, out RaycastHit hit, COLLISION_RAY))
                continue;

            // Si la mariposa esta mas cerca del obstaculo que el margen de seguridad
            if (hit.distance < COLLISION_HOVER)
            {
                // Empuja en la direccion opuesta al obstaculo
                Vector3 pushDir = -dir;
                transform.position = Vector3.Lerp(
                    transform.position,
                    hit.point + pushDir * COLLISION_HOVER,
                    Time.fixedDeltaTime * COLLISION_PUSH
                );

                // Cancela la componente de velocidad que va HACIA el obstaculo
                // para que no "luche" contra el empuje
                float velocityToward = Vector3.Dot(_rb.linearVelocity, dir);
                if (velocityToward > 0f)
                    _rb.linearVelocity -= dir * velocityToward;
            }
        }
    }

    // ───────────────────────────────────────────────────────────────
    // GIZMOS — visualiza los rayos en el editor
    // ───────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        foreach (Vector3 dir in RAY_DIRECTIONS)
        {
            // Verde = rayo libre, rojo = rayo tocando algo
            bool hit = Physics.Raycast(transform.position, dir, COLLISION_RAY);
            Gizmos.color = hit ? Color.red : Color.green;
            Gizmos.DrawRay(transform.position, dir * COLLISION_RAY);
        }
    }
}