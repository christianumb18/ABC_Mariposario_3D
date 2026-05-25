using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla los botones del menu principal 3D (escena Menu_3D).
///
/// SETUP:
///   1. Adjunta este script a un GameObject de la escena Menu_3D
///      (por ejemplo al Canvas o a un GameObject vacio "MenuManager").
///   2. En el boton "Iniciar" -> OnClick -> arrastra ese GameObject
///      y selecciona MainMenu.PlayGame().
///   3. En el boton "Salir" -> OnClick -> selecciona MainMenu.QuitGame().
///   4. Verifica que las escenas esten en Build Profiles:
///        indice 0: Menu_3D
///        indice 1: MapaMariposario
///        (opcional) SeleccionMariposa
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Nombres de escenas (Build Profiles)")]
    [Tooltip("Nombre exacto de la escena del mariposario en Build Profiles.")]
    [SerializeField] private string mariposarioSceneName = "MapaMariposario_conPlantas";

    [Tooltip("MapaMariposario_conPlantas")]
    [SerializeField] private string selectionSceneName = "";

    [Tooltip("Escena de la biblioteca/seleccion de mariposa.")]
    [SerializeField] private string bibliotecaSceneName = "SeleccionMariposa";

    // ── Boton INICIAR ──────────────────────────────────────────────
    public void PlayGame()
    {
        // Si hay escena de seleccion configurada, va alli primero
        if (!string.IsNullOrEmpty(selectionSceneName))
        {
            SceneManager.LoadScene(selectionSceneName);
            return;
        }

        // Si no, va directo al mariposario
        SceneManager.LoadScene(mariposarioSceneName);
    }

    // ── Boton SALIR ────────────────────────────────────────────────
    public void QuitGame()
    {
        Debug.Log("[MainMenu] Saliendo del juego...");

#if UNITY_EDITOR
        // En el editor detiene el Play Mode
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Botones extra (los puedes conectar despues) ────────────────
    public void OpenBiblioteca()
    {
        if (string.IsNullOrEmpty(bibliotecaSceneName))
        {
            Debug.LogWarning("[MainMenu] bibliotecaSceneName esta vacio.");
            return;
        }
        SceneManager.LoadScene(bibliotecaSceneName);
    }

    public void OpenConfiguracion()
    {
        if (SettingsPanel.Instance != null)
        {
            SettingsPanel.Instance.Open();
        }
        else
        {
            Debug.LogWarning("[MainMenu] SettingsPanel.Instance no existe en la escena. " +
                            "Asegurate de que el Canvas_Settings este en la escena Menu_3D " +
                            "y tenga el script SettingsPanel adjunto.");
        }
    }

    public void OpenPerfil()
    {
        if (ProfilePanel.Instance != null)
        {
            ProfilePanel.Instance.Open();
        }
        else
        {
            Debug.LogWarning("[MainMenu] ProfilePanel.Instance");
        }
    }

    public void OpenLogros()
    {
        Debug.Log("[MainMenu] Logros aun no implementados.");
    }

    public void OpenMensaje()
    {
        Debug.Log("");
    }
}
