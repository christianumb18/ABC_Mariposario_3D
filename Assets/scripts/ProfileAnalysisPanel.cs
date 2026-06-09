using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de análisis del perfil: muestra fortalezas y fallas con frases tipo
///   "Tu especie más fuerte: Morpho (450 pts)"
///   "Tu mayor reto: Caligo (12 muertes)"
/// y una tabla por especie con score / vidas / muertes / video.
///
/// Botón flotante en la esquina superior derecha (📊) que abre el panel.
///
/// SETUP:
///   1. Adjunta este script a un GameObject persistente (puede ser InteractionManager).
///   2. (Opcional) showFloatingButton = false si prefieres llamar Open() desde
///      otro botón.
/// </summary>
public class ProfileAnalysisPanel : MonoBehaviour
{
    public static ProfileAnalysisPanel Instance { get; private set; }

    [Header("Comportamiento")]
    public bool showFloatingButton = true;

    [Tooltip("Posición del botón ★ flotante. Anchor = top-right. " +
             "X negativo = más a la izquierda. Y negativo = más abajo.")]
    public Vector2 floatingButtonPosition = new Vector2(-20f, -150f);

    [Tooltip("Tamaño del botón ★ flotante.")]
    public Vector2 floatingButtonSize = new Vector2(72f, 72f);

    [Header("Estilo")]
    public Color overlayColor   = new Color(0f, 0f, 0f, 0.78f);
    public Color panelColor     = new Color(0.08f, 0.10f, 0.16f, 0.98f);
    public Color goodColor      = new Color(0.30f, 0.85f, 0.45f, 1f);
    public Color badColor       = new Color(1f,    0.30f, 0.30f, 1f);
    public Color buttonColor    = new Color(0.20f, 0.45f, 0.80f, 1f);
    public Color buttonAltColor = new Color(0.35f, 0.35f, 0.40f, 1f);

    // ── UI runtime ────────────────────────────────────────────────────
    private Canvas        _canvas;
    private GameObject    _root;
    private GameObject    _floatingButton;
    private TMP_Text      _strengthText;
    private TMP_Text      _weaknessText;
    private TMP_Text      _summaryText;
    private RectTransform _rowsContainer;

