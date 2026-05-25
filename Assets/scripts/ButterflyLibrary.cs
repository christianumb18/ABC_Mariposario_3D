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
public class ButterflyLibrary : MonoBehaviour
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
    public Button btnBack;

    [Header("Transicion")]
    [Range(0.1f, 1f)]
    public float transitionDuration = 0.35f;

    // ── Estado ─────────────────────────────────────────────────────
    private int _currentIndex = 0;
    private GameObject _activeInstanceButterfly;
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
        btnBack.onClick.AddListener(OnBack);

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

        instanceButterfly(data);

        // Actualiza UI
        if (nameText != null) nameText.text = data.speciesName;
        if (descriptionText != null) descriptionText.text = data.description;

        // Flechas: oculta si solo hay una especie
        bool multipleSpecies = species.Count > 1;
        btnLeft.gameObject.SetActive(multipleSpecies);
        btnRight.gameObject.SetActive(multipleSpecies);
    }

    private void instanceButterfly(ButterflyData data)
    {
        // Destruye la instancia anterior
        if (_activeInstanceButterfly != null)
            Destroy(_activeInstanceButterfly);


        // Instancia el nuevo prefab en el display
        if (data.prefabButterfly != null)
        {
            _activeInstanceButterfly = Instantiate(data.prefabButterfly, butterflyDisplay.position,
                                          butterflyDisplay.rotation, butterflyDisplay);

            // ── ButterflyAnimator ──────────────────────────────────────
            ButterflyAnimator _activeAnimator = _activeInstanceButterfly.GetComponent<ButterflyAnimator>();
            if (_activeAnimator != null)
            {
                // Activa la animacion de vuelo apenas aparece la mariposa
                _activeAnimator.PlayAnimation(ButterflyAnimator.ButterflyAnimation.Preview);
            }
            else
            {
                Debug.LogWarning($"[MariposarioSpawner] '{data.prefabButterfly.name}' no tiene ButterflyAnimator. " +
                                  "Agrega el script al prefab si quieres controlar animaciones.");
            }

            // Elimina scripts de gameplay para que solo sea visual
            StripGameplayScripts(_activeInstanceButterfly);
        }
    }

    // ── Transicion animada entre especies ──────────────────────────

    private IEnumerator TransitionTo(int newIndex, bool fromRight)
    {
        _isTransitioning = true;

        // 1. Sale la mariposa actual
        if (_activeInstanceButterfly != null)
        {
            float dir = fromRight ? -1f : 1f;
            yield return StartCoroutine(SlideOut(_activeInstanceButterfly, dir));
            Destroy(_activeInstanceButterfly);
            _activeInstanceButterfly = null;
        }

        // 2. Actualiza textos mientras el prefab no es visible
        _currentIndex = newIndex;
        ButterflyData data = species[_currentIndex];
        if (nameText != null) nameText.text = data.speciesName;
        if (descriptionText != null) descriptionText.text = data.description;

        // 3. Instancia la nueva y la hace entrar
        if (data.prefabButterfly != null)
        {
            float dir = fromRight ? 1f : -1f;
            Vector3 offscreen = butterflyDisplay.position + Vector3.right * dir * 4f;

            _activeInstanceButterfly = Instantiate(data.prefabButterfly, offscreen,
                                          butterflyDisplay.rotation, butterflyDisplay);
            StripGameplayScripts(_activeInstanceButterfly);

            yield return StartCoroutine(SlideIn(_activeInstanceButterfly,
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
        if (_activeInstanceButterfly != null && !ButterflyRotator.IsDragging)
            _activeInstanceButterfly.transform.Rotate(Vector3.up,
                                              displayRotationSpeed * Time.deltaTime,
                                              Space.World);
    }

    private void OnBack()
    {
        MariposarioGameManager.Instance.LoadSceneMenu();
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

    // Limpieza por si la escena se descarga de otra forma (boton atras, etc.)
    private void OnDestroy()
    {
        if (_activeInstanceButterfly != null)
            Destroy(_activeInstanceButterfly);
    }
}