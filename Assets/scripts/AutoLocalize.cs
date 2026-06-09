using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Escanea automaticamente todos los TMP_Text de cada escena al cargarla
/// y les pega un LocalizedText con la clave que coincida en el diccionario.
/// Asi NO hay que agregar el componente manualmente a cada label.
///
/// Solo traduce textos que coincidan EXACTO con el diccionario. Si no
/// coinciden (textos dinamicos como "FPS: 60", "C $ 0.000", nombres de
/// jugador, etc.) los deja intactos.
///
/// Re-escanea en varios momentos:
///   - Al cargar una escena (sceneLoaded)
///   - Algunos frames despues (para capturar UI creada por Start de otros scripts)
///   - Al cambiar el idioma (por si hay UI nueva)
/// </summary>
public class AutoLocalize : MonoBehaviour
{
    public static AutoLocalize Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[AutoLocalize]");
        go.AddComponent<AutoLocalize>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += Scan;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= Scan;
    }

    private void Start()
    {
        Scan();
        StartCoroutine(DelayedRescan());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Scan();
        StartCoroutine(DelayedRescan());
    }

    // Re-escanea unos frames despues. Captura UI creada por Start() de otros scripts.
    private IEnumerator DelayedRescan()
    {
        yield return null;       // 1 frame
        Scan();
        yield return new WaitForSecondsRealtime(0.5f);
        Scan();
    }

    public void Scan()
    {
        if (LocalizationManager.Instance == null) return;

        int attached = 0;
        int active = SceneManager.sceneCount;
        for (int s = 0; s < active; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.IsValid() || !scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                attached += ProcessTree(root.transform);
            }
        }

        // DontDestroyOnLoad scene
        if (Instance != null)
            attached += ProcessTree(Instance.transform.root);

        if (attached > 0)
            Debug.Log($"[AutoLocalize] Localizadas {attached} etiquetas en escena '{SceneManager.GetActiveScene().name}'.");
    }

    private int ProcessTree(Transform root)
    {
        var labels = root.GetComponentsInChildren<TMP_Text>(includeInactive: true);
        int attached = 0;
        foreach (var label in labels)
        {
            if (label == null) continue;
            if (label.GetComponent<LocalizedText>() != null) continue;

            string current = label.text;
            string key = LocalizationManager.Instance.FindKey(current);
            if (key == null) continue;

            var loc = label.gameObject.AddComponent<LocalizedText>();
            loc.key = key;
            loc.Refresh();
            attached++;
        }
        return attached;
    }
}
