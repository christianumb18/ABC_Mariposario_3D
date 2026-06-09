using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OpenSettingsButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OpenSettings);
    }

    private void OpenSettings()
    {
        if (SettingsPanel.Instance != null)
            SettingsPanel.Instance.Open();
        else
            Debug.LogWarning("[OpenSettingsButton] No hay SettingsPanel.Instance vivo.");
    }
}