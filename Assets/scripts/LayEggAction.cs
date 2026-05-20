using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Maneja la accion de poner huevos.
/// Adjunta al mismo GameObject que HostPlant, o a un GameObject gestor de la escena.
///
/// SETUP:
///   1. Arrastra el boton de huevos a layEggButton.
///   2. El boton llama a OnLayEggPressed() desde su onClick.
///   3. Arrastra el MariposarioSpawner para obtener la mariposa activa.
/// </summary>
public class LayEggAction : MonoBehaviour
{
    [Header("Referencias")]
    public MariposarioSpawner spawner;       // Para obtener la mariposa activa

    [Header("Configuracion")]
    [Tooltip("Segundos que dura la animacion de poner huevos")]
    public float layEggDuration = 3f;

    // ── Estado ────────────────────────────────────────────────────
    private bool _isLayingEgg;

    // ═══════════════════════════════════════════════════════════════

    private void Start()
    {
        if (spawner.layEggButton != null)
            spawner.layEggButton.onClick.AddListener(OnLayEggPressed);
    }

    // ── Se llama al presionar el boton ────────────────────────────
    public void OnLayEggPressed()
    {
        if (_isLayingEgg) return;
        StartCoroutine(LayEggRoutine());
    }

    private IEnumerator LayEggRoutine()
    {
        _isLayingEgg = true;

        // Obtiene el animador de la mariposa activa
        ButterflyAnimator anim = GetButterflyAnimator();
        ButterflyController ctrl = spawner ? spawner.ActiveButterfly : null;

        // Desactiva el movimiento mientras pone huevos
        if (ctrl != null) ctrl.enabled = false;

        // Reproduce animacion de poner huevos
        if (anim != null)
            anim.PlayAnimation(ButterflyAnimator.ButterflyAnimation.Passive);

        // Oculta el boton mientras dura la accion
        spawner.layEggButton.gameObject.SetActive(false);

        // Espera la duracion de la animacion
        yield return new WaitForSeconds(layEggDuration);

        // Vuelve a vuelo normal
        if (anim != null)
            anim.PlayAnimation(ButterflyAnimator.ButterflyAnimation.Flying);

        if (ctrl != null) ctrl.enabled = true;

        _isLayingEgg = false;
    }

    private ButterflyAnimator GetButterflyAnimator()
    {
        if (spawner == null || spawner.ActiveButterfly == null) return null;
        return spawner.ActiveButterfly.GetComponent<ButterflyAnimator>();
    }
}