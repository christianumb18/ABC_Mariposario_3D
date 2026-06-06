using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Durante GameState.Inspecting encierra la mariposa en un wrapper vacío y lo rota.
/// Auto-rota cuando no hay input; al arrastrar el usuario toma control para
/// mirarla desde cualquier ángulo.
///
/// Lee Mouse/Touchscreen directo del New Input System (no depende del EventSystem).
/// </summary>
public class ButterflyInspector : MonoBehaviour
{
    [Tooltip("Grados/segundo de auto-rotación cuando no hay input")]
    public float autoRotateSpeed   = 18f;
    [Tooltip("Grados/píxel al arrastrar manualmente")]
    public float rotateSensitivity = 0.35f;
    [Tooltip("Movimiento mínimo (px²) para considerar drag y no tap")]
    public float dragDeadzoneSqr   = 0.5f;

    private Camera               _cam;
    private ButterflyNPCBehavior _inspectedNPC;
    private Transform            _originalParent;
    private GameObject           _wrapper;
    private Animator             _npcAnimator;
    private bool                 _prevInspecting;

    // ─────────────────────────────────────────────────────────────────

    private void Start() => _cam = Camera.main;

    private void Update()
    {
        bool inspecting = GameStateManager.Instance != null &&
                          GameStateManager.Instance.CurrentState == GameState.Inspecting;

        if ( inspecting && !_prevInspecting) OnEnterInspecting();
        if (!inspecting &&  _prevInspecting) OnExitInspecting();
        _prevInspecting = inspecting;

        if (!inspecting || _wrapper == null) return;

        bool userDragged = false;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            Vector2 d = mouse.delta.ReadValue();
            if (d.sqrMagnitude > dragDeadzoneSqr)
            {
                Rotate(d, _wrapper.transform);
                userDragged = true;
            }
        }

        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed)
        {
            Vector2 d = ts.primaryTouch.delta.ReadValue();
            if (d.sqrMagnitude > dragDeadzoneSqr)
            {
                Rotate(d, _wrapper.transform);
                userDragged = true;
            }
        }

        if (!userDragged)
            _wrapper.transform.Rotate(Vector3.up, autoRotateSpeed * Time.deltaTime, Space.World);
    }

    // ── Transiciones ──────────────────────────────────────────────────

    private void OnEnterInspecting()
    {
        _inspectedNPC = GameStateManager.Instance?.SelectedNPC;
        if (_inspectedNPC == null) return;

        _originalParent = _inspectedNPC.transform.parent;

        _wrapper = new GameObject("ButterflyRotWrapper");
        _wrapper.transform.SetPositionAndRotation(
            _inspectedNPC.transform.position,
            _inspectedNPC.transform.rotation);
        _inspectedNPC.transform.SetParent(_wrapper.transform, worldPositionStays: true);

        _npcAnimator = _inspectedNPC.GetComponentInChildren<Animator>();
        if (_npcAnimator != null) _npcAnimator.applyRootMotion = false;
    }

    private void OnExitInspecting()
    {
        if (_inspectedNPC != null && _wrapper != null)
            _inspectedNPC.transform.SetParent(_originalParent, worldPositionStays: true);

        if (_wrapper     != null) { Destroy(_wrapper);  _wrapper     = null; }
        if (_npcAnimator != null) { _npcAnimator.applyRootMotion = true; _npcAnimator = null; }
        _inspectedNPC   = null;
        _originalParent = null;
    }

    // ── Rotación ──────────────────────────────────────────────────────

    private void Rotate(Vector2 delta, Transform t)
    {
        t.Rotate(Vector3.up, -delta.x * rotateSensitivity, Space.World);
        if (_cam == null) _cam = Camera.main;
        if (_cam != null)
            t.Rotate(_cam.transform.right, delta.y * rotateSensitivity, Space.World);
    }
}
