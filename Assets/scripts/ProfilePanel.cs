using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controla la UI del Canvas_Perfil. Lee y escribe a ProfileManager.Instance.
///
/// Flujo del nombre (3 estados):
///   - REPOSO: solo se ve el label, el input y el boton Aceptar estan ocultos.
///   - EDITANDO: al tocar el lapiz, el input aparece con el texto actual,
///     el teclado del movil se abre solo, y aparece el boton Aceptar.
///   - ACEPTADO: al tocar Aceptar, se guarda el nombre, el input se oculta
///     y el label se actualiza.
///
/// SETUP (segun la jerarquia de tu imagen):
///   - Canvas_Perfil > Panel_Perfil
///       - nombre              (TMP_Text)       ← label "User01"
///       - InputField (TMP)    (TMP_InputField) ← campo "Cambia tu nombre..."
///       - Button_Aceptar      (Button)         ← boton "Aceptar"
///       - Button_Edit         (Button)         ← icono del lapiz
///       - Button_Close        (Button)         ← X (cierra el panel)
///       - Panel_Stats
///           - progreso/numero    (TMP_Text)    ← "0%"
///           - tiempoVuelo/numero (TMP_Text)    ← "00:00:00"
///           - totalEspecies/numero (TMP_Text)  ← "0/7"
///       - Panel_Buttons
///           - Button_Borrar    (Button)        ← borra nombre
///           - Button_Resetear  (Button)        ← borra stats
///       - Panel_Info
///           - Button_RedABC    (Button)        ← abre URL
///
///   Conecta cada referencia desde el Inspector.
///   El panel se abre/cierra desde fuera (boton del HUD → ProfilePanel.Toggle()).
/// </summary>
public class ProfilePanel : MonoBehaviour

{
    // ── Singleton (alcance de escena, no persistente) ──────────────
    public static ProfilePanel Instance { get; private set; }
    // ── Panel principal ────────────────────────────────────────────
    [Header("Panel principal")]
    public GameObject panelRoot;          // Panel_Perfil
    public Button btnClose;

    // ── Bloque del nombre ──────────────────────────────────────────
    [Header("Nombre")]
    [Tooltip("Label que muestra el nombre actual.")]
    public TMP_Text nameLabel;
    [Tooltip("Campo donde el jugador escribe el nuevo nombre.")]
    public TMP_InputField nameInput;
    [Tooltip("Icono del lapiz: pasa de REPOSO a EDITANDO.")]
    public Button btnEdit;
    [Tooltip("Boton verde: confirma el nuevo nombre.")]
    public Button btnAceptar;
    [Tooltip("Maximo de caracteres permitidos en el nombre.")]
    public int maxNameLength = 16;

    // ── Bloque de stats ────────────────────────────────────────────
    [Header("Stats")]
    public TMP_Text progressText;         // "0%"
    public TMP_Text flightTimeText;       // "00:00:00"
    public TMP_Text totalSpeciesText;     // "0/7"

    // ── Botones de accion ──────────────────────────────────────────
    [Header("Acciones")]
    public Button btnBorrar;              // borra el nombre
    public Button btnResetear;            // borra stats (no nombre)
    [Tooltip("Boton para cambiar de perfil. Si esta vacio se autoarma al abrir el panel.")]
    public Button btnCambiarPerfil;
    [Tooltip("Nombre exacto de la escena del menu principal en Build Settings.")]
    public string menuSceneName = "Menu_3D";

    // ── Info / link externo ────────────────────────────────────────
    [Header("Red ABC")]
    public Button btnRedABC;
    [Tooltip("URL que se abre al tocar el boton Red ABC.")]
    public string redABCUrl = "https://www.redabc.com";

    // ── Refresco del tiempo ────────────────────────────────────────
    // El tiempo se refresca con un timer ligero solo cuando el panel
    // esta abierto. Asi no consumimos nada cuando el panel esta cerrado.
    private const float REFRESH_INTERVAL = 1f; // cada segundo basta para HH:MM:SS
    private float _refreshTimer;

    // ═══════════════════════════════════════════════════════════════
    // CICLO DE VIDA
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        WireButtons();
        ConfigureInput();

