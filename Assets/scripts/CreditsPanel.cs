using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Panel de creditos y aviso de copyright para "Mariposario Virtual Interactivo".
///
/// Se autoconstruye al iniciar el juego (RuntimeInitializeOnLoadMethod).
/// No requiere arrastrar nada en el Inspector: crea Canvas, panel, scroll,
/// texto, boton flotante "©" en la esquina y boton cerrar automaticamente.
///
/// Singleton + DontDestroyOnLoad para que sea accesible desde cualquier
/// escena. En escenas distintas a Menu_3D solo aparece cuando se abre.
/// </summary>
public class CreditsPanel : MonoBehaviour
{
    public static CreditsPanel Instance { get; private set; }

    private GameObject panelRoot;
    private GameObject floatingButton;
    private TMP_Text textCreditos;

    // Texto oficial - editar requiere autorizacion de Red A.B.C.
    private const string COPYRIGHT_TEXT =
"<size=120%><b>COPYRIGHT © 2025–2026 RED A.B.C.</b></size>\n" +
"<size=90%>TODOS LOS DERECHOS RESERVADOS.</size>\n" +
"\n" +
"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
"\n" +
"<b>DERECHOS PATRIMONIALES</b>\n" +
"Titular: Red A.B.C (Organización No Gubernamental Ambiental)\n" +
"Representante Legal: Ing. Sonia Dame\n" +
"País de registro: República de Colombia\n" +
"\n" +
"La presente obra —aplicación móvil educativa \"Mariposario Virtual Interactivo\", " +
"desarrollada en Unity 3D para plataforma Android— es propiedad patrimonial " +
"exclusiva de Red A.B.C., conforme a lo dispuesto en la Ley 23 de 1982 y la " +
"Ley 1915 de 2018 de la República de Colombia.\n" +
"Queda prohibida su reproducción, distribución, modificación o explotación " +
"comercial sin autorización escrita del titular.\n" +
"\n" +
"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
"\n" +
"<b>DERECHOS MORALES</b>\n" +
"<size=85%>(Inalienables e irrenunciables — Art. 30, Ley 23/1982)</size>\n" +
"Autores: Equipo de Ingeniería — Universidad Manuela Beltrán (UMB)\n" +
"\n" +
"  1. Juan Nicolás Santos        —  Diseño UX/UI\n" +
"  2. Daniel Barragán            —  Diseño y Animación\n" +
"  3. Víctor Guillén             —  Líder Técnico y Programador\n" +
"  4. Cristian Garzón            —  Programador\n" +
"  5. Sebastián Restrepo Franco  —  QA y Programador\n" +
"\n" +
"Los autores conservan de forma irrenunciable e inalienable:\n" +
"  • El derecho de paternidad sobre la obra\n" +
"  • El derecho a la integridad de la obra\n" +
"  • El derecho de divulgación\n" +
"\n" +
"Obra desarrollada en modalidad de encargo institucional para Red A.B.C., " +
"en el marco de la Práctica Empresarial I — Universidad Manuela Beltrán, " +
"Bogotá D.C., Colombia. 2025–2026.\n" +
"\n" +
"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";

    // ───────────────────────────────────────────────────────────────
    // BOOTSTRAP - se crea automaticamente al cargar la primera escena
    // ───────────────────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("CreditsPanel_Auto");
        go.AddComponent<CreditsPanel>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateFloatingButtonVisibility(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (panelRoot != null && panelRoot.activeSelf)
            panelRoot.SetActive(false);
        Time.timeScale = 1f;
        EnsureEventSystem();
        UpdateFloatingButtonVisibility(scene);
    }

