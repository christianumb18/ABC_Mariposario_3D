using UnityEngine;

/// <summary>
/// Controlador de vuelo de la mariposa integrado con la camara orbital (FixedTouchField).
///
/// CAMBIO PRINCIPAL:
///   HandleMovement ya NO proyecta camForward sobre el plano horizontal.
///   Usa los 3 ejes de la camara tal como ButterflyInput los posiciona cada frame,
///   de modo que Vinput empuja en la direccion real de la camara (incluida su Y),
///   permitiendo subir y bajar naturalmente con el joystick.
/// </summary>
public class ButterflyController : MonoBehaviour
{
    // ── Input (escrito por ButterflyInput cada frame) ──────────────
    [HideInInspector] public float Hinput;    // Joystick X  (-1..1)
    [HideInInspector] public float Vinput;    // Joystick Y  (-1..1)
    [HideInInspector] public float VertInput; // Subir/bajar dedicado (-1..1) — opcional

    [Header("Datos de especie")]
    public ButterflyData data;

    [Header("Alas")]
    public Transform leftWing;
    public Transform rightWing;

    // ── Estado interno ─────────────────────────────────────────────
    private Vector3 _moveDir = Vector3.forward;

    // ═══════════════════════════════════════════════════════════════

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

        // Vinput -> avanza/retrocede en la direccion a la que apunta la camara (con Y).
        // Hinput -> desplaza lateralmente.
        // VertInput -> subida/bajada pura en espacio mundo (boton Q/E o segundo eje).
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
    // BOB VERTICAL PASIVO (solo cuando el jugador no mueve el joystick)
    // ───────────────────────────────────────────────────────────────
    private void BobVertically()
    {
        if (Mathf.Abs(Hinput) > 0.05f || Mathf.Abs(Vinput) > 0.05f) return;

        float bob = Mathf.Sin(Time.time * data.bobFrequency * Mathf.PI * 2f)
                  * data.bobAmplitude;
        transform.position += Vector3.up * (bob * Time.deltaTime);
    }

    // ───────────────────────────────────────────────────────────────
    // API publica
    // ───────────────────────────────────────────────────────────────
    public void Initialize(ButterflyData newData)
    {
        data = newData;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            var mat = r.material;
            if (mat.HasProperty("_Color"))
                mat.color = data.wingTint;
        }
    }
}