    // ═════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Instance = this; }
        else Instance = this;
    }

    private void Start()
    {
        BuildUI();
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        if (_root != null && _root.activeSelf) Refresh();
    }

    public void Open()
    {
        if (_root == null) return;
        _root.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (_root != null) _root.SetActive(false);
    }

    // ── Análisis ──────────────────────────────────────────────────────

    private static string L(string key)
    {
        return LocalizationManager.Instance != null
            ? LocalizationManager.Instance.Get(key)
            : key;
    }

    private void Refresh()
    {
        if (ProfileManager.Instance == null || ProfileManager.Instance.Active == null)
        {
            // Si no hay perfil activo, mostrar mensaje claro en vez de vacío
            if (_summaryText  != null) _summaryText.text  = "<i>Sin perfil activo.</i>";
            if (_strengthText != null) _strengthText.text = "";
            if (_weaknessText != null) _weaknessText.text = "";
            return;
        }
        var profile = ProfileManager.Instance.Active;

        // Resumen general
        _summaryText.text =
            $"<b>{profile.userName}</b>\n" +
            $"{L("profile.total_score")}: <b>{profile.totalScore}</b>\n" +
            $"{L("profile.flight_time")}: <b>{ProfileManager.Instance.FlightTimeFormatted}</b>\n" +
            $"{L("profile.videos_seen")}: <b>{profile.CountSpeciesWithVideoSeen()}/{ProfileManager.Instance.TotalSpecies}</b>";

        // Fortaleza
        var strong = ProfileManager.Instance.GetStrongest();
        string strengthHeader = $"<color=#56DA73>{L("profile.strength")}</color>";
        _strengthText.text = strong != null
            ? $"{strengthHeader}\n{string.Format(L("profile.strongest"), $"<b>{strong.speciesID}</b>", strong.score, strong.highScore)}"
            : $"{strengthHeader}\n{L("profile.no_data")}";

        // Mayor reto / fallas
        var weak = ProfileManager.Instance.GetMostChallenging();
        string challengeHeader = $"<color=#FF6060>{L("profile.challenge")}</color>";
        if (weak != null)
        {
            string template = L(weak.totalDeaths == 1 ? "profile.beaten_singular" : "profile.beaten_plural");
            _weaknessText.text = $"{challengeHeader}\n{string.Format(template, $"<b>{weak.speciesID}</b>", weak.totalDeaths)}";
        }
        else
        {
            _weaknessText.text = $"{challengeHeader}\n{L("profile.never_beaten")}";
        }

        // Tabla por especie
        RebuildRows(profile);
    }

    private void RebuildRows(ProfileData profile)
    {
        for (int i = _rowsContainer.childCount - 1; i >= 0; i--)
            Destroy(_rowsContainer.GetChild(i).gameObject);

        // Header
        var header = CreateRow(_rowsContainer,
            L("profile.col.species"), L("profile.col.score"), L("profile.col.deaths"),
            L("profile.col.lives"), L("profile.col.video"));
        header.GetComponent<Image>().color = new Color(0.18f, 0.20f, 0.28f, 1f);
        ColorRowText(header, new Color(0.7f, 0.8f, 1f, 1f), bold: true);

        // Una fila por especie con datos
        List<SpeciesProgress> sorted = new(profile.species);
        sorted.Sort((a, b) => b.score.CompareTo(a.score));

        foreach (var sp in sorted)
        {
            var row = CreateRow(_rowsContainer,
                sp.speciesID,
                sp.score.ToString(),
                sp.totalDeaths.ToString(),
                $"{sp.lives}/3",
                sp.videoSeen ? "✓" : "—");

            // Subtle row coloring por fortaleza/falla
            float t = profile.totalScore > 0 ? Mathf.InverseLerp(0f, profile.totalScore * 0.5f, sp.score) : 0f;
            row.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.20f, 0.85f + t * 0.10f);
        }
    }

    // ── Construcción de UI ────────────────────────────────────────────

    private void BuildUI()
    {
        var canvasGO = new GameObject("ProfileAnalysisCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 110;
        var sc = canvasGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        if (showFloatingButton) BuildFloatingButton(canvasGO.transform);

        BuildMainPanel(canvasGO.transform);
        _root.SetActive(false);
    }

    private void BuildFloatingButton(Transform parent)
    {
        _floatingButton = new GameObject("AnalysisFloatingBtn");
        _floatingButton.transform.SetParent(parent, false);
        var rt = _floatingButton.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.sizeDelta = floatingButtonSize;
        rt.anchoredPosition = floatingButtonPosition;

        var img = _floatingButton.AddComponent<Image>();
        img.color = buttonColor;
        var btn = _floatingButton.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Open);

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(_floatingButton.transform, false);
        var lrt = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "★";
        tmp.fontSize = 40f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private void BuildMainPanel(Transform parent)
    {
        _root = new GameObject("Root");
        _root.transform.SetParent(parent, false);
        var rrt = _root.AddComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
        rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
        var overlay = _root.AddComponent<Image>();
        overlay.color = overlayColor;

        var panel = new GameObject("Panel");
        panel.transform.SetParent(_root.transform, false);
        var prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot     = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(900f, 820f);
        panel.AddComponent<Image>().color = panelColor;

        // Título (estático arriba)
        var titleText = AddText(panel.transform, L("profile.title"),
            new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.98f),
            42f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        var titleLoc = titleText.gameObject.AddComponent<LocalizedText>();
        titleLoc.key = "profile.title";
        titleLoc.Refresh();

        // ScrollView (zona central scrollable, deja espacio para titulo arriba y boton abajo)
        BuildScrollableContent(panel.transform);

        // Botón cerrar (estático abajo)
        CreateButton(panel.transform, L("ui.close"),
            new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.08f),
            buttonColor, Close);
    }

    private void BuildScrollableContent(Transform parent)
    {
        // ── ScrollRect contenedor ──────────────────────────────────────
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(parent, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.02f, 0.10f);
        scrollRT.anchorMax = new Vector2(0.98f, 0.89f);
        scrollRT.offsetMin = Vector2.zero; scrollRT.offsetMax = Vector2.zero;
        scrollGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // ── Viewport (con Mask para recortar contenido al area visible) ─
        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        var viewportRT = viewportGO.AddComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = new Vector2(0.95f, 1f);    // deja 5% a la derecha para el scrollbar
        viewportRT.offsetMin = new Vector2(8f, 8f);
        viewportRT.offsetMax = new Vector2(-8f, -8f);
        viewportRT.pivot = new Vector2(0f, 1f);
        var viewportImg = viewportGO.AddComponent<Image>();
        viewportImg.color = new Color(1f, 1f, 1f, 0.01f);
        viewportGO.AddComponent<Mask>().showMaskGraphic = false;

        // ── Content (crece verticalmente, contiene los textos y la tabla)
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0f, 0f);

        var contentVLG = contentGO.AddComponent<VerticalLayoutGroup>();
        contentVLG.spacing = 12f;
        contentVLG.padding = new RectOffset(10, 10, 10, 10);
        contentVLG.childAlignment = TextAnchor.UpperLeft;
        contentVLG.childForceExpandWidth  = true;
        contentVLG.childForceExpandHeight = false;
        contentVLG.childControlWidth      = true;
        contentVLG.childControlHeight     = true;

        var fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRT;
        scrollRect.content  = contentRT;

        // ── Scrollbar vertical a la derecha ─────────────────────────────
        var sbGO = new GameObject("Scrollbar");
        sbGO.transform.SetParent(scrollGO.transform, false);
        var sbRT = sbGO.AddComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0.95f, 0f);
        sbRT.anchorMax = new Vector2(1f, 1f);
        sbRT.offsetMin = new Vector2(2f, 8f);
        sbRT.offsetMax = new Vector2(-4f, -8f);
        sbGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);
        var sb = sbGO.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        var slideAreaGO = new GameObject("SlidingArea");
        slideAreaGO.transform.SetParent(sbGO.transform, false);
        var slideRT = slideAreaGO.AddComponent<RectTransform>();
        slideRT.anchorMin = Vector2.zero; slideRT.anchorMax = Vector2.one;
        slideRT.offsetMin = new Vector2(4f, 4f); slideRT.offsetMax = new Vector2(-4f, -4f);

        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(slideAreaGO.transform, false);
        var handleRT = handleGO.AddComponent<RectTransform>();
        handleRT.anchorMin = Vector2.zero; handleRT.anchorMax = Vector2.one;
        handleRT.offsetMin = Vector2.zero; handleRT.offsetMax = Vector2.zero;
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = new Color(0.9f, 0.9f, 1f, 0.7f);
        sb.targetGraphic = handleImg;
        sb.handleRect = handleRT;

        scrollRect.verticalScrollbar = sb;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // ── Crear los textos como hijos del Content (los maneja VerticalLayoutGroup)
        _summaryText  = CreateContentText(contentGO.transform, 22f);
        _strengthText = CreateContentText(contentGO.transform, 22f);
        _weaknessText = CreateContentText(contentGO.transform, 22f);

        // Contenedor de filas de tabla (también dentro del scrollable)
        var rowsGO = new GameObject("Rows");
        rowsGO.transform.SetParent(contentGO.transform, false);
        _rowsContainer = rowsGO.AddComponent<RectTransform>();

        var rowsVLG = rowsGO.AddComponent<VerticalLayoutGroup>();
        rowsVLG.spacing = 4f;
        rowsVLG.childForceExpandWidth  = true;
        rowsVLG.childForceExpandHeight = false;
        rowsVLG.childControlHeight     = false;
        rowsVLG.childControlWidth      = true;

        var rowsFitter = rowsGO.AddComponent<ContentSizeFitter>();
        rowsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private TMP_Text CreateContentText(Transform parent, float size)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.flexibleHeight = -1f;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = size;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private GameObject CreateRow(Transform parent, string c1, string c2, string c3, string c4, string c5)
    {
        var go = new GameObject("Row");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 38f;
        le.preferredHeight = 38f;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.14f, 0.20f, 0.85f);

        AddTextAnchored(go.transform, c1, 0.02f, 0.40f, 20f, TextAlignmentOptions.MidlineLeft);
        AddTextAnchored(go.transform, c2, 0.40f, 0.56f, 20f, TextAlignmentOptions.Midline);
        AddTextAnchored(go.transform, c3, 0.56f, 0.72f, 20f, TextAlignmentOptions.Midline);
        AddTextAnchored(go.transform, c4, 0.72f, 0.86f, 20f, TextAlignmentOptions.Midline);
        AddTextAnchored(go.transform, c5, 0.86f, 0.98f, 20f, TextAlignmentOptions.Midline);

        return go;
    }

    private void ColorRowText(GameObject row, Color color, bool bold)
    {
        foreach (var t in row.GetComponentsInChildren<TextMeshProUGUI>())
        {
            t.color = color;
            if (bold) t.fontStyle = FontStyles.Bold;
        }
    }

    private TMP_Text AddText(Transform parent, string text,
                             Vector2 ancMin, Vector2 ancMax, float size,
                             FontStyles style, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void AddTextAnchored(Transform parent, string text,
                                 float xMin, float xMax,
                                 float size, TextAlignmentOptions align)
    {
        var go = new GameObject("Cell");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, 0f);
        rt.anchorMax = new Vector2(xMax, 1f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = Color.white;
        tmp.alignment = align;
        tmp.raycastTarget = false;
    }

    private void CreateButton(Transform parent, string label,
                              Vector2 anchorMin, Vector2 anchorMax,
                              Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var labelTmp = AddText(go.transform, label,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            26f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

        // Auto-localiza si el label coincide con una entrada del diccionario
        if (LocalizationManager.Instance != null)
        {
            string key = LocalizationManager.Instance.FindKey(label);
            if (key != null)
            {
                var loc = labelTmp.gameObject.AddComponent<LocalizedText>();
                loc.key = key;
                loc.Refresh();
            }
        }
    }
}
