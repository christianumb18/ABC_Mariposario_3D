using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Usa SOLO el sistema de eventos de Unity (EventSystems) para calcular TouchDist.
/// Esto garantiza que las coordenadas son siempre del mismo sistema (UI),
/// tanto en editor con mouse como en movil con toque.
/// Se elimina completamente Input.touches y Mouse.current para evitar
/// la mezcla de sistemas de coordenadas
/// </summary>
public class FixedTouchField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [HideInInspector] public Vector2 TouchDist;
    [HideInInspector] public bool Pressed;

    // PointerOld y PointerId se mantienen por compatibilidad con codigo externo
    // pero ya no se usan internamente
    [HideInInspector] public Vector2 PointerOld;
    [HideInInspector] public int PointerId = -1;

    // ═══════════════════════════════════════════════════════════════

    void Update()
    {
        // TouchDist se asigna en OnDrag cada frame que hay movimiento real.
        // Si no hubo OnDrag este frame, se limpia a cero.
        // Esto reemplaza el calculo manual de delta que mezclaba coordenadas.
        if (!Pressed)
            TouchDist = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Pressed = true;
        PointerId = eventData.pointerId;
        PointerOld = eventData.position;
        TouchDist = Vector2.zero;  // evita salto en el primer frame
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!Pressed) return;

        // Delta calculado 100% dentro del sistema de eventos de Unity.
        // eventData.delta es el movimiento del puntero desde el frame anterior,
        // en coordenadas de pantalla consistentes entre mouse y toque.
        TouchDist = eventData.delta;
        PointerOld = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Pressed = false;
        PointerId = -1;
        TouchDist = Vector2.zero;
    }
}