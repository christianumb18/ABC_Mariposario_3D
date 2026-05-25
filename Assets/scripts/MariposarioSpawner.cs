using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lee la especie guardada en ButterflyGameManager e instancia el prefab
/// correcto al cargar la escena del mariposario.
///
/// SETUP en la escena Mariposario:
///   - Adjunta este script al ButterflySpawnPoint (reemplaza a ButterflySelector).
///   - Arrastra la Main Camera al campo cameraInput.
///   - Ya NO necesitas la lista de especies aqui: vienen del GameManager.
/// </summary>
public class MariposarioSpawner : MonoBehaviour
{
    [Header("Referencia a la camara")]
    public ButterflyUserControl cameraInput;

    [Header("Referencia al minimapa")]
    public MinimapController minimapController;

    [Header("UI")]
    public Button layEggButton;         // Boton que se habilita al acercarse
    public TextMeshProUGUI textPlantCorrect;   // Texto que indica si la planta es correcta
    public TextMeshProUGUI textPlantIncorrect; // Texto que indica si la planta es incorrecta

    // ── Instancia activa ───────────────────────────────────────────
    private ButterflyController _activeButterfly;
    public ButterflyController ActiveButterfly => _activeButterfly;
    private ButterflyAnimator _activeAnimator;

    // ═══════════════════════════════════════════════════════════════

    private void Start()
    {
        // Verifica que el singleton exista y tenga una especie guardada
        if (MariposarioGameManager.Instance == null)
        {
            Debug.LogError("[MariposarioSpawner] ButterflyGameManager no encontrado. " +
                           "Asegurate de que exista en la escena de seleccion.");
            return;
        }

        ButterflyData data = MariposarioGameManager.Instance.SelectedSpecie;

        if (data == null)
        {
            Debug.LogError("[MariposarioSpawner] No hay especie seleccionada en el GameManager.");
            return;
        }

        SpawnButterfly(data);

        if (ProfileManager.Instance != null)
            ProfileManager.Instance.StartCounting();

    }

    // ── Instancia el prefab ───────────────────────────────────────
    private void SpawnButterfly(ButterflyData data)
    {
        if (data.prefabButterfly == null)
        {
            Debug.LogError($"[MariposarioSpawner] '{data.speciesName}' no tiene prefab asignado.");
            return;
        }

        // Destruye instancia previa si existiera
        if (_activeButterfly != null)
            Destroy(_activeButterfly.gameObject);

        GameObject go = Instantiate(data.prefabButterfly, transform.position, transform.rotation);
        _activeButterfly = go.GetComponent<ButterflyController>();

        if (_activeButterfly == null)
        {
            Debug.LogError($"[MariposarioSpawner] El prefab '{data.prefabButterfly.name}' no tiene ButterflyController.");
            return;
        }

        _activeButterfly.Initialize(data);

        // ── ButterflyAnimator ──────────────────────────────────────
        _activeAnimator = go.GetComponent<ButterflyAnimator>();
        if (_activeAnimator != null)
        {
            // Activa la animacion de vuelo apenas aparece la mariposa
            _activeAnimator.PlayAnimation(ButterflyAnimator.ButterflyAnimation.Flying);
        }
        else
        {
            Debug.LogWarning($"[MariposarioSpawner] '{data.prefabButterfly.name}' no tiene ButterflyAnimator. " +
                              "Agrega el script al prefab si quieres controlar animaciones.");
        }
        
        // Notifica a la camara
        if (cameraInput != null)
            cameraInput.SetTarget(_activeButterfly);
        else
            Debug.LogWarning("[MariposarioSpawner] cameraInput no asignado.");
            
        // Notifica al minimapa
        if (minimapController != null)
            minimapController.SetTarget(_activeButterfly.transform);
        else
            Debug.LogWarning("[MariposarioSpawner] minimapController no asignado.");
    }
}