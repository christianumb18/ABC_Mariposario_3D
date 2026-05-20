using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adjunta este script a cada planta hospedera en la escena.
///
/// SETUP:
///   1. Agrega un SphereCollider al GameObject de la planta → Is Trigger = true.
///   2. Ajusta el radio del collider para definir el rango de proximidad.
///   3. Arrastra el boton de huevos al campo layEggButton.
///   4. Asegurate de que la mariposa esta en la layer "Butterfly".
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class HostPlant : MonoBehaviour
{
    [Header("Identificacion")]
    [Tooltip("Debe coincidir exactamente con ButterflyData.hostPlantID de la especie correcta")]
    public string hostPlantID = "";

    private float proximityRadius = 0.5f;

    [Header("Referencias")]
    public MariposarioSpawner spawner;
    private LayerMask butterflyLayer;    // Selecciona la layer "Butterfly" aqui

    // ── Estado ────────────────────────────────────────────────────
    private bool _correctSpeciesNearby;

    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Asegura que el collider trigger existe con el radio correcto
        SphereCollider col = GetComponent<SphereCollider>();
        butterflyLayer = LayerMask.GetMask("Butterfly");
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
            Debug.Log("[HostPlant] SphereCollider agregado automaticamente.");
        }
        col.isTrigger = true;
        col.radius = proximityRadius;

        // El boton empieza deshabilitado
        SetButtonVisible(false);
    }

    // ── Trigger: mariposa entra en rango ──────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (!IsButterfly(other)) return;

        if (SpeciesMatches())
        {
            _correctSpeciesNearby = true;
            SetButtonVisible(true);
            SetTextVisible(true, spawner.textPlantCorrect);
            SetTextVisible(false, spawner.textPlantIncorrect);
        }
        else
        {
            // Especie incorrecta: no muestra el boton
            _correctSpeciesNearby = false;
            SetButtonVisible(false);
            SetTextVisible(false, spawner.textPlantCorrect);
            SetTextVisible(true, spawner.textPlantIncorrect);
        }
    }

    // ── Trigger: mariposa sale del rango ──────────────────────────
    private void OnTriggerExit(Collider other)
    {
        if (!IsButterfly(other)) return;
        _correctSpeciesNearby = false;
        SetButtonVisible(false);
        SetTextVisible(false, spawner.textPlantCorrect);
        SetTextVisible(false, spawner.textPlantIncorrect);
        InactiveAnimation();
    }

    private bool SpeciesMatches()
    {
        if (spawner == null || spawner.ActiveButterfly == null)
        {
            Debug.LogWarning("[HostPlant] No hay mariposa activa en el spawner.", this);
            return false;
        }

        ButterflyController ctrl = spawner.ActiveButterfly;
        ButterflyData data = ctrl.GetData();

        if (data == null)
        {
            Debug.LogWarning("[HostPlant] La mariposa activa no tiene ButterflyData.", this);
            return false;
        }

        bool matches = data.hostPlantID == hostPlantID;

        if (!matches)
            Debug.Log($"[HostPlant] Especie incorrecta. " +
                      $"Planta espera '{hostPlantID}', " +
                      $"mariposa es '{data.hostPlantID}'.");

        Debug.Log($"[HostPlant] Especie {(matches ? "correcta" : "incorrecta")} detectada: '{data.hostPlantID}'.");
        return matches;
    }

    // ── Verifica que el collider pertenece a la mariposa ──────────
    private bool IsButterfly(Collider other)
    {
        return ((1 << other.gameObject.layer) & butterflyLayer) != 0;
    }

    // ── Muestra u oculta el boton ─────────────────────────────────
    private void SetButtonVisible(bool visible)
    {
        if (spawner.layEggButton == null) return;
        spawner.layEggButton.gameObject.SetActive(visible);
    }

    // ── Muestra u oculta el texto ─────────────────────────────────
    private void SetTextVisible(bool visible, TextMeshProUGUI text)
    {
        if (text == null) return;
        text.gameObject.SetActive(visible);
    }

    // ── Gizmo para visualizar el rango en el editor ───────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.25f);
        Gizmos.DrawSphere(transform.position, proximityRadius);
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
    }

    private void InactiveAnimation()
    {
        if (spawner == null || spawner.ActiveButterfly == null) return;
        ButterflyController ctrl = spawner.ActiveButterfly;
        ButterflyAnimator anim = ctrl.GetComponent<ButterflyAnimator>();
        if (anim != null)
        {
            anim.PlayAnimation(ButterflyAnimator.ButterflyAnimation.Flying);
        }
    }

}