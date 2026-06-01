using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pantalla de selección/creación de perfil al iniciar el juego.
/// Se auto-construye como overlay sobre cualquier Canvas existente.
///
/// MUESTRA al iniciar:
///   - Lista de perfiles guardados (botón por cada uno).
///   - Botón "Crear nuevo perfil" → abre input para nombre.
///   - Botón "Continuar" si ya hay un perfil activo (entra directo).
///
/// SETUP:
///   1. Adjunta este script a un GameObject persistente de la escena del menú
///      (ej. ProfileManager o un GameObject "ProfileSelector").
///   2. Activa "Show On Start" si quieres que aparezca al cargar la escena.
///   3. Llama Open() desde un botón del menú si quieres invocarlo manualmente.
/// </summary>
public class ProfileSelectionUI : MonoBehaviour
{
    [Header("Comportamiento")]
    [Tooltip("Si está activado, se muestra automáticamente al iniciar.")]
    public bool showOnStart = true;

    [Header("Estilo")]
    public Color overlayColor    = new Color(0f, 0f, 0f, 0.85f);
    public Color panelColor      = new Color(0.10f, 0.12f, 0.18f, 0.98f);
    public Color buttonColor     = new Color(0.20f, 0.45f, 0.80f, 1f);
    public Color buttonAltColor  = new Color(0.30f, 0.30f, 0.35f, 1f);
    public Color buttonDangerCol = new Color(0.65f, 0.20f, 0.20f, 1f);
    public Color textColor       = Color.white;

    // ── UI runtime ────────────────────────────────────────────────────
    private Canvas        _canvas;
    private GameObject    _root;
    private RectTransform _listContainer;
    private TMP_InputField _nameInput;
    private GameObject    _createPanel;
    private TMP_Text      _titleText;

    // ═════════════════════════════════════════════════════════════════

    private void Start()
    {
        BuildUI();
        if (showOnStart) Open();
        else Close();
    }

    public void Open()
    {
        _root.SetActive(true);
        RefreshList();
    }

    public void Close()
    {
        if (_root != null) _root.SetActive(false);
    }

    // ── Lista de perfiles ─────────────────────────────────────────────

    private void RefreshList()
    {
        if (_listContainer == null || ProfileManager.Instance == null) return;

        for (int i = _listContainer.childCount - 1; i >= 0; i--)
            Destroy(_listContainer.GetChild(i).gameObject);

        List<ProfileData> profiles = ProfileManager.Instance.GetAllProfiles();
        string activeName = ProfileManager.Instance.Active != null
            ? ProfileManager.Instance.Active.userName : null;

        foreach (var p in profiles)
        {
            string profName = p.userName;
            string subtitle = $"{p.totalScore} pts  ·  {p.CountSpeciesWithVideoSeen()}/{ProfileManager.Instance.TotalSpecies} especies";
            bool   isActive = profName == activeName;

            var row = CreateButton(_listContainer, profName,
                isActive ? buttonColor : buttonAltColor,
                () => { ProfileManager.Instance.SwitchProfile(profName); Close(); });
            AddSubtitle(row, subtitle);

            AddDeleteButton(row, profName);
        }

        var createBtn = CreateButton(_listContainer, "+ Crear nuevo perfil",
            buttonAltColor, ShowCreatePanel);
        createBtn.GetComponent<LayoutElement>().minHeight = 56f;
    }

    private void ShowCreatePanel()
    {
        _createPanel.SetActive(true);
        _nameInput.text = "";
        _nameInput.Select();
        _nameInput.ActivateInputField();
    }

