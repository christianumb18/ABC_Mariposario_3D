using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton que persiste entre escenas.
/// 
/// /// ESCENA DE SELECCION:
///   - ButterflySelectionUI llama a ButterflyGameManager.Instance.SelectSpecies(data)
///   - Luego llama a ButterflyGameManager.Instance.LoadMariposario()
///   /// ESCENA DEL MARIPOSARIO:
///   - MariposarioSpawner lee ButterflyGameManager.Instance.SelectedSpecies
///     e instancia el prefab correcto.
/// </summary>
public class MariposarioGameManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────
    public static MariposarioGameManager Instance { get; private set; }

    // ── Nombre de las escenas (ajusta segun tu proyecto) ──────────
    private string selectionSceneName = "SeleccionMariposa";
    private string mariposarioSceneName = "MapaMariposario_conPlantas";

    // ── Dato persistente ───────────────────────────────────────────
    /// <summary>Especie que el jugador eligio. Disponible en cualquier escena.</summary>
    public ButterflyData SelectedSpecie { get; private set; }

    [Header("Referencias")]
    public ButterflyUserControl cameraInput;   // Main Camera

    public static bool IsMenuOpen { get; private set; }

    public ButterflyData fallbackSpecies;

    // ═══════════════════════════════════════════════════════════════

    private void Start()
    {
        // Solo para testing: carga la primera especie si no hay ninguna seleccionada
        if (SelectedSpecie == null && fallbackSpecies != null)
            SelectedSpecie = fallbackSpecies;
    }

    private void Awake()
    {
        // Patron singleton: si ya existe una instancia, destruye el duplicado
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);  // <-- sobrevive al cambio de escena
    }

    /// <summary>
    /// Guarda la especie elegida. Llamar desde ButterflySelectionUI.
    /// </summary>
    public void SelectSpecies(ButterflyData data)
    {
        SelectedSpecie = data;
        Debug.Log($"[ButterflyGameManager] Especie guardada: {data.speciesName}");
    }

    /// <summary>Carga la escena del mariposario.</summary>
    public void LoadMariposario()
    {
        if (SelectedSpecie == null)
        {
            Debug.LogWarning("[ButterflyGameManager] No hay especie seleccionada.");
            return;
        }
        SceneManager.LoadScene(mariposarioSceneName);
    }

    /// <summary>Vuelve a la pantalla de seleccion.</summary>
    public void LoadSelectionScene()
    {
        SceneManager.LoadScene(selectionSceneName);
    }
}