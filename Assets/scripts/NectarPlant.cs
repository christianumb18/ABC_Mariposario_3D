using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Planta nectarifera. Cualquier mariposa puede alimentarse de ella (no valida especie).
/// Al acercarse: muestra el boton "Comer" y un mensaje.
/// Al comer: suma puntos y recupera 1 corazon de vida (tope 6).
///
/// SETUP:
///   1. Agrega un SphereCollider al prefab de la planta -> Is Trigger = true.
///   2. Ajusta el radio del collider para el rango de proximidad.
///   3. Arrastra el MariposarioSpawner al campo spawner.
///   4. Asegurate de que la mariposa esta en la layer "Butterfly".
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class NectarPlant : MonoBehaviour
{
    [Header("Referencias")]
    public MariposarioSpawner spawner;

    [Header("Nombre visible (para la UI)")]
    [Tooltip("Nombre comun o cientifico que se muestra al jugador al detectarla.")]
    public string displayName = "";
    [Tooltip("Traduccion en ingles. Si esta vacio se usa displayName.")]
    public string displayNameEn = "";

    [Header("Configuracion")]
    [Tooltip("Puntos que otorga al comer")]
    public int nectarPoints = 20;

    [Tooltip("Radio de proximidad para detectar la mariposa")]
    public float proximityRadius = 0.005f;

    [Tooltip("Segundos que dura la animacion de comer antes de volver a volar")]
    public float eatAnimationDuration = 1.5f;

    // ── Estado ────────────────────────────────────────────────────
    private LayerMask butterflyLayer;
    private bool _butterflyNearby;

    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        butterflyLayer = LayerMask.GetMask("Butterfly");
        col.isTrigger = true;
        col.radius = proximityRadius;

        SetButtonVisible(false);
        SetTextVisible(false);
    }

    // ── Trigger: mariposa entra en rango ──────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (!IsButterfly(other)) return;
         

        _butterflyNearby = true;
        SetButtonVisible(true);
        // Activar PRIMERO (dispara LocalizedText.OnEnable que pondria la clave
        // base), DESPUES inyectar el nombre — asi nuestro texto queda al final.
        SetTextVisible(true);
        UpdateNectarText();

        // El boton ejecuta Eat() al pulsarlo
        if (spawner != null && spawner.nectarButton != null)
        {
            spawner.nectarButton.onClick.RemoveAllListeners();
            spawner.nectarButton.onClick.AddListener(Eat);
        }
    }

    // ── Trigger: mariposa sale del rango ──────────────────────────
    private void OnTriggerExit(Collider other)
    {
        if (!IsButterfly(other)) return;
        _butterflyNearby = false;
        SetButtonVisible(false);
        SetTextVisible(false);
    }

    // ── Accion de comer ───────────────────────────────────────────
    private void Eat()
    {
        if (!_butterflyNearby) return;
        if (spawner == null || spawner.ActiveButterfly == null) return;

        ButterflyData data = spawner.ActiveButterfly.GetData();
        if (data == null) return;
        string speciesID = data.speciesName;
        if (string.IsNullOrEmpty(speciesID)) return;

        // Suma puntos
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.AddScoreToSpecies(speciesID, nectarPoints);

        // Recupera 1 vida (tope 6)
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.GainLife(speciesID);

        // Animacion de posarse temporal, luego vuelve a volar
        ButterflyAnimator anim = spawner.ActiveButterfly.GetComponent<ButterflyAnimator>();
        if (anim != null)
        {
            anim.PlayAnimation(ButterflyAnimator.ButterflyAnimation.Passive);
            StartCoroutine(ReturnToFlying(anim));
        }

        Debug.Log($"[NectarPlant] '{speciesID}' comio nectar. +{nectarPoints} puntos, +1 vida.");
    }

    private IEnumerator ReturnToFlying(ButterflyAnimator anim)
    {
        yield return new WaitForSeconds(eatAnimationDuration);
        if (anim != null)
            anim.PlayAnimation(ButterflyAnimator.ButterflyAnimation.Flying);
    }

    // ── Verifica que el collider pertenece a la mariposa ──────────
    private bool IsButterfly(Collider other)
    {
        // Debe estar en la layer Butterfly
        if (((1 << other.gameObject.layer) & butterflyLayer) == 0) return false;

        // Y ademas debe ser LA mariposa del jugador, no una NPC.
        // Comparamos contra la ActiveButterfly del spawner.
        if (spawner == null || spawner.ActiveButterfly == null) return false;

        Transform playerRoot = spawner.ActiveButterfly.transform.root;
        return other.transform.root == playerRoot;
    }

    // ── Muestra u oculta el boton ─────────────────────────────────
    private void SetButtonVisible(bool visible)
    {
        if (spawner == null || spawner.nectarButton == null) return;
        spawner.nectarButton.gameObject.SetActive(visible);
    }

    // ── Muestra u oculta el texto ─────────────────────────────────
    private void SetTextVisible(bool visible)
    {
        if (spawner == null || spawner.textNectar == null) return;
        spawner.textNectar.gameObject.SetActive(visible);
    }

    // ── Inyecta el nombre de la planta en el texto ────────────────
    private void UpdateNectarText()
    {
        if (spawner == null || spawner.textNectar == null) return;
        bool en = LocalizationManager.Instance != null
                  && LocalizationManager.Instance.CurrentLanguage == 1;
        string name = (en && !string.IsNullOrEmpty(displayNameEn)) ? displayNameEn : displayName;
        string prefix = en ? "Nectar plant identified" : "Planta nectarífera identificada";
        spawner.textNectar.text = string.IsNullOrEmpty(name)
            ? prefix
            : $"{prefix}: <b>{name}</b>";
    }

    // ── Gizmo para ver el rango en el editor ──────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.25f, 1f, 0.45f, 0.25f);
        Gizmos.DrawSphere(transform.position, proximityRadius);
        Gizmos.color = new Color(0.25f, 1f, 0.45f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
    }
}