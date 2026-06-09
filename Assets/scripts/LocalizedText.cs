using TMPro;
using UnityEngine;

/// <summary>
/// Pegar este componente a un TMP_Text. Asignar la clave en el Inspector.
/// El texto se actualiza automaticamente cuando cambia el idioma.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
[DisallowMultipleComponent]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Clave del diccionario de LocalizationManager. Ej: ui.start")]
    public string key;

    private TMP_Text _label;

    private void Awake()
    {
        _label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (_label == null) _label = GetComponent<TMP_Text>();
        if (LocalizationManager.Instance == null || string.IsNullOrEmpty(key)) return;
        _label.text = LocalizationManager.Instance.Get(key);
    }
}
