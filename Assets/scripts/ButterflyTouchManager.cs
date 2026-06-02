using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Detecta taps sobre mariposas NPC y los reporta al GameStateManager.
/// Solo activo durante GameState.Exploring.
///
/// Estrategia dual: raycast físico → fallback por proximidad en pantalla (fiable en móvil).
/// </summary>
public class ButterflyTouchManager : MonoBehaviour
{
    [Header("Detección")]
    public Camera    mainCamera;
    public LayerMask npcLayerMask;
    public float     maxRayDistance   = 80f;
    [Tooltip("Radio en píxeles para selección táctil (proximidad en pantalla)")]
    public float     tapRadiusPixels  = 120f;
    [Tooltip("Píxeles máximos de arrastre para que sea tap y no drag")]
    public float     maxTapDragPixels = 20f;

    private Vector2 _pointerDownPos;
    private bool    _pointerDown;
    private int     _activeTouchId = -1;

    private readonly List<RaycastResult> _uiResults = new List<RaycastResult>();

    // ─────────────────────────────────────────────────────────────────

    private void OnEnable()  => EnhancedTouchSupport.Enable();
    private void OnDisable() => EnhancedTouchSupport.Disable();

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (npcLayerMask.value == 0) npcLayerMask = ~0;
    }

    private void Update()
    {
        if (GameStateManager.Instance == null) return;
        if (GameStateManager.Instance.CurrentState != GameState.Exploring) return;

        HandleMouse();
        HandleTouch();
    }

    // ── Mouse (editor) ────────────────────────────────────────────────

    private void HandleMouse()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = mouse.position.ReadValue();
            if (IsOverUI(pos)) return;
            _pointerDown    = true;
            _pointerDownPos = pos;
        }

        if (mouse.leftButton.wasReleasedThisFrame && _pointerDown)
        {
            _pointerDown = false;
            Vector2 pos  = mouse.position.ReadValue();
            if (Vector2.Distance(pos, _pointerDownPos) <= maxTapDragPixels)
                TrySelect(_pointerDownPos);
        }

        if (!mouse.leftButton.isPressed) _pointerDown = false;
    }

    // ── Touch (Android) ───────────────────────────────────────────────

    private void HandleTouch()
    {
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase == TouchPhase.Began && _activeTouchId < 0)
            {
                if (IsOverUI(touch.screenPosition)) continue;
                _activeTouchId  = touch.touchId;
                _pointerDownPos = touch.screenPosition;
            }

            if (touch.touchId != _activeTouchId) continue;

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                _activeTouchId = -1;
                if (Vector2.Distance(touch.screenPosition, _pointerDownPos) <= maxTapDragPixels)
                    TrySelect(_pointerDownPos);
            }
        }
    }

    // ── Selección ─────────────────────────────────────────────────────

    private void TrySelect(Vector2 screenPos)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, npcLayerMask))
        {
            var npcHit = hit.collider.GetComponentInParent<ButterflyNPCBehavior>();
            if (npcHit != null && npcHit.enabled)
            {
                GameStateManager.Instance.SelectButterfly(npcHit);
                return;
            }
        }

        var allNpcs = FindObjectsByType<ButterflyNPCBehavior>(FindObjectsSortMode.None);
        ButterflyNPCBehavior closest  = null;
        float                closestD = tapRadiusPixels;

        foreach (var npc in allNpcs)
        {
            if (!npc.enabled) continue;
            Vector3 sp = mainCamera.WorldToScreenPoint(npc.transform.position);
            if (sp.z <= 0f) continue;
            float d = Vector2.Distance(new Vector2(sp.x, sp.y), screenPos);
            if (d < closestD) { closestD = d; closest = npc; }
        }

        if (closest != null)
            GameStateManager.Instance.SelectButterfly(closest);
    }

    // ── UI check — solo bloquea si hay un Selectable (botón, joystick) ──

    private bool IsOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        var ped = new PointerEventData(EventSystem.current) { position = screenPos };
        _uiResults.Clear();
        EventSystem.current.RaycastAll(ped, _uiResults);
        foreach (var r in _uiResults)
            if (r.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null)
                return true;
        return false;
    }
}
