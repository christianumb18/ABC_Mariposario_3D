using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona el panel de selección de especie de mariposa.
///
/// SETUP:
///  1. Crea un Canvas con un Panel (selectionPanel).
///  2. Dentro del panel, un contenedor con Layout Group (buttonContainer).
///  3. Crea un Button prefab con Image + TMP_Text → buttonPrefab.
///  4. Añade este script a un GameObject "ButterflySelector".
///  5. Arrastra los ButterflyData assets a species[].
///  6. El GameManager se suscribe a onSpeciesSelected automáticamente.
/// </summary>
public class ButterflySelector : MonoBehaviour
{
    [Header("Especies disponibles")]
    public List<ButterflyData> species = new();

    [Header("UI")]
    public GameObject selectionPanel;
    public Transform buttonContainer;
    public GameObject buttonPrefab;
    public TMP_Text descriptionText;
    public Image previewIcon;

    /// <summary>Se dispara cuando el jugador confirma una especie.</summary>
    public event Action<ButterflyData> onSpecieSelected;

    // ── Estado ────────────────────────────────────────────────────
    private bool _isOpen;

    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildButtons();
        selectionPanel.SetActive(false);
    }

    // ── Construye los botones dinámicamente ───────────────────────

    private void BuildButtons()
    {
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        foreach (var bd in species)
        {
            GameObject btn = Instantiate(buttonPrefab, buttonContainer);

            var img = btn.GetComponentInChildren<Image>();
            if (img != null && bd.icon != null) img.sprite = bd.icon;

            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = bd.speciesName;

            ButterflyData captured = bd;
            btn.GetComponent<Button>().onClick.AddListener(() => SelectSpecies(captured));
        }
    }

    // ── Selección ─────────────────────────────────────────────────

    public void SelectSpecies(ButterflyData selected)
    {
        if (selected == null) return;

        if (descriptionText != null)
            descriptionText.text = $"<b>{selected.speciesName}</b>\n{selected.description}";

        if (previewIcon != null && selected.icon != null)
            previewIcon.sprite = selected.icon;

        // Notifica al GameManager
        onSpecieSelected?.Invoke(selected);

        CloseMenu();
    }

    // ── Toggle del menú ───────────────────────────────────────────

    public void ToggleMenu()
    {
        _isOpen = !_isOpen;
        selectionPanel.SetActive(_isOpen);
    }

    public void CloseMenu()
    {
        _isOpen = false;
        selectionPanel.SetActive(false);
    }
}