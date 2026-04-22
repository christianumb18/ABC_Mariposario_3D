using UnityEngine;

/// <summary>
/// Adjunta a un GameObject vacio "GameManager".
/// Solo coordina el menu y escucha cambios de especie.
/// La instanciacion del prefab la hace ButterflySelector.
/// </summary>
public class MariposarioGameManager : MonoBehaviour
{
    [Header("Referencias")]
    public ButterflySelector selector;      // ButterflySpawnPoint
    public ButterflyUserControl cameraInput;   // Main Camera

    [Header("HUD")]
    public GameObject hudCanvas;

    public static bool IsMenuOpen { get; private set; }

    // ═══════════════════════════════════════════════════════════════

    private void Start()
    {
        if (selector != null)
            selector.onSpeciesSelected += OnSpeciesSelected;
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Tab))
        //    ToggleMenu();
    }

    private void ToggleMenu()
    {
        IsMenuOpen = !IsMenuOpen;

        if (selector != null)
            selector.ToggleMenu();

        if (cameraInput != null)
            cameraInput.enabled = !IsMenuOpen;

        if (hudCanvas != null)
            hudCanvas.SetActive(!IsMenuOpen);
    }

    private void OnSpeciesSelected(ButterflyData data)
    {
        Debug.Log($"[GameManager] Especie activa: {data.speciesName}");
    }

    private void OnDestroy()
    {
        if (selector != null)
            selector.onSpeciesSelected -= OnSpeciesSelected;
    }
}