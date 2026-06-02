using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crea íconos en el minimapa para HostPlant + MinimapTarget en MODO DESCUBRIMIENTO,
/// con filtro por especie:
///   - HostPlant y Nectar: SOLO aparecen las que coinciden con tu especie actual.
///     Las de otras especies nunca se muestran.
///   - Predator: aparecen apenas los descubras (a cualquier distancia ≤ discoveryRadius)
///     y se dibuja una ZONA DE ALERTA de `predatorAlertRadius` metros alrededor.
///
/// Si cambias de mariposa (OnChooseBecomeButterfly), las hospederas/nectaríferas
/// se reevalúan automáticamente: las de la nueva especie aparecen al descubrirlas,
/// las de la especie anterior desaparecen.
///
/// SETUP:
///   1. Adjunta este script al GameObject MinimapFrame.
///   2. (Opcional) Arrastra MinimapImage, MinimapCamera y MariposarioSpawner.
///      Si los dejas vacíos se auto-detectan en Start.
/// </summary>
public class MinimapMarkerSystem : MonoBehaviour
{
    [Header("Referencias (auto-detección si están vacías)")]
    public RectTransform minimapArea;
    public Camera        minimapCamera;
    public MariposarioSpawner spawner;

    [Header("Descubrimiento")]
    [Tooltip("Distancia (metros, plano XZ) a la que se descubre un objetivo")]
    [Range(1f, 30f)] public float discoveryRadius = 8f;

    [Tooltip("Duración del flash al descubrir (segundos)")]
    [Range(0.1f, 2f)] public float flashDuration = 0.6f;

    [Tooltip("Cuánto crece el ícono en el flash (1.8 = 80% más grande al inicio)")]
    [Range(1f, 3f)] public float flashScalePeak = 1.8f;

    [Header("Filtro por especie")]
    [Tooltip("Si está activado, hospederas y nectaríferas solo aparecen si su " +
             "hostPlantID coincide con tu especie actual. Los depredadores se " +
             "muestran siempre que los descubras.")]
    public bool filterHostAndNectarBySpecies = true;

    [Header("Depredadores")]
    [Tooltip("Radio (en metros del mundo) de la zona de alerta alrededor del depredador")]
    [Range(0.5f, 15f)] public float predatorAlertRadius = 3f;

    [Tooltip("Color del círculo de alerta (alpha bajo = translúcido)")]
    public Color predatorAlertColor = new Color(1f, 0.15f, 0.15f, 0.28f);

    [Header("Estilo")]
    [Range(8f, 48f)] public float iconSize = 20f;
    [Range(0f, 30f)] public float edgePadding = 10f;

    [Header("Colores por tipo")]
    public Color hostPlantColor = new Color(1f,    0.2f,  0.85f, 1f);
    public Color nectarColor    = new Color(0.25f, 1f,    0.45f, 1f);
    public Color predatorColor  = new Color(1f,    0.15f, 0.15f, 1f);

    [Header("Pulso de la hospedera de tu especie")]
    public bool  pulseSpeciesHostPlant = true;
    [Range(0.5f, 6f)] public float pulseSpeed = 3f;
    [Range(0f, 0.5f)] public float pulseAmplitude = 0.25f;

    // ── Estado interno ────────────────────────────────────────────────
    private readonly List<MarkerEntry> _markers = new();
    private static Sprite _circleSprite;
    private static Sprite _triangleSprite;

    private class MarkerEntry
    {
        public Transform         target;
        public MinimapTargetType type;
        public string            hostPlantID;
        public RectTransform     icon;
        public Image             iconImage;
        public RectTransform     alertRing;   // solo Predator
        public Image             alertRingImage;
        public Color             baseColor;
        public bool              discovered;
        public float             discoveredTime;
    }

    // ═════════════════════════════════════════════════════════════════

    private void Start()
    {
        AutoDetectReferences();
        BuildSprites();
        DiscoverTargets();
    }

