using UnityEngine;

/// <summary>
/// Lee el joystick y el campo táctil y los aplica al ButterflyController y a la cámara.
///
/// MEJORAS RESPECTO A LA VERSION ANTERIOR:
///   - La cámara orbita en YAW (horizontal) Y en PITCH (vertical) con el TouchField.
///   - El pitch de la cámara se limita para que nunca quede boca abajo ni mire desde abajo.
///   - ButterflyController.VertInput se puede alimentar con un segundo joystick o con
///     los botones de pantalla (llama a SetVerticalInput desde un botón UI).
///   - La posición de la cámara se calcula desde ángulos esféricos para que el pitch
///     traslade el forward de la cámara hacia arriba/abajo correctamente.
/// </summary>
[RequireComponent(typeof(ButterflyController))]
public class ButterflyUserControl : MonoBehaviour
{
    // ── Referencias ────────────────────────────────────────────────
    [Header("Controles táctiles")]
    public FixedJoystick LeftJoystick;
    public FixedTouchField TouchField;

    // ── Opciones de cámara ─────────────────────────────────────────
    [Header("Cámara orbital")]
    [Tooltip("Distancia horizontal de la cámara al jugador")]
    public float CameraDistance = 20f;

    [Tooltip("Altura base de la cámara sobre la mariposa")]
    public float CameraHeight = 15f;

    [Tooltip("Offset lateral fijo (ajusta la composición)")]
    public float CameraLateral = 4f;

    [Tooltip("Sensibilidad del arrastre horizontal (yaw)")]
    public float YawSensitivity = 0.15f;

    [Tooltip("Sensibilidad del arrastre vertical (pitch)")]
    public float PitchSensitivity = 0.12f;

    [Tooltip("Límite inferior del pitch (grados, negativo = mira hacia abajo)")]
    [Range(-80f, 0f)]
    public float PitchMin = -60f;

    [Tooltip("Límite superior del pitch (grados, positivo = mira hacia arriba)")]
    [Range(0f, 80f)]
    public float PitchMax = 70f;

    // ── Suavizado ─────────────────────────────────────────────────
    [Header("Suavizado de cámara")]
    [Range(1f, 20f)] public float CameraSmoothing = 8f;

    // ── Estado interno ─────────────────────────────────────────────
    private ButterflyController _control;
    private float _yaw = 180f;  // Inicia detrás del jugador
    private float _pitch = 20f;   // Ángulo vertical inicial
    private Vector3 _camVelocity; // Para SmoothDamp

    // ── Input vertical externo (botones UI de subir/bajar) ─────────
    private float _verticalButtonInput = 0f;

    // ═══════════════════════════════════════════════════════════════

    private void Start()
    {
        _control = GetComponent<ButterflyController>();

        // Inicializa el yaw apuntando detrás de la mariposa
        _yaw = transform.eulerAngles.y + 180f;
    }

    private void Update()
    {
        HandleCameraOrbit();
        SendInputToController();
        UpdateCameraTransform();
    }

    // ───────────────────────────────────────────────────────────────
    // 1. Lee el TouchField y actualiza los ángulos de órbita
    // ───────────────────────────────────────────────────────────────
    private void HandleCameraOrbit()
    {
        if (TouchField == null || !TouchField.Pressed) return;

        // Arrastre horizontal → gira alrededor del eje Y (yaw)
        _yaw += TouchField.TouchDist.x * YawSensitivity;

        // Arrastre vertical → inclina la cámara arriba/abajo (pitch)
        // Restamos porque arrastrar hacia arriba debe subir la cámara
        _pitch -= TouchField.TouchDist.y * PitchSensitivity;
        _pitch = Mathf.Clamp(_pitch, PitchMin, PitchMax);
    }

    // ───────────────────────────────────────────────────────────────
    // 2. Calcula la posición y rotación de la cámara con coordenadas esféricas
    // ───────────────────────────────────────────────────────────────
    private void UpdateCameraTransform()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Punto que la cámara debe mirar (ligeramente por encima de la mariposa)
        Vector3 lookTarget = transform.position + Vector3.up * 2f;

        // Coordenadas esféricas → posición en mundo
        // yaw   = rotación horizontal alrededor del jugador
        // pitch = elevación vertical de la cámara
        Quaternion orbitRot = Quaternion.Euler(-_pitch, _yaw, 0f);

        // Radio de la esfera: usamos CameraDistance en el plano y CameraHeight como
        // offset adicional en Y para que el pitch mueva la cámara en arco real.
        // Vector base: la cámara parte desde atrás y a la derecha (CameraLateral)
        Vector3 orbitOffset = new Vector3(CameraLateral, CameraHeight, -CameraDistance);
        Vector3 desiredPos = lookTarget + orbitRot * orbitOffset;

        // Suavizado de posición
        cam.transform.position = Vector3.SmoothDamp(
            cam.transform.position,
            desiredPos,
            ref _camVelocity,
            1f / CameraSmoothing
        );

        // La cámara siempre mira al punto objetivo
        cam.transform.rotation = Quaternion.LookRotation(
            lookTarget - cam.transform.position,
            Vector3.up
        );
    }

    // ───────────────────────────────────────────────────────────────
    // 3. Envía los valores de input al ButterflyController
    // ───────────────────────────────────────────────────────────────
    private void SendInputToController()
    {
        if (_control == null || LeftJoystick == null) return;

        _control.Hinput = LeftJoystick.Direction.x;
        _control.Vinput = LeftJoystick.Direction.y;

        // VertInput: botones UI (ver SetVerticalInput) o segundo eje de joystick
        _control.VertInput = _verticalButtonInput;
    }

    // ───────────────────────────────────────────────────────────────
    // API pública — conecta botones UI de subir/bajar
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Llama desde el OnPointerDown de un botón UI "Subir" con value = 1,
    /// y desde "Bajar" con value = -1. Llama con 0 en OnPointerUp.
    /// </summary>
    public void SetVerticalInput(float value)
    {
        _verticalButtonInput = value;
    }

    /// <summary>
    /// Útil si quieres que la cámara empiece detrás del jugador al spawnear.
    /// </summary>
    public void ResetCameraAngle()
    {
        _yaw = transform.eulerAngles.y + 180f;
        _pitch = 20f;
    }
}