        if (panelRoot != null) panelRoot.SetActive(false);
        // El panel arranca cerrado. Se abre desde un boton del HUD que
        // llame a ProfilePanel.Toggle() / Open().
    }

    private void OnEnable()
    {
        if (ProfileManager.Instance != null)
        {
            ProfileManager.Instance.OnNameChanged += RefreshName;
            ProfileManager.Instance.OnProgressChanged += RefreshProgressAndSpecies;
        }
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDisable()
    {
        if (ProfileManager.Instance != null)
        {
            ProfileManager.Instance.OnNameChanged -= RefreshName;
            ProfileManager.Instance.OnProgressChanged -= RefreshProgressAndSpecies;
        }
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf) return;

        _refreshTimer += Time.unscaledDeltaTime;
        if (_refreshTimer >= REFRESH_INTERVAL)
        {
            _refreshTimer = 0f;
            RefreshFlightTime();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // SETUP
    // ═══════════════════════════════════════════════════════════════

    private void WireButtons()
    {
        if (btnClose != null) btnClose.onClick.AddListener(Close);
        if (btnEdit != null) btnEdit.onClick.AddListener(EnterEditMode);
        if (btnAceptar != null) btnAceptar.onClick.AddListener(ConfirmEdit);
        if (btnBorrar != null) btnBorrar.onClick.AddListener(OnBorrarPressed);
        if (btnResetear != null) btnResetear.onClick.AddListener(OnResetearPressed);
        if (btnRedABC != null) btnRedABC.onClick.AddListener(OnRedABCPressed);
        if (btnCambiarPerfil != null) btnCambiarPerfil.onClick.AddListener(OnCambiarPerfilPressed);
    }

    private void ConfigureInput()
    {
        if (nameInput == null) return;

        nameInput.characterLimit = maxNameLength;
        // Submit con Enter en pantalla = mismo efecto que tocar Aceptar.
        // En movil, el "Done" del teclado dispara onSubmit.
        nameInput.onSubmit.AddListener(_ => ConfirmEdit());
    }

    // ═══════════════════════════════════════════════════════════════
    // ABRIR / CERRAR
    // ═══════════════════════════════════════════════════════════════

    public void Open()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(true);
        EnsureCambiarPerfilButton();
        RefreshAll();
        SetEditingState(false);   // siempre abrimos en REPOSO
    }

    public void Close()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (panelRoot == null) return;
        if (panelRoot.activeSelf) Close();
        else Open();
    }

    // ═══════════════════════════════════════════════════════════════
    // EDICION DE NOMBRE — los 3 estados
    // ═══════════════════════════════════════════════════════════════

    // REPOSO → EDITANDO
    private void EnterEditMode()
    {
        if (nameInput == null) return;

        // Pre-carga el texto actual en el input (esto es lo que pediste:
        // "traer el texto del label al input cuando se clickea").
        nameInput.text = ProfileManager.Instance != null
            ? ProfileManager.Instance.UserName
            : (nameLabel != null ? nameLabel.text : "");

        SetEditingState(true);

        // Foco + abre el teclado virtual en movil
        nameInput.Select();
        nameInput.ActivateInputField();
    }

    // EDITANDO → ACEPTADO
    private void ConfirmEdit()
    {
        if (nameInput == null) return;

        string newName = nameInput.text != null ? nameInput.text.Trim() : "";

        if (ProfileManager.Instance != null)
            ProfileManager.Instance.SetUserName(newName);

        // Refresco inmediato sin depender del evento (a prueba de bugs)
        if (nameLabel != null) nameLabel.text = newName;

        SetEditingState(false);
    }

    // Aplica el estado: bloquea o desbloquea input y boton Aceptar.
    // El input y el boton quedan SIEMPRE visibles; solo cambia si son
    // interactuables. Asi el usuario ve la UI completa todo el tiempo.
    private void SetEditingState(bool editing)
    {
        if (nameInput != null)
        {
            // interactable=false hace que el input no responda a toques
            // y se vea "grisado" (segun el ColorBlock del componente).
            nameInput.interactable = editing;
            if (!editing)
            {
                nameInput.DeactivateInputField();
                nameInput.text = "";   // limpia el campo en modo reposo
            }
        }
        if (btnAceptar != null) btnAceptar.interactable = editing;
    }

    // ═══════════════════════════════════════════════════════════════
    // BOTONES DE ACCION
    // ═══════════════════════════════════════════════════════════════

    private void OnBorrarPressed()
    {
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.ClearUserName();

        // Refresco inmediato sin depender del evento (a prueba de bugs)
        if (nameLabel != null) nameLabel.text = "";
    }

    private void OnResetearPressed()
    {
        // "Resetear" → tiempo de vuelo y progreso a 0, nombre intacto.
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.ResetStats();
        RefreshAll();
    }

    private void OnRedABCPressed()
    {
        if (string.IsNullOrEmpty(redABCUrl))
        {
            Debug.LogWarning("[ProfilePanel] redABCUrl esta vacio en el Inspector.");
            return;
        }
        Application.OpenURL(redABCUrl);
    }

    // ── Cambiar perfil ─────────────────────────────────────────────
    // En la escena del menu, ProfileSelectionUI.Instance existe y la
    // abrimos directo. En cualquier otra escena (mariposario, ciclo),
    // cargamos Menu_3D — alli ProfileSelectionUI se muestra al iniciar.
    private void OnCambiarPerfilPressed()
    {
        Close();

        if (ProfileSelectionUI.Instance != null)
        {
            ProfileSelectionUI.Instance.Open();
            return;
        }

        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogWarning("[ProfilePanel] menuSceneName esta vacio.");
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    // Si el Inspector no tiene asignado el boton "Cambiar perfil", lo
    // creamos al vuelo. Si btnResetear esta asignado, lo clonamos para
    // que herede tamano, color y estilo, y lo colocamos justo debajo.
    private void EnsureCambiarPerfilButton()
    {
        if (btnCambiarPerfil != null) return;
        if (panelRoot == null) return;

        // Caso A: tenemos btnResetear como referencia visual → clonamos
        if (btnResetear != null)
        {
            var clone = Instantiate(btnResetear.gameObject, btnResetear.transform.parent);
            clone.name = "Btn_CambiarPerfil";

            // Limpiar listeners heredados
            var btnClone = clone.GetComponent<Button>();
            btnClone.onClick.RemoveAllListeners();
            btnClone.onClick.AddListener(OnCambiarPerfilPressed);

            // Posicion: centrado igual que Resetear, justo debajo con gap.
            // Ancho 1.8x el original para que "Cambiar perfil" entre bien.
            var srcRT = (RectTransform)btnResetear.transform;
            var rt = (RectTransform)clone.transform;
            rt.anchorMin = srcRT.anchorMin;
            rt.anchorMax = srcRT.anchorMax;
            rt.pivot = srcRT.pivot;
            rt.sizeDelta = new Vector2(srcRT.sizeDelta.x * 1.8f, srcRT.sizeDelta.y);
            rt.anchoredPosition = srcRT.anchoredPosition
                + new Vector2(0f, -(srcRT.sizeDelta.y + 10f));

            // Color azul (sobreescribe el amarillo heredado)
            var img = clone.GetComponent<Image>();
            if (img != null) img.color = new Color(0.20f, 0.45f, 0.80f, 1f);

            // Texto: cambiarlo y ponerlo en blanco para contraste
            var tmp = clone.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = "Cambiar perfil";
                tmp.color = Color.white;
            }

            btnCambiarPerfil = btnClone;
            return;
        }

        // Caso B: fallback — boton azul abajo del panel
        var go = new GameObject("Btn_CambiarPerfil",
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(panelRoot.transform, false);

        var rt2 = (RectTransform)go.transform;
        rt2.anchorMin = new Vector2(0.5f, 0f);
        rt2.anchorMax = new Vector2(0.5f, 0f);
        rt2.pivot = new Vector2(0.5f, 0f);
        rt2.anchoredPosition = new Vector2(0f, 30f);
        rt2.sizeDelta = new Vector2(224f, 48f);

        go.GetComponent<Image>().color = new Color(0.20f, 0.45f, 0.80f, 1f);
        btnCambiarPerfil = go.GetComponent<Button>();
        btnCambiarPerfil.onClick.AddListener(OnCambiarPerfilPressed);

        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(go.transform, false);
        var lblRT = (RectTransform)lblGO.transform;
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;
        var tmpf = lblGO.AddComponent<TextMeshProUGUI>();
        tmpf.text = "Cambiar perfil";
        tmpf.alignment = TextAlignmentOptions.Center;
        tmpf.fontSize = 19;
        tmpf.fontStyle = FontStyles.Bold;
        tmpf.color = Color.white;
    }

    // ═══════════════════════════════════════════════════════════════
    // REFRESCO DE UI
    // ═══════════════════════════════════════════════════════════════

    private void RefreshAll()
    {
        RefreshName();
        RefreshProgressAndSpecies();
        RefreshFlightTime();
    }

    private void RefreshName()
    {
        if (nameLabel == null) return;
        nameLabel.text = ProfileManager.Instance != null
            ? ProfileManager.Instance.UserName
            : "";
    }

    private void RefreshFlightTime()
    {
        if (flightTimeText == null || ProfileManager.Instance == null) return;
        flightTimeText.text = ProfileManager.Instance.FlightTimeFormatted;
    }

    private void RefreshProgressAndSpecies()
    {
        if (ProfileManager.Instance == null) return;

        if (progressText != null)
        {
            // Sin decimales: 28% queda mas limpio que 28.5714%.
            progressText.text = $"{Mathf.RoundToInt(ProfileManager.Instance.ProgressPercent)}%";
        }

        if (totalSpeciesText != null)
        {
            totalSpeciesText.text =
                $"{ProfileManager.Instance.Progress}/{ProfileManager.Instance.TotalSpecies}";
        }
    }
}
