using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD del juego: muestra puntos de la especie actual + vidas restantes.
/// Se auto-construye al iniciar (Canvas dedicado, esquina superior izquierda).
///
/// Actualiza cada frame leyendo ProfileManager + spawner.ActiveButterfly.
/// Reacciona a:
///   - OnLifeLost: parpadeo rojo cuando perdés una vida.
///   - OnSpeciesReset: parpadeo intenso cuando perdés las 3 y reset.
///
/// SETUP:
///   1. Adjunta este script a "InteractionManager".
///   2. (Opcional) Ajusta posición y tamaño desde el inspector.
/// </summary>
public class ScoreHUD : MonoBehaviour
{
    [Header("Referencias (auto-detección si están vacías)")]
    public MariposarioSpawner spawner;

    [Header("Posición y tamaño")]
    public Vector2 anchoredPosition = new Vector2(20f, -20f);
    public Vector2 panelSize        = new Vector2(260f, 86f);

    [Header("Estilo")]
    public Color panelColor    = new Color(0f, 0f, 0f, 0.5f);
    public Color textColor     = Color.white;
    public Color liveColor     = new Color(1f, 0.25f, 0.35f, 1f);
    public Color emptyLiveColor = new Color(0.35f, 0.35f, 0.4f, 1f);
    public Color damageFlashColor = new Color(1f, 0.1f, 0.1f, 1f);
    [Range(10f, 40f)] public float textSize = 20f;
    [Range(16f, 48f)] public float heartSize = 26f;

    // ── UI runtime ────────────────────────────────────────────────────
    private Canvas        _canvas;
    private GameObject    _panel;
    private TMP_Text      _scoreText;
    private TMP_Text      _speciesText;
    private Image[]       _hearts = new Image[3];
    private float         _flashUntil;
    private float         _resetFlashUntil;
    private static Sprite _heartSprite;

    // ═════════════════════════════════════════════════════════════════

    private void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<MariposarioSpawner>();
        BuildHeartSprite();
        BuildUI();