    private void LateUpdate()
    {
        if (minimapArea == null || minimapCamera == null) return;

        Transform butterfly = spawner != null && spawner.ActiveButterfly != null
            ? spawner.ActiveButterfly.transform
            : null;

        string currentHostID = GetCurrentHostPlantID();
        float pixelsPerMeter = GetPixelsPerMeter();

        for (int i = _markers.Count - 1; i >= 0; i--)
        {
            var m = _markers[i];

            if (m.target == null)
            {
                if (m.icon != null)     Destroy(m.icon.gameObject);
                if (m.alertRing != null) Destroy(m.alertRing.gameObject);
                _markers.RemoveAt(i);
                continue;
            }

            // ─── Filtro por especie ───────────────────────────────────
            bool speciesMatch =
                m.type == MinimapTargetType.Predator
                || !filterHostAndNectarBySpecies
                || (!string.IsNullOrEmpty(currentHostID)
                    && !string.IsNullOrEmpty(m.hostPlantID)
                    && m.hostPlantID == currentHostID);

            if (!speciesMatch)
            {
                if (m.icon.gameObject.activeSelf)      m.icon.gameObject.SetActive(false);
                if (m.alertRing != null && m.alertRing.gameObject.activeSelf)
                    m.alertRing.gameObject.SetActive(false);
                continue;
            }

            // ─── Descubrimiento ───────────────────────────────────────
            if (!m.discovered)
            {
                if (butterfly == null) continue;

                float dx = m.target.position.x - butterfly.position.x;
                float dz = m.target.position.z - butterfly.position.z;
                float distSqr = dx * dx + dz * dz;
                float radSqr  = discoveryRadius * discoveryRadius;

                if (distSqr <= radSqr)
                {
                    m.discovered     = true;
                    m.discoveredTime = Time.time;

                    // Persistir descubrimiento en el perfil del jugador (para que
                    // se conserve entre sesiones y se pueda resetear por muerte).
                    if (ProfileManager.Instance != null && !string.IsNullOrEmpty(currentHostID))
                    {
                        string targetID = m.target.gameObject.name;
                        ProfileManager.Instance.MarkTargetDiscovered(currentHostID, targetID);
                    }
                }
                else
                {
                    continue;
                }
            }

            m.icon.gameObject.SetActive(true);

            // ─── Posición en el minimapa ──────────────────────────────
            Vector3 vp = minimapCamera.WorldToViewportPoint(m.target.position);
            bool behind   = vp.z < 0f;
            bool offRange = behind || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f;

            Vector2 half = minimapArea.rect.size * 0.5f;
            Vector2 anchored;
            float rotation = 0f;
            Sprite sprite;

            if (!offRange)
            {
                anchored = new Vector2((vp.x - 0.5f) * minimapArea.rect.width,
                                       (vp.y - 0.5f) * minimapArea.rect.height);
                sprite = _circleSprite;
            }
            else
            {
                Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
                if (behind) dir = -dir;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
                dir.Normalize();

                float maxX = half.x - edgePadding;
                float maxY = half.y - edgePadding;
                float tx = Mathf.Abs(dir.x) > 0.001f ? maxX / Mathf.Abs(dir.x) : float.PositiveInfinity;
                float ty = Mathf.Abs(dir.y) > 0.001f ? maxY / Mathf.Abs(dir.y) : float.PositiveInfinity;
                anchored = dir * Mathf.Min(tx, ty);

                rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                sprite = _triangleSprite;
            }

            m.icon.anchoredPosition = anchored;
            m.icon.localRotation    = Quaternion.Euler(0f, 0f, rotation);
            m.iconImage.sprite      = sprite;
            m.iconImage.color       = m.baseColor;

            // ─── Escala = flash decay + pulso (si es tu hospedera) ────
            float scale = 1f;

            float sinceDiscovery = Time.time - m.discoveredTime;
            if (sinceDiscovery < flashDuration)
            {
                float t = sinceDiscovery / flashDuration;
                scale *= Mathf.Lerp(flashScalePeak, 1f, 1f - (1f - t) * (1f - t));
            }

            if (pulseSpeciesHostPlant
                && m.type == MinimapTargetType.HostPlant
                && !string.IsNullOrEmpty(m.hostPlantID)
                && m.hostPlantID == currentHostID)
            {
                scale *= 1f + Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) * pulseAmplitude;
            }

            m.icon.localScale = Vector3.one * scale;

            // ─── Zona de alerta para depredadores ─────────────────────
            if (m.type == MinimapTargetType.Predator && m.alertRing != null)
            {
                if (!offRange)
                {
                    m.alertRing.gameObject.SetActive(true);
                    m.alertRing.anchoredPosition = anchored;

                    float diameter = pixelsPerMeter > 0f
                        ? predatorAlertRadius * 2f * pixelsPerMeter
                        : iconSize * 3f; // fallback si no es ortográfica
                    m.alertRing.sizeDelta = new Vector2(diameter, diameter);
                    m.alertRingImage.color = predatorAlertColor;
                }
                else
                {
                    // Fuera del minimapa: ocultar el círculo (no tiene posición real)
                    if (m.alertRing.gameObject.activeSelf)
                        m.alertRing.gameObject.SetActive(false);
                }
            }
        }
    }

    // ── Cálculo de escala mundo → píxeles ─────────────────────────────

    private float GetPixelsPerMeter()
    {
        if (minimapCamera == null || minimapArea == null) return 0f;
        if (!minimapCamera.orthographic) return 0f;
        float orthoSize = minimapCamera.orthographicSize;
        if (orthoSize <= 0f) return 0f;
        return minimapArea.rect.height / (orthoSize * 2f);
    }

    // ── Auto-detección y descubrimiento ───────────────────────────────

    private void AutoDetectReferences()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<MariposarioSpawner>();

        if (minimapCamera == null)
        {
            var mc = FindFirstObjectByType<MinimapController>();
            if (mc != null) minimapCamera = mc.GetComponent<Camera>();
        }

        if (minimapArea == null)
        {
            foreach (var raw in FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (raw.texture != null && minimapCamera != null
                    && raw.texture == minimapCamera.targetTexture)
                {
                    minimapArea = raw.rectTransform;
                    break;
                }
            }
        }

        if (minimapArea == null)
            Debug.LogError("[MinimapMarkerSystem] No se encontró el RawImage del minimapa.", this);
        if (minimapCamera == null)
            Debug.LogError("[MinimapMarkerSystem] No se encontró la MinimapCamera.", this);
    }

    private void DiscoverTargets()
    {
        if (minimapArea == null) return;

        foreach (var hp in FindObjectsByType<HostPlant>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hp.GetComponent<MinimapTarget>() != null) continue;
            AddMarker(hp.transform, MinimapTargetType.HostPlant, hp.hostPlantID, hostPlantColor);
        }

        foreach (var mt in FindObjectsByType<MinimapTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Color color = ColorForType(mt.type);
            if (mt.iconColorOverride.a > 0.001f) color = mt.iconColorOverride;

            string id = mt.hostPlantID;
            if (string.IsNullOrEmpty(id) && mt.type == MinimapTargetType.HostPlant)
            {
                var hp = mt.GetComponent<HostPlant>();
                if (hp != null) id = hp.hostPlantID;
            }

            AddMarker(mt.transform, mt.type, id, color);
        }
    }

    private void AddMarker(Transform target, MinimapTargetType type, string hostID, Color color)
    {
        // Zona de alerta (solo Predator) — se crea PRIMERO para que quede detrás
        RectTransform alertRing = null;
        Image alertRingImg = null;
        if (type == MinimapTargetType.Predator)
        {
            var ringGO = new GameObject($"AlertRing_{target.name}");
            ringGO.transform.SetParent(minimapArea, false);
            ringGO.layer = minimapArea.gameObject.layer;

            alertRing = ringGO.AddComponent<RectTransform>();
            alertRing.anchorMin = new Vector2(0.5f, 0.5f);
            alertRing.anchorMax = new Vector2(0.5f, 0.5f);
            alertRing.pivot     = new Vector2(0.5f, 0.5f);
            alertRing.sizeDelta = new Vector2(iconSize * 3f, iconSize * 3f);

            alertRingImg = ringGO.AddComponent<Image>();
            alertRingImg.sprite        = _circleSprite;
            alertRingImg.color         = predatorAlertColor;
            alertRingImg.raycastTarget = false;

            ringGO.SetActive(false);
        }

        // Ícono
        var go = new GameObject($"Marker_{type}_{target.name}");
        go.transform.SetParent(minimapArea, false);
        go.layer = minimapArea.gameObject.layer;

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(iconSize, iconSize);

        var img = go.AddComponent<Image>();
        img.sprite        = _circleSprite;
        img.color         = color;
        img.raycastTarget = false;

        go.SetActive(false); // empieza oculto

        _markers.Add(new MarkerEntry
        {
            target         = target,
            type           = type,
            hostPlantID    = hostID,
            icon           = rt,
            iconImage      = img,
            alertRing      = alertRing,
            alertRingImage = alertRingImg,
            baseColor      = color,
            discovered     = false,
        });
    }

    private Color ColorForType(MinimapTargetType t)
    {
        switch (t)
        {
            case MinimapTargetType.HostPlant: return hostPlantColor;
            case MinimapTargetType.Nectar:    return nectarColor;
            case MinimapTargetType.Predator:  return predatorColor;
        }
        return Color.white;
    }

    private string GetCurrentHostPlantID()
    {
        if (spawner == null || spawner.ActiveButterfly == null) return null;
        var data = spawner.ActiveButterfly.GetData();
        return data != null ? data.hostPlantID : null;
    }

    // ── Sprites procedurales ──────────────────────────────────────────

    private static void BuildSprites()
    {
        if (_circleSprite == null)   _circleSprite   = MakeCircleSprite(64);
        if (_triangleSprite == null) _triangleSprite = MakeTriangleSprite(64);
    }

    private static Sprite MakeCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px  = new Color32[size * size];
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r;
                float dy = y + 0.5f - r;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - dist);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Sprite MakeTriangleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px  = new Color32[size * size];
        float cx = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            float t = y / (float)(size - 1);
            float halfW = (1f - t) * size * 0.5f;
            for (int x = 0; x < size; x++)
            {
                bool inside = Mathf.Abs(x - cx) <= halfW;
                px[y * size + x] = new Color32(255, 255, 255, inside ? (byte)255 : (byte)0);
            }
        }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public void RefreshTargets()
    {
        foreach (var m in _markers)
        {
            if (m.icon != null)      Destroy(m.icon.gameObject);
            if (m.alertRing != null) Destroy(m.alertRing.gameObject);
        }
        _markers.Clear();
        DiscoverTargets();
    }

    /// <summary>
    /// Marca como NO descubiertos todos los targets cuyo hostPlantID coincide
    /// con la especie dada. Lo llama ScoreManager cuando la mariposa muere 3
    /// veces — debe redescubrir todo de esa especie.
    /// </summary>
    public void ResetDiscoveriesForSpecies(string speciesID)
    {
        if (string.IsNullOrEmpty(speciesID)) return;
        foreach (var m in _markers)
        {
            if (m == null) continue;
            // Hospederas/Nectaríferas asociadas a esta especie
            if ((m.type == MinimapTargetType.HostPlant || m.type == MinimapTargetType.Nectar)
                && m.hostPlantID == speciesID)
            {
                m.discovered = false;
                if (m.icon != null)      m.icon.gameObject.SetActive(false);
                if (m.alertRing != null) m.alertRing.gameObject.SetActive(false);
            }
        }
    }
}