    private void OnCreateConfirm()
    {
        string name = _nameInput.text != null ? _nameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name)) return;
        if (ProfileManager.Instance.CreateProfile(name))
        {
            _createPanel.SetActive(false);
            Close();
        }
        else
        {
            // Nombre duplicado o vacío: feedback simple — solo refrescamos
            _nameInput.text = "";
            _nameInput.ActivateInputField();
        }
    }

    // ── Construcción de UI ────────────────────────────────────────────

    private void BuildUI()
    {
        var canvasGO = new GameObject("ProfileSelectionCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200;
        var sc = canvasGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _root = new GameObject("Root");
        _root.transform.SetParent(canvasGO.transform, false);
        var rrt = _root.AddComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
        rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
        var overlay = _root.AddComponent<Image>();
        overlay.color = overlayColor;

        // Panel central
        var panel = new GameObject("Panel");
        panel.transform.SetParent(_root.transform, false);
        var prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot     = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(600f, 720f);
        var pImg = panel.AddComponent<Image>();
        pImg.color = panelColor;

        // Título
        _titleText = AddText(panel.transform, "Elige tu perfil",
            new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f),
            44f, FontStyles.Bold, TextAlignmentOptions.Center);

        // Contenedor de lista con VerticalLayoutGroup
        var listGO = new GameObject("List");
        listGO.transform.SetParent(panel.transform, false);
        var lrt = listGO.AddComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.05f, 0.05f);
        lrt.anchorMax = new Vector2(0.95f, 0.86f);
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        _listContainer = lrt;

        var vlg = listGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // Panel de creación (overlay)
        BuildCreatePanel();

        _root.SetActive(false);
    }

    private void BuildCreatePanel()
    {
        _createPanel = new GameObject("CreatePanel");
        _createPanel.transform.SetParent(_root.transform, false);
        var rt = _createPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 260f);

        var bg = _createPanel.AddComponent<Image>();
        bg.color = panelColor;

        AddText(_createPanel.transform, "Nombre del nuevo perfil",
            new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f),
            28f, FontStyles.Bold, TextAlignmentOptions.Center);

        // Input
        var inGO = new GameObject("Input");
        inGO.transform.SetParent(_createPanel.transform, false);
        var inRT = inGO.AddComponent<RectTransform>();
        inRT.anchorMin = new Vector2(0.08f, 0.42f);
        inRT.anchorMax = new Vector2(0.92f, 0.70f);
        inRT.offsetMin = Vector2.zero; inRT.offsetMax = Vector2.zero;
        var inBg = inGO.AddComponent<Image>();
        inBg.color = new Color(1f, 1f, 1f, 0.10f);

        _nameInput = inGO.AddComponent<TMP_InputField>();
        _nameInput.characterLimit = 20;

        // TMP textComponent (visible) + placeholder
        var textArea = new GameObject("Text");
        textArea.transform.SetParent(inGO.transform, false);
        var trt = textArea.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(14f, 6f);
        trt.offsetMax = new Vector2(-14f, -6f);
        var tmpComp = textArea.AddComponent<TextMeshProUGUI>();
        tmpComp.color = Color.white;
        tmpComp.fontSize = 28f;
        tmpComp.alignment = TextAlignmentOptions.MidlineLeft;
        _nameInput.textComponent = tmpComp;

        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(inGO.transform, false);
        var phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = new Vector2(0f, 0f);
        phRT.anchorMax = new Vector2(1f, 1f);
        phRT.offsetMin = new Vector2(14f, 6f);
        phRT.offsetMax = new Vector2(-14f, -6f);
        var ph = phGO.AddComponent<TextMeshProUGUI>();
        ph.color = new Color(1f, 1f, 1f, 0.4f);
        ph.fontSize = 28f;
        ph.fontStyle = FontStyles.Italic;
        ph.alignment = TextAlignmentOptions.MidlineLeft;
        ph.text = "Tu nombre…";
        _nameInput.placeholder = ph;

        CreateButtonAbsolute(_createPanel.transform, "Aceptar",
            new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.34f),
            buttonColor, OnCreateConfirm);
        CreateButtonAbsolute(_createPanel.transform, "Cancelar",
            new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.34f),
            buttonAltColor, () => _createPanel.SetActive(false));

        _createPanel.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private GameObject CreateButton(Transform parent, string label, Color color,
                                    UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 70f);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 70f;
        le.preferredHeight = 70f;

        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        AddText(go.transform, label,
            new Vector2(0.04f, 0.32f), new Vector2(0.85f, 0.95f),
            26f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        return go;
    }

    private void AddSubtitle(GameObject parent, string text)
    {
        AddText(parent.transform, text,
            new Vector2(0.04f, 0.04f), new Vector2(0.85f, 0.36f),
            18f, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
    }

    private void AddDeleteButton(GameObject parent, string profileName)
    {
        var go = new GameObject("Delete");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.87f, 0.18f);
        rt.anchorMax = new Vector2(0.97f, 0.82f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = buttonDangerCol;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() =>
        {
            ProfileManager.Instance.DeleteProfile(profileName);
            RefreshList();
        });

        AddText(go.transform, "X",
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            28f, FontStyles.Bold, TextAlignmentOptions.Center);
    }

    private TMP_Text AddText(Transform parent, string text,
                             Vector2 anchorMin, Vector2 anchorMax,
                             float size, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = textColor;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void CreateButtonAbsolute(Transform parent, string label,
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
            26f, FontStyles.Bold, TextAlignmentOptions.Center);
    }
}
