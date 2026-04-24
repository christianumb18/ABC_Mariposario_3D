using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla la pantalla de seleccion de mariposa.
/// Adjunta a un GameObject "SelectionUI" en la escena ButterflySelection.
///
/// SETUP:
///   - Arrastra los ButterflyData assets a la lista species[].
///   - Conecta los campos UI desde el Inspector.
///   - ButterflyDisplay es un GameObject vacio en el centro de la escena
///     donde se instancia el prefab 3D.
/// </summary>
public class ButterflySelectionUI : MonoBehaviour
{
    [Header("Especies")]
    public List<ButterflyData> species = new();

    [Header("Display 3D")]
    public Transform butterflyDisplay;      // Punto central donde aparece el prefab
    public float displayRotationSpeed = 30f; // Rotacion automatica (grados/seg)

    [Header("UI")]
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public Button btnLeft;
    public Button btnRight;
    public Button btnConfirm;
    public TMP_Text confirmLabel;
    public TMP_Text dragHint;

    [Header("Transicion")]
    [Range(0.1f, 1f)]
    public float transitionDuration = 0.35f;

    // ── Estado ─────────────────────────────────────────────────────
    private int _currentIndex = 0;
    private GameObject _activeInstance;
    private bool _isTransitioning;

    // ═══════════════════════════════════════════════════════════════

    private void Start()
    {
        if (species == null || species.Count == 0)
        {
            Debug.LogError("[ButterflySelectionUI] No hay especies asignadas.");
            return;
        }

        btnLeft.onClick.AddListener(PreviousSpecies);
        btnRight.onClick.AddListener(NextSpecies);
        btnConfirm.onClick.AddListener(OnConfirm);

        ShowSpecies(_currentIndex, instant: true);
    }

    // ── Navegacion ─────────────────────────────────────────────────

    public void NextSpecies()
    {
        if (_isTransitioning) return;
        int next = (_currentIndex + 1) % species.Count;
        StartCoroutine(TransitionTo(next, fromRight: true));
    }

    public void PreviousSpecies()
    {
        if (_isTransitioning) return;
        int prev = (_currentIndex - 1 + species.Count) % species.Count;
        StartCoroutine(TransitionTo(prev, fromRight: false));
    }

    // ── Muestra una especie al instante (al iniciar) ───────────────

    private void ShowSpecies(int index, bool instant = false)
    {
        _currentIndex = index;
        ButterflyData data = species[index];

        // Destruye la instancia anterior
        if (_activeInstance != null)
            Destroy(_activeInstance);

        // Instancia el nuevo prefab en el display
        if (data.prefabButterfly != null)
        {
            _activeInstance = Instantiate(data.prefabButterfly, butterflyDisplay.position,
                                          butterflyDisplay.rotation, butterflyDisplay);
            // Elimina scripts de gameplay para que solo sea visual
            StripGameplayScripts(_activeInstance);
        }

        // Actualiza UI
        if (nameText != null) nameText.text = data.speciesName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (confirmLabel != null) confirmLabel.text = $"Volar como {data.speciesName}";

        // Flechas: oculta si solo hay una especie
        bool multipleSpecies = species.Count > 1;
        btnLeft.gameObject.SetActive(multipleSpecies);
        btnRight.gameObject.SetActive(multipleSpecies);

        // Hint de arrastre
        if (dragHint != null) dragHint.gameObject.SetActive(true);
    }

    // ── Transicion animada entre especies ──────────────────────────

    private IEnumerator TransitionTo(int newIndex, bool fromRight)
    {
        _isTransitioning = true;

        // 1. Sale la mariposa actual
        if (_activeInstance != null)
        {
            float dir = fromRight ? -1f : 1f;
            yield return StartCoroutine(SlideOut(_activeInstance, dir));
            Destroy(_activeInstance);
            _activeInstance = null;
        }

        // 2. Actualiza textos mientras el prefab no es visible
        _currentIndex = newIndex;
        ButterflyData data = species[_currentIndex];
        if (nameText != null) nameText.text = data.speciesName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (confirmLabel != null) confirmLabel.text = $"Volar como {data.speciesName}";

        // 3. Instancia la nueva y la hace entrar
        if (data.prefabButterfly != null)
        {
            float dir = fromRight ? 1f : -1f;
            Vector3 offscreen = butterflyDisplay.position + Vector3.right * dir * 4f;

            _activeInstance = Instantiate(data.prefabButterfly, offscreen,
                                          butterflyDisplay.rotation, butterflyDisplay);
            StripGameplayScripts(_activeInstance);

            yield return StartCoroutine(SlideIn(_activeInstance,
                                                 butterflyDisplay.position));
        }

        _isTransitioning = false;
    }

    private IEnumerator SlideOut(GameObject go, float dirX)
    {
        Vector3 start = go.transform.position;
        Vector3 end = start + Vector3.right * dirX * 4f;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            if (go != null) go.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }

    private IEnumerator SlideIn(GameObject go, Vector3 target)
    {
        Vector3 start = go.transform.position;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            if (go != null) go.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        if (go != null) go.transform.position = target;
    }

    // ── Rotacion automatica del prefab ─────────────────────────────

    private void Update()
    {
        if (_activeInstance != null && !ButterflyRotator.IsDragging)
            _activeInstance.transform.Rotate(Vector3.up,
                                              displayRotationSpeed * Time.deltaTime,
                                              Space.World);
    }

    // ── Confirmacion ───────────────────────────────────────────────

    private void OnConfirm()
    {
        if (MariposarioGameManager.Instance == null) return;
        MariposarioGameManager.Instance.SelectSpecies(species[_currentIndex]);
        MariposarioGameManager.Instance.LoadMariposario();
    }

    // ── Quita scripts de gameplay del prefab visual ────────────────
    // Evita que ButterflyController, Rigidbody, etc. interfieran
    // en la pantalla de seleccion.

    private void StripGameplayScripts(GameObject go)
    {
        // Desactiva controladores de gameplay
        foreach (var c in go.GetComponentsInChildren<ButterflyController>())
            c.enabled = false;

        // Desactiva fisica
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }
}