    // Solo mostrar el boton "©" flotante en Menu_3D
    private void UpdateFloatingButtonVisibility(Scene scene)
    {
        if (floatingButton == null) return;
        floatingButton.SetActive(scene.name == "Menu_3D");
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCCION DE UI EN RUNTIME
    // ═══════════════════════════════════════════════════════════════
    private void BuildUI()
    {
        EnsureEventSystem();

        // ── Canvas raiz ────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas_Creditos",
            typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // siempre encima
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // ── Boton flotante "©" en esquina inferior izquierda ──────
        floatingButton = new GameObject("Btn_Creditos_Flotante",
            typeof(RectTransform), typeof(Image), typeof(Button));
        floatingButton.transform.SetParent(canvasGO.transform, false);
        var fbRT = (RectTransform)floatingButton.transform;
        fbRT.anchorMin = new Vector2(0f, 0f);
        fbRT.anchorMax = new Vector2(0f, 0f);
        fbRT.pivot = new Vector2(0f, 0f);
        fbRT.anchoredPosition = new Vector2(30f, 30f);
        fbRT.sizeDelta = new Vector2(80f, 80f);
        var fbImg = floatingButton.GetComponent<Image>();
        fbImg.color = new Color(0f, 0f, 0f, 0.55f);
        var fbBtn = floatingButton.GetComponent<Button>();
        fbBtn.onClick.AddListener(Open);

        var fbLabel = new GameObject("©", typeof(RectTransform));
        fbLabel.transform.SetParent(floatingButton.transform, false);
        var fbLblRT = (RectTransform)fbLabel.transform;
        fbLblRT.anchorMin = Vector2.zero; fbLblRT.anchorMax = Vector2.one;
        fbLblRT.offsetMin = Vector2.zero; fbLblRT.offsetMax = Vector2.zero;
        var fbTxt = fbLabel.AddComponent<TextMeshProUGUI>();
        fbTxt.text = "©";
        fbTxt.alignment = TextAlignmentOptions.Center;
        fbTxt.fontSize = 44;
        fbTxt.color = Color.white;

        // ── Panel modal (fullscreen oscurecido) ────────────────────
        panelRoot = new GameObject("Panel_Creditos",
            typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(canvasGO.transform, false);
        var pRT = (RectTransform)panelRoot.transform;
        pRT.anchorMin = Vector2.zero; pRT.anchorMax = Vector2.one;
        pRT.offsetMin = Vector2.zero; pRT.offsetMax = Vector2.zero;
        var pImg = panelRoot.GetComponent<Image>();
        pImg.color = new Color(0f, 0f, 0f, 0.85f);
        pImg.raycastTarget = true;

        // ── Caja central blanca ────────────────────────────────────
        var box = new GameObject("Caja", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(panelRoot.transform, false);
        var boxRT = (RectTransform)box.transform;
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(1200f, 900f);
        boxRT.anchoredPosition = Vector2.zero;
        box.GetComponent<Image>().color = new Color(0.97f, 0.97f, 0.94f, 1f);

        // ── Titulo ─────────────────────────────────────────────────
        var title = new GameObject("Titulo", typeof(RectTransform));
        title.transform.SetParent(box.transform, false);
        var tRT = (RectTransform)title.transform;
        tRT.anchorMin = new Vector2(0f, 1f); tRT.anchorMax = new Vector2(1f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = new Vector2(0f, -20f);
        tRT.sizeDelta = new Vector2(-40f, 60f);
        var tTxt = title.AddComponent<TextMeshProUGUI>();
        tTxt.text = "CRÉDITOS Y COPYRIGHT";
        tTxt.alignment = TextAlignmentOptions.Center;
        tTxt.fontSize = 36;
        tTxt.fontStyle = FontStyles.Bold;
        tTxt.color = new Color(0.1f, 0.2f, 0.1f, 1f);

        // ── Boton cerrar X ─────────────────────────────────────────
        var closeBtnGO = new GameObject("Btn_Cerrar",
            typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGO.transform.SetParent(box.transform, false);
        var cbRT = (RectTransform)closeBtnGO.transform;
        cbRT.anchorMin = new Vector2(1f, 1f);
        cbRT.anchorMax = new Vector2(1f, 1f);
        cbRT.pivot = new Vector2(1f, 1f);
        cbRT.anchoredPosition = new Vector2(-15f, -15f);
        cbRT.sizeDelta = new Vector2(60f, 60f);
        closeBtnGO.GetComponent<Image>().color = new Color(0.7f, 0.2f, 0.2f, 1f);
        closeBtnGO.GetComponent<Button>().onClick.AddListener(Close);

        var xLabel = new GameObject("X", typeof(RectTransform));
        xLabel.transform.SetParent(closeBtnGO.transform, false);
        var xRT = (RectTransform)xLabel.transform;
        xRT.anchorMin = Vector2.zero; xRT.anchorMax = Vector2.one;
        xRT.offsetMin = Vector2.zero; xRT.offsetMax = Vector2.zero;
        var xTxt = xLabel.AddComponent<TextMeshProUGUI>();
        xTxt.text = "X";
        xTxt.alignment = TextAlignmentOptions.Center;
        xTxt.fontSize = 32;
        xTxt.fontStyle = FontStyles.Bold;
        xTxt.color = Color.white;

        // ── ScrollView ─────────────────────────────────────────────
        var scrollGO = new GameObject("Scroll",
            typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(box.transform, false);
        var scRT = (RectTransform)scrollGO.transform;
        scRT.anchorMin = new Vector2(0f, 0f); scRT.anchorMax = new Vector2(1f, 1f);
        scRT.offsetMin = new Vector2(30f, 100f);
        scRT.offsetMax = new Vector2(-30f, -100f);
        scrollGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.6f);
        var scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 30f;

        // Mask
        var maskGO = new GameObject("Viewport",
            typeof(RectTransform), typeof(Image), typeof(Mask));
        maskGO.transform.SetParent(scrollGO.transform, false);
        var mRT = (RectTransform)maskGO.transform;
        mRT.anchorMin = Vector2.zero; mRT.anchorMax = Vector2.one;
        mRT.offsetMin = Vector2.zero; mRT.offsetMax = Vector2.zero;
        maskGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        maskGO.GetComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = (RectTransform)maskGO.transform;

        // Content
        var contentGO = new GameObject("Content", typeof(RectTransform),
            typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
        contentGO.transform.SetParent(maskGO.transform, false);
        var coRT = (RectTransform)contentGO.transform;
        coRT.anchorMin = new Vector2(0f, 1f);
        coRT.anchorMax = new Vector2(1f, 1f);
        coRT.pivot = new Vector2(0.5f, 1f);
        coRT.anchoredPosition = Vector2.zero;
        coRT.sizeDelta = new Vector2(0f, 0f);
        var fitter = contentGO.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        scrollRect.content = (RectTransform)contentGO.transform;

        // Texto
        var textGO = new GameObject("Texto", typeof(RectTransform));
        textGO.transform.SetParent(contentGO.transform, false);
        textCreditos = textGO.AddComponent<TextMeshProUGUI>();
        textCreditos.text = COPYRIGHT_TEXT;
        textCreditos.fontSize = 22;
        textCreditos.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        textCreditos.alignment = TextAlignmentOptions.TopLeft;
        textCreditos.enableWordWrapping = true;
        textCreditos.richText = true;

        // Scrollbar vertical
        var sbGO = new GameObject("Scrollbar",
            typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGO.transform.SetParent(scrollGO.transform, false);
        var sbRT = (RectTransform)sbGO.transform;
        sbRT.anchorMin = new Vector2(1f, 0f); sbRT.anchorMax = new Vector2(1f, 1f);
        sbRT.pivot = new Vector2(1f, 0.5f);
        sbRT.anchoredPosition = new Vector2(0f, 0f);
        sbRT.sizeDelta = new Vector2(15f, -10f);
        sbGO.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        var sb = sbGO.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        var sbHandleArea = new GameObject("Sliding Area", typeof(RectTransform));
        sbHandleArea.transform.SetParent(sbGO.transform, false);
        var sbhaRT = (RectTransform)sbHandleArea.transform;
        sbhaRT.anchorMin = Vector2.zero; sbhaRT.anchorMax = Vector2.one;
        sbhaRT.offsetMin = new Vector2(2f, 2f);
        sbhaRT.offsetMax = new Vector2(-2f, -2f);

        var sbHandle = new GameObject("Handle",
            typeof(RectTransform), typeof(Image));
        sbHandle.transform.SetParent(sbHandleArea.transform, false);
        var sbhRT = (RectTransform)sbHandle.transform;
        sbhRT.anchorMin = Vector2.zero; sbhRT.anchorMax = Vector2.one;
        sbhRT.offsetMin = Vector2.zero; sbhRT.offsetMax = Vector2.zero;
        sbHandle.GetComponent<Image>().color = new Color(0.3f, 0.5f, 0.3f, 1f);
        sb.targetGraphic = sbHandle.GetComponent<Image>();
        sb.handleRect = (RectTransform)sbHandle.transform;
        scrollRect.verticalScrollbar = sb;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // Posicion inicial: panel oculto
        panelRoot.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════
    // API publica
    // ═══════════════════════════════════════════════════════════════
    public void Open()
    {
        if (panelRoot == null) return;
        if (textCreditos != null) textCreditos.text = COPYRIGHT_TEXT;
        panelRoot.SetActive(true);
        if (SceneManager.GetActiveScene().name != "Menu_3D")
            Time.timeScale = 0f;
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Toggle()
    {
        if (panelRoot == null) return;
        if (panelRoot.activeSelf) Close();
        else Open();
    }
}
