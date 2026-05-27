using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Panel de inspección que ocupa la mitad DERECHA de la pantalla.
/// Se autoinicializa: crea su propio Canvas, Background, VideoPlayer, RawImage y textos.
///
/// Llama Show(data) para mostrar; Hide() para ocultar.
/// </summary>
public class ButterflyVideoPanel : MonoBehaviour
{
    private Canvas        _canvas;
    private GameObject    _panelRoot;
    private RawImage      _videoDisplay;
    private VideoPlayer   _videoPlayer;
    private RenderTexture _rt;
    private TMP_Text      _commonNameText;
    private TMP_Text      _scientificNameText;
    private TMP_Text      _descriptionText;
    private bool          _initialized;

    // ─────────────────────────────────────────────────────────────────

    public void Show(ButterflySpeciesData data)
    {
        Initialize();

        if (data != null)
        {
            _commonNameText.text     = data.commonName;
            _scientificNameText.text = $"<i>{data.scientificName}</i>";
            _descriptionText.text    = data.description;

            if (_videoPlayer != null && data.videoClip != null)
            {
                _videoPlayer.clip   = data.videoClip;
                _videoPlayer.isLooping = true;
                _videoPlayer.Play();
            }
            else if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
            }
        }
        else
        {
            _commonNameText.text     = "Sin datos";
            _scientificNameText.text = "";
            _descriptionText.text    = "Asigna un ButterflySpeciesData en el ButterflyIdentity de este prefab.";
            if (_videoPlayer != null) _videoPlayer.Stop();
        }

        _panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (!_initialized) return;
        if (_videoPlayer != null) _videoPlayer.Stop();
        _panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_rt != null) _rt.Release();
    }

    // ── Inicialización diferida (al primer Show) ──────────────────────

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // Canvas dedicado para el panel
        var canvasGO = new GameObject("ButterflyVideoPanelCanvas");
        _canvas      = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 90;          // por debajo del botón Volver (que es 100)
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920f, 1080f);
        scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight   = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Panel raíz — ocupa la MITAD DERECHA de la pantalla
        _panelRoot = new GameObject("Panel");
        _panelRoot.transform.SetParent(canvasGO.transform, false);
        var panelRT = _panelRoot.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(1f,   1f);
        panelRT.offsetMin = new Vector2(20f,  20f);
        panelRT.offsetMax = new Vector2(-20f, -20f);

        var bg = _panelRoot.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.06f, 0.10f, 0.95f);

        // Nombre común — barra superior
        _commonNameText = CreateText("CommonName",
            new Vector2(0.03f, 0.85f), new Vector2(0.97f, 0.97f),
            48f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        // Nombre científico — debajo del común
        _scientificNameText = CreateText("ScientificName",
            new Vector2(0.03f, 0.78f), new Vector2(0.97f, 0.86f),
            28f, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);

        // Video — franja central
        _videoDisplay = CreateRawImage("VideoDisplay",
            new Vector2(0.03f, 0.30f), new Vector2(0.97f, 0.76f));

        // Descripción — franja inferior
        _descriptionText = CreateText("Description",
            new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.28f),
            22f, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        // VideoPlayer
        _videoPlayer = canvasGO.AddComponent<VideoPlayer>();
        _rt = new RenderTexture(1280, 720, 0);
        _videoPlayer.renderMode    = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture = _rt;
        _videoDisplay.texture      = _rt;

        var src = canvasGO.AddComponent<AudioSource>();
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _videoPlayer.SetTargetAudioSource(0, src);

        _panelRoot.SetActive(false);
    }

    // ── Helpers UI ────────────────────────────────────────────────────

    private RawImage CreateRawImage(string goName, Vector2 ancMin, Vector2 ancMax)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(_panelRoot.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = ancMin;
        rt.anchorMax        = ancMax;
        rt.sizeDelta        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return go.AddComponent<RawImage>();
    }

    private TMP_Text CreateText(string goName, Vector2 ancMin, Vector2 ancMax,
                                float size, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(_panelRoot.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = ancMin;
        rt.anchorMax        = ancMax;
        rt.sizeDelta        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = Color.white;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        return tmp;
    }
}
