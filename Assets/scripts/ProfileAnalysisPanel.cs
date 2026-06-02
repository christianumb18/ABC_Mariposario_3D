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

    private void Start()
    {
        BuildUI();
    }

    public void Open()
    {
        _root.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (_root != null) _root.SetActive(false);
    }

    // ── Análisis ──────────────────────────────────────────────────────

    private void Refresh()
    {
        if (ProfileManager.Instance == null || ProfileManager.Instance.Active == null) return;
        var profile = ProfileManager.Instance.Active;

        // Resumen general
        _summaryText.text =
            $"<b>{profile.userName}</b>\n" +
            $"Puntaje total: <b>{profile.totalScore}</b>\n" +
            $"Tiempo de vuelo: <b>{ProfileManager.Instance.FlightTimeFormatted}</b>\n" +
            $"Especies con video visto: <b>{profile.CountSpeciesWithVideoSeen()}/{ProfileManager.Instance.TotalSpecies}</b>";

        // Fortaleza
        var strong = ProfileManager.Instance.GetStrongest();
        _strengthText.text = strong != null
            ? $"<color=#56DA73>★ Fortaleza</color>\nTu especie más fuerte: <b>{strong.speciesID}</b>  ({strong.score} pts, récord {strong.highScore})"
            : "<color=#56DA73>★ Fortaleza</color>\nAún sin datos — explora y suma puntos.";

        // Mayor reto / fallas
        var weak = ProfileManager.Instance.GetMostChallenging();
        _weaknessText.text = weak != null
            ? $"<color=#FF6060>⚠ Mayor reto</color>\n<b>{weak.speciesID}</b> te ha vencido {weak.totalDeaths} {(weak.totalDeaths == 1 ? "vez" : "veces")}. Ten cuidado con los depredadores."
            : "<color=#FF6060>⚠ Mayor reto</color>\nNo te ha vencido ningún depredador. ¡Buen trabajo!";

        // Tabla por especie
        RebuildRows(profile);
    }

    private void RebuildRows(ProfileData profile)
    {
        for (int i = _rowsContainer.childCount - 1; i >= 0; i--)
            Destroy(_rowsContainer.GetChild(i).gameObject);

        // Header
        var header = CreateRow(_rowsContainer, "Especie", "Score", "Muertes", "Vidas", "Video");
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

        // Título
        AddText(panel.transform, "Tu progreso",
            new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.98f),
            42f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

        // Resumen general
        _summaryText = AddText(panel.transform, "",
            new Vector2(0.04f, 0.74f), new Vector2(0.96f, 0.89f),
            22f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white);

        // Fortaleza
        _strengthText = AddText(panel.transform, "",
            new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.73f),
            22f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white);

        // Falla
        _weaknessText = AddText(panel.transform, "",
            new Vector2(0.04f, 0.50f), new Vector2(0.96f, 0.61f),
            22f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white);

        // Contenedor de filas (tabla)
        var rowsGO = new GameObject("Rows");
        rowsGO.transform.SetParent(panel.transform, false);
        _rowsContainer = rowsGO.AddComponent<RectTransform>();
        _rowsContainer.anchorMin = new Vector2(0.04f, 0.10f);
        _rowsContainer.anchorMax = new Vector2(0.96f, 0.48f);
        _rowsContainer.offsetMin = Vector2.zero; _rowsContainer.offsetMax = Vector2.zero;

        var vlg = rowsGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight     = false;

        // Botones
        CreateButton(panel.transform, "Cerrar",
            new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.08f),
            buttonColor, Close);
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

        AddText(go.transform, label,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            26f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
    }
}
