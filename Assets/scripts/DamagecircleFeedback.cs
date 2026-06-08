using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Feedback visual de daño: dibuja un círculo rojo alrededor de la mariposa
/// cuando pierde una vida. Se engancha al evento OnLifeLost de ProfileManager
/// (el mismo que usa ScoreHUD), así que no toca el sistema de vidas existente,
/// solo reacciona a él.
///
/// El círculo es un anillo rojo en pantalla, centrado sobre la mariposa, que
/// crece un poco y se desvanece en medio segundo.
///
/// SETUP:
///   1. Adjunta este script al "InteractionManager" (junto a ScoreManager y ScoreHUD).
///   2. (Opcional) Ajusta color, duración y tamaño desde el Inspector.
/// </summary>
public class DamageCircleFeedback : MonoBehaviour
{
    [Header("Referencias (auto-detección si está vacío)")]
    public MariposarioSpawner spawner;

    [Header("Apariencia")]
    public Color circleColor = new Color(1f, 0.1f, 0.1f, 1f);
    [Tooltip("Tamaño base del círculo en pantalla (px a 1080p)")]
    public float baseSize = 200f;
    [Tooltip("Altura sobre la mariposa donde se centra el círculo")]
    public float worldYOffset = 1.5f;

    [Header("Animación")]
    [Tooltip("Cuánto dura el efecto en segundos")]
    public float duration = 0.5f;
    [Tooltip("Cuánto crece el círculo durante el efecto (1 = no crece)")]
    public float growth = 1.5f;

    // ── Runtime ───────────────────────────────────────────────────────
    private Canvas _canvas;
    private RectTransform _canvasRect;
    private RectTransform _circle;
    private Image _circleImg;
    private float _startTime = -999f;
    private bool _active;
    private static Sprite _ringSprite;

    // ═════════════════════════════════════════════════════════════════

    private void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<MariposarioSpawner>();

        BuildRingSprite();
        BuildUI();

        if (ProfileManager.Instance != null)
        {
            ProfileManager.Instance.OnLifeLost     += OnLifeLost;
            ProfileManager.Instance.OnSpeciesReset += OnSpeciesReset;
        }
        else
        {
            Debug.LogWarning("[DamageCircleFeedback] ProfileManager no encontrado al iniciar.", this);
        }
    }

    private void OnDestroy()
    {
        if (ProfileManager.Instance != null)
        {
            ProfileManager.Instance.OnLifeLost     -= OnLifeLost;
            ProfileManager.Instance.OnSpeciesReset -= OnSpeciesReset;
        }
    }

    // ── Disparadores ─────────────────────────────────────────────────
    private void OnLifeLost(string speciesID, int lives) => TriggerCircle();
    private void OnSpeciesReset(string speciesID)         => TriggerCircle();

    private void TriggerCircle()
    {
        _startTime = Time.time;
        _active = true;
        if (_circleImg != null) _circleImg.enabled = true;
    }

    // ── Animación cada frame ─────────────────────────────────────────
    private void Update()
    {
        if (!_active || _circle == null) return;

        float t = (Time.time - _startTime) / duration;
        if (t >= 1f)
        {
            _active = false;
            _circleImg.enabled = false;
            return;
        }

        // Posiciona el círculo sobre la mariposa
        Camera cam = Camera.main;
        if (cam != null && spawner != null && spawner.ActiveButterfly != null)
        {
            Vector3 world = spawner.ActiveButterfly.transform.position + Vector3.up * worldYOffset;
            Vector3 screen = cam.WorldToScreenPoint(world);

            if (screen.z < 0f)
            {
                // La mariposa está detrás de la cámara: oculta el círculo
                _circleImg.enabled = false;
                return;
            }
            _circleImg.enabled = true;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screen, null, out Vector2 local);
            _circle.anchoredPosition = local;
        }

        // Crece y se desvanece
        float scale = Mathf.Lerp(1f, growth, t);
        _circle.localScale = Vector3.one * scale;

        Color c = circleColor;
        c.a = Mathf.Lerp(1f, 0f, t);
        _circleImg.color = c;
    }

    // ── Construcción de la UI ────────────────────────────────────────
    private void BuildUI()
    {
        var canvasGO = new GameObject("DamageCircleCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 80;   // por encima del HUD
        var sc = canvasGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        _canvasRect = canvasGO.GetComponent<RectTransform>();

        var go = new GameObject("DamageCircle");
        go.transform.SetParent(canvasGO.transform, false);
        _circle = go.AddComponent<RectTransform>();
        _circle.anchorMin = new Vector2(0.5f, 0.5f);
        _circle.anchorMax = new Vector2(0.5f, 0.5f);
        _circle.pivot     = new Vector2(0.5f, 0.5f);
        _circle.sizeDelta = new Vector2(baseSize, baseSize);

        _circleImg = go.AddComponent<Image>();
        _circleImg.sprite = _ringSprite;
        _circleImg.color  = circleColor;
        _circleImg.raycastTarget = false;
        _circleImg.enabled = false;   // oculto hasta que haya daño
    }

    // ── Sprite procedural del anillo rojo ────────────────────────────
    private static void BuildRingSprite()
    {
        if (_ringSprite != null) return;
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerR = size * 0.48f;
        float innerR = size * 0.36f;   // grosor del anillo

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                bool inRing = dist <= outerR && dist >= innerR;
                px[y * size + x] = new Color32(255, 255, 255, inRing ? (byte)255 : (byte)0);
            }

        tex.SetPixels32(px); tex.Apply();
        _ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}