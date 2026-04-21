using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Adjunta SOLO al ButterflySpawnPoint.
/// 
/// Responsabilidades:
///   1. Instanciar el prefab correcto cuando el jugador elige una especie.
///   2. Destruir el prefab anterior.
///   3. Notificar a ButterflyInput (en la camara) con SetTarget().
///   4. Notificar al GameManager via evento onSpeciesSelected.
/// </summary>
public class ButterflySelector : MonoBehaviour
{
    [Header("Especies disponibles")]
    public List<ButterflyData> species = new();

    [Header("UI de seleccion")]
    public GameObject selectionPanel;
    public Transform buttonContainer;
    public GameObject buttonPrefab;
    public TMP_Text descriptionText;
    public Image previewIcon;

    [Header("Referencia a la camara (ButterflyInput)")]
    public ButterflyUserControl cameraInput;       // Arrastra la Main Camera aqui

    /// <summary>Notifica al GameManager cuando cambia la especie.</summary>
    public event Action<ButterflyData> onSpeciesSelected;

    // ── Estado ────────────────────────────────────────────────────
    private bool _isOpen;
    private ButterflyController _activeButterfly;   // instancia actual en escena

    // ═══════════════════════════════════════════════════════════════

    private void Start()
    {
        BuildButtons();
        //selectionPanel.SetActive(false);

        // Carga la primera especie automaticamente al iniciar
        if (species.Count > 0)
            SelectSpecies(species[1]);
    }

    // ── Construye los botones del menu ────────────────────────────
    private void BuildButtons()
    {
        //foreach (Transform child in buttonContainer)
        //    Destroy(child.gameObject);

        //foreach (var bd in species)
        //{
        //    GameObject btn = Instantiate(buttonPrefab, buttonContainer);

        //    var img = btn.GetComponentInChildren<Image>();
        //    if (img != null && bd.icon != null) img.sprite = bd.icon;

        //    var label = btn.GetComponentInChildren<TMP_Text>();
        //    if (label != null) label.text = bd.speciesName;

        //    ButterflyData captured = bd;
        //    btn.GetComponent<Button>().onClick.AddListener(() => SelectSpecies(captured));
        //}
    }

    // ── Seleccion de especie ──────────────────────────────────────
    public void SelectSpecies(ButterflyData selected)
    {
        if (selected == null) return;

        // Actualiza descripcion en UI
        if (descriptionText != null)
            descriptionText.text = $"<b>{selected.speciesName}</b>\n{selected.description}";
        if (previewIcon != null && selected.icon != null)
            previewIcon.sprite = selected.icon;

        SpawnButterfly(selected);
        onSpeciesSelected?.Invoke(selected);
        CloseMenu();
    }

    // ── Instancia el prefab correcto ──────────────────────────────
    private void SpawnButterfly(ButterflyData data)
    {
        if (data.prefabButterfly == null)
        {
            Debug.LogError($"[ButterflySelector] '{data.speciesName}' no tiene prefab asignado.", this);
            return;
        }

        // Destruye la mariposa anterior si existe
        if (_activeButterfly != null)
            Destroy(_activeButterfly.gameObject);

        // Instancia el nuevo prefab en la posicion del SpawnPoint
        GameObject go = Instantiate(data.prefabButterfly, transform.position, transform.rotation);
        _activeButterfly = go.GetComponent<ButterflyController>();

        if (_activeButterfly == null)
        {
            Debug.LogError($"[ButterflySelector] El prefab '{data.prefabButterfly.name}' no tiene ButterflyController.", this);
            return;
        }

        // Aplica los datos de especie al controlador
        _activeButterfly.Initialize(data);

        // Notifica a ButterflyInput (en la camara) para que empiece a seguir el nuevo objeto
        if (cameraInput != null)
            cameraInput.SetTarget(_activeButterfly);
        else
            Debug.LogWarning("[ButterflySelector] cameraInput no asignado. Arrastra la Main Camera al campo 'Camera Input'.", this);
    }

    // ── Toggle del menu ───────────────────────────────────────────
    public void ToggleMenu()
    {
        _isOpen = !_isOpen;
        selectionPanel.SetActive(_isOpen);
        Time.timeScale = _isOpen ? 0f : 1f;
    }

    public void CloseMenu()
    {
        _isOpen = false;
        //selectionPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // ── Acceso publico a la mariposa activa ───────────────────────
    public ButterflyController ActiveButterfly => _activeButterfly;
}