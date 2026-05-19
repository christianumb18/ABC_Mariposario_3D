using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adjunta a la Main Camera.
/// Controla la camara orbital y envia input al ButterflyController.
/// </summary>
public class ButterflyUserControl : MonoBehaviour
{
    [Header("Controles tactiles")]
    public FixedJoystick LeftJoystick;
    public FixedTouchField TouchField;
    public Button btnBack;

    [Header("Camara orbital")]
    public float CameraDistance = 8f;
    public float CameraHeight = 8f;
    public float CameraLateral = 10f;

    [Header("Sensibilidad separada por eje")]
    [Tooltip("Sensibilidad horizontal (yaw)")]
    public float YawSensitivity = 0.08f;
    [Tooltip("Sensibilidad vertical (pitch) — mantener menor que yaw")]
    public float PitchSensitivity = 0.05f;

    [Header("Limites de pitch")]
    [Range(-80f, 0f)] public float PitchMin = -60f;
    [Range(0f, 80f)] public float PitchMax = 70f;

    [Header("Suavizado")]
    [Range(1f, 20f)] public float CameraSmoothing = 8f;

    [Header("Filtro de ruido (movil)")]
    [Tooltip("Pixeles minimos de arrastre para considerar movimiento real")]
    public float DeadZone = 4f;

    // ── Estado interno ─────────────────────────────────────────────
    private ButterflyController _control;
    private float _yaw = 180f;
    private float _pitch = 20f;
    private Vector3 _camVelocity;
    private float _verticalButtonInput;

    // Escala de DPI: normaliza la sensibilidad entre pantallas de distinta densidad
    // Un telefono de 420 DPI genera el doble de pixeles que uno de 210 DPI
    // con el mismo arrastre fisico → sin escala, se ve el doble de rapido
    private float _dpiScale = 1f;

    // ═══════════════════════════════════════════════════════════════

    private void Start()
    {
        float dpi = Screen.dpi > 0 ? Screen.dpi : 96f;
        _dpiScale = 96f / dpi;

        btnBack.onClick.AddListener(OnBack);
    }

    private void Update()
    {
        if (_control == null) return;
        HandleCameraOrbit();
        SendInputToController();
        UpdateCameraTransform();
    }

    // ── Orbita de camara ───────────────────────────────────────────
    private void HandleCameraOrbit()
    {
        if (TouchField == null || !TouchField.Pressed) return;

        Vector2 dist = TouchField.TouchDist;

        // Filtra micro-movimientos: dedo quieto en movil genera ruido de 1-2 px
        // que sin dead zone se acumula y mueve la camara solo en el eje Y
        if (dist.magnitude < DeadZone) return;

        _yaw += dist.x * YawSensitivity * _dpiScale;
        _pitch -= dist.y * PitchSensitivity * _dpiScale;
        _pitch = Mathf.Clamp(_pitch, PitchMin, PitchMax);
    }

    // ── Posicion y rotacion de la camara ───────────────────────────
    private void UpdateCameraTransform()
    {
        Vector3 lookTarget = _control.transform.position + Vector3.up * 2f;
        Quaternion orbitRot = Quaternion.Euler(-_pitch, _yaw, 0f);
        Vector3 desiredPos = lookTarget + orbitRot * new Vector3(CameraLateral, CameraHeight, -CameraDistance);

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos, ref _camVelocity, 1f / CameraSmoothing);

        transform.rotation = Quaternion.LookRotation(
            lookTarget - transform.position, Vector3.up);
    }

    // ── Envia input al controlador de vuelo ────────────────────────
    private void SendInputToController()
    {
        if (LeftJoystick == null) return;
        _control.Hinput = LeftJoystick.Direction.x;
        _control.Vinput = LeftJoystick.Direction.y;
        _control.VertInput = _verticalButtonInput;
    }

    // ── API publica ────────────────────────────────────────────────

    public void SetTarget(ButterflyController newController)
    {
        _control = newController;
        if (_control != null)
        {
            _yaw = _control.transform.eulerAngles.y + 180f;
            _pitch = 20f;
        }
    }

    private void OnBack()
    {
        MariposarioGameManager.Instance.LoadSceneSelection();
    }

    public void SetVerticalInput(float value) => _verticalButtonInput = value;
}