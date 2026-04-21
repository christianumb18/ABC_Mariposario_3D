using UnityEngine;

/// <summary>
/// Adjunta a la Main Camera.
/// NO usa GetComponent<ButterflyController>() en Start porque la camara
/// no tiene ese script. La referencia se asigna desde ButterflySelector
/// cada vez que se instancia un nuevo prefab de mariposa.
/// </summary>
public class ButterflyUserControl : MonoBehaviour
{
    [Header("Controles tactiles")]
    public FixedJoystick LeftJoystick;
    public FixedTouchField TouchField;

    [Header("Camara orbital")]
    public float CameraDistance = 20f;
    public float CameraHeight = 15f;
    public float CameraLateral = 4f;
    public float YawSensitivity = 0.15f;
    public float PitchSensitivity = 0.12f;

    [Range(-80f, 0f)] public float PitchMin = -60f;
    [Range(0f, 80f)] public float PitchMax = 70f;
    [Range(1f, 20f)] public float CameraSmoothing = 8f;

    // ── Estado interno ─────────────────────────────────────────────
    private ButterflyController _control;   // se asigna desde SetTarget()
    private float _yaw = 180f;
    private float _pitch = 20f;
    private Vector3 _camVelocity;
    private float _verticalButtonInput;

    // ═══════════════════════════════════════════════════════════════

    private void Update()
    {
        // Solo procesa si ya hay una mariposa instanciada
        if (_control == null) return;

        HandleCameraOrbit();
        SendInputToController();
        UpdateCameraTransform();
    }

    // ── Orbita de camara ───────────────────────────────────────────
    private void HandleCameraOrbit()
    {
        if (TouchField == null || !TouchField.Pressed) return;
        _yaw += TouchField.TouchDist.x * YawSensitivity;
        _pitch -= TouchField.TouchDist.y * PitchSensitivity;
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

    /// <summary>
    /// Llamado desde ButterflySelector cada vez que se instancia un nuevo prefab.
    /// </summary>
    public void SetTarget(ButterflyController newController)
    {
        _control = newController;
        if (_control != null)
        {
            _yaw = _control.transform.eulerAngles.y + 180f;
            _pitch = 20f;
        }
    }

    /// <summary>Conecta a botones UI de subir/bajar.</summary>
    public void SetVerticalInput(float value) => _verticalButtonInput = value;
}