        if (ProfileManager.Instance != null)
        {
            ProfileManager.Instance.OnLifeLost     += OnLifeLost;
            ProfileManager.Instance.OnSpeciesReset += OnSpeciesReset;
            ProfileManager.Instance.OnScoreChanged += OnScoreChanged;
        }
    }

    private void OnDestroy()
    {
        if (ProfileManager.Instance != null)
        {
            ProfileManager.Instance.OnLifeLost     -= OnLifeLost;
            ProfileManager.Instance.OnSpeciesReset -= OnSpeciesReset;
            ProfileManager.Instance.OnScoreChanged -= OnScoreChanged;
        }
    }

    private void Update()
    {
        RefreshDisplay();
    }

    // ── Callbacks ─────────────────────────────────────────────────────

    private void OnLifeLost(string speciesID, int lives) { _flashUntil = Time.time + 0.35f; }
    private void OnSpeciesReset(string speciesID)        { _resetFlashUntil = Time.time + 1.2f; }
    private void OnScoreChanged(string speciesID, int newScore) { /* refresca en Update */ }

    // ── Refresh ───────────────────────────────────────────────────────

    private void RefreshDisplay()
    {
        if (_scoreText == null || _speciesText == null) return;

        string speciesID = GetCurrentSpeciesID();
        SpeciesProgress sp = ProfileManager.Instance != null && !string.IsNullOrEmpty(speciesID)
            ? ProfileManager.Instance.GetSpecies(speciesID)
            : null;

        int score = sp != null ? sp.score : 0;
        int lives = sp != null ? sp.lives : 3;

        string puntos = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.Get("hud.points")
            : "PUNTOS";
        _scoreText.text   = $"{puntos}  <b>{score}</b>";
        _speciesText.text = string.IsNullOrEmpty(speciesID) ? "—" : speciesID;

        // Color del texto: flash si daño/reset
        Color c = textColor;
        if (Time.time < _resetFlashUntil)
            c = Color.Lerp(textColor, damageFlashColor,
                Mathf.PingPong((_resetFlashUntil - Time.time) * 6f, 1f));
        else if (Time.time < _flashUntil)
            c = damageFlashColor;
        _scoreText.color   = c;
        _speciesText.color = c;

        // Corazones
        for (int i = 0; i < _hearts.Length; i++)
        {
            if (_hearts[i] == null) continue;
            _hearts[i].color = i < lives ? liveColor : emptyLiveColor;
        }
    }

    private string GetCurrentSpeciesID()
    {
        if (spawner == null || spawner.ActiveButterfly == null) return null;
        var d = spawner.ActiveButterfly.GetData();
        return d != null ? d.speciesName : null;
    }

    // ── Construcción de UI ────────────────────────────────────────────

    private void BuildUI()
    {
        var canvasGO = new GameObject("ScoreHUDCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 75;
        var sc = canvasGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("ScorePanel");
        _panel.transform.SetParent(canvasGO.transform, false);
        var rt = _panel.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.sizeDelta        = new Vector2(panelSize.x, 78f);   // mas chico, sin la fila de especie
        rt.anchoredPosition = anchoredPosition;

        var bg = _panel.AddComponent<Image>();
        bg.color = panelColor;
        bg.raycastTarget = false;

        // Score (arriba del panel, ahora sin especie)
        _scoreText = CreateText("Score",
            new Vector2(10f, -8f), new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(-10f, -40f), textSize, FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft);

        // Texto de la especie — CENTRADO ARRIBA en pantalla completa
        var speciesGO = new GameObject("SpeciesNameTopCenter");
        speciesGO.transform.SetParent(canvasGO.transform, false);
        var srt = speciesGO.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 1f);
        srt.anchorMax = new Vector2(0.5f, 1f);
        srt.pivot     = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -30f);   // 30px desde arriba
        srt.sizeDelta = new Vector2(700f, 60f);

        _speciesText = speciesGO.AddComponent<TextMeshProUGUI>();
        _speciesText.fontSize  = 40f;
        _speciesText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        _speciesText.color     = textColor;
        _speciesText.alignment = TextAlignmentOptions.Center;
        _speciesText.raycastTarget = false;
        _speciesText.enableWordWrapping = false;
        _speciesText.outlineWidth = 0.2f;
        _speciesText.outlineColor = new Color(0f, 0f, 0f, 0.9f);

        // Corazones (abajo, fila horizontal)
        for (int i = 0; i < 3; i++)
        {
            var hGO = new GameObject($"Heart{i}");
            hGO.transform.SetParent(_panel.transform, false);
            var hrt = hGO.AddComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0f);
            hrt.anchorMax = new Vector2(0f, 0f);
            hrt.pivot     = new Vector2(0f, 0f);
            hrt.sizeDelta = new Vector2(heartSize, heartSize);
            hrt.anchoredPosition = new Vector2(10f + i * (heartSize + 4f), 8f);

            var img = hGO.AddComponent<Image>();
            img.sprite = _heartSprite;
            img.color  = liveColor;
            img.raycastTarget = false;

            _hearts[i] = img;
        }
    }

    private TMP_Text CreateText(string goName, Vector2 anchoredPos,
                                Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta,
                                float size, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(_panel.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(0f, 1f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = textColor;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    // ── Heart sprite procedural ───────────────────────────────────────

    private static void BuildHeartSprite()
    {
        if (_heartSprite != null) return;
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px  = new Color32[size * size];

        // Forma de corazón: dos círculos arriba + triángulo invertido abajo
        float r = size * 0.22f;
        Vector2 c1 = new Vector2(size * 0.32f, size * 0.66f);
        Vector2 c2 = new Vector2(size * 0.68f, size * 0.66f);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                bool inCircle1 = (p - c1).sqrMagnitude <= r * r;
                bool inCircle2 = (p - c2).sqrMagnitude <= r * r;

                // Triángulo: vértices (0.1,0.55) (0.9,0.55) (0.5,0.05) (en unidades 0..size)
                Vector2 A = new Vector2(size * 0.10f, size * 0.55f);
                Vector2 B = new Vector2(size * 0.90f, size * 0.55f);
                Vector2 C = new Vector2(size * 0.50f, size * 0.05f);
                bool inTri = PointInTriangle(p, A, B, C);

                bool inside = inCircle1 || inCircle2 || inTri;
                px[y * size + x] = new Color32(255, 255, 255, inside ? (byte)255 : (byte)0);
            }
        tex.SetPixels32(px); tex.Apply();
        _heartSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) =>
        (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }
}
