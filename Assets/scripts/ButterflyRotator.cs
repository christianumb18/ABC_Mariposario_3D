using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Permite rotar el prefab 3D de la mariposa arrastrando con el dedo/mouse.
/// Adjunta al GameObject "ButterflyDisplay" que tiene un Collider.
///
/// SETUP:
///   1. Agrega un SphereCollider al ButterflyDisplay (radio ~1.5, Is Trigger = true).
///   2. Asegurate de que la camara de seleccion tiene un PhysicsRaycaster.
///   3. El EventSystem de la escena debe existir (se crea automaticamente con el Canvas).
/// </summary>
public class ButterflyRotator : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Sensibilidad")]
    [Range(0.1f, 5f)] public float horizontalSensitivity = 1.2f;
    [Range(0.1f, 5f)] public float verticalSensitivity = 0.6f;

    [Header("Limite vertical (grados)")]
    [Range(0f, 90f)] public float pitchLimit = 45f;

    [Header("Inercia al soltar")]
    [Range(0f, 20f)] public float inertia = 8f;   // 0 = sin inercia

    // ── Estado compartido con ButterflySelectionUI ─────────────────
    public static bool IsDragging { get; private set; }

    // ── Estado interno ─────────────────────────────────────────────
    private Vector2 _lastPointerPos;
    private Vector2 _velocity;          // velocidad de rotacion para inercia
    private float _currentPitch = 0f;

    // ═══════════════════════════════════════════════════════════════

    private void Update()
    {
        // Aplica inercia cuando el jugador suelta el dedo
        if (!IsDragging && _velocity.sqrMagnitude > 0.01f)
        {
            ApplyRotation(_velocity * Time.deltaTime);
            _velocity = Vector2.Lerp(_velocity, Vector2.zero, Time.deltaTime * inertia);
        }
    }

    // ── Eventos de puntero ─────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        IsDragging = true;
        _lastPointerPos = eventData.position;
        _velocity = Vector2.zero;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsDragging = false;
        // La velocidad acumulada se convierte en inercia
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - _lastPointerPos;
        _lastPointerPos = eventData.position;

        // Guarda velocidad para inercia
        _velocity = delta / Time.deltaTime;

        ApplyRotation(delta);
    }

    // ── Rotacion real ──────────────────────────────────────────────

    private void ApplyRotation(Vector2 delta)
    {
        // Horizontal: gira alrededor del eje Y global
        float yaw = -delta.x * horizontalSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up, yaw, Space.World);

        // Vertical: inclina alrededor del eje X local, con limite
        float pitchDelta = delta.y * verticalSensitivity * Time.deltaTime;
        float newPitch = Mathf.Clamp(_currentPitch + pitchDelta, -pitchLimit, pitchLimit);
        float pitchDiff = newPitch - _currentPitch;
        _currentPitch = newPitch;

        transform.Rotate(Vector3.right, -pitchDiff, Space.Self);
    }
}