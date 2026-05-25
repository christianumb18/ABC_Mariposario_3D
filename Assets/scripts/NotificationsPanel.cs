using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de notificaciones (version simple: vive solo en la escena actual).
///
/// Cada item se muestra asi:
///
///   ┌─────────────────────────────────────────┐
///   │ 23/05/2026                              │  ← fila 1: fecha (izquierda)
///   │ Mariposa Colobura disponible!           │  ← fila 2: mensaje
///   │                              10:42 a.m. │  ← fila 3: hora (derecha)
///   ├─────────────────────────────────────────┤  ← linea separadora
///   │ ...                                     │
///   └─────────────────────────────────────────┘
///
/// SETUP en la escena:
///   1. Canvas_Notifications con este script adjunto.
///   2. Panel_Notificaciones (GameObject UI con Image de fondo).
///   3. ScrollView con Content (Vertical Layout Group + Content Size Fitter Vertical).
///      IMPORTANTE: en el ScrollView, desmarca "Horizontal" para que solo
///      haga scroll vertical.
///      En el Content (segun tu imagen ya lo tienes): Vertical Layout Group con
///      "Control Child Size > Width" marcado y "Child Force Expand > Width" marcado,
///      y Content Size Fitter con "Vertical Fit = Preferred Size".
///   4. Prefab "NotificationItem": basta con un GameObject con
///      RectTransform + LayoutElement. El script crea los textos por dentro.
///   5. Conecta panelRoot, content, notificationItemPrefab en el Inspector.
///   6. Boton del HUD → OnClick → NotificationsPanel.Toggle()
/// </summary>
public class NotificationsPanel : MonoBehaviour
{
    [Header("Panel principal")]
    public GameObject panelRoot;                  // Panel_Notificaciones
    [Tooltip("Boton X (opcional). Si no usas boton de cerrar, dejalo vacio.")]
    public Button btnClose;

    [Header("Lista de notificaciones")]
    [Tooltip("El Content del ScrollView (donde se instancian los items).")]
    public Transform content;
    [Tooltip("Prefab del item. Puede ser un GameObject vacio con RectTransform + LayoutElement; el script genera los textos por dentro.")]
    public GameObject notificationItemPrefab;

    [Header("Estilo de los items")]
    [Tooltip("Padding interno horizontal del item (izquierda y derecha) en pixeles.")]
    public int itemPaddingHorizontal = 12;
    [Tooltip("Padding interno vertical del item (arriba y abajo) en pixeles.")]
    public int itemPaddingVertical = 10;
    [Tooltip("Espacio entre fecha, mensaje y hora dentro de un item.")]
    public float itemInnerSpacing = 4f;
    [Tooltip("Color del texto de la fecha.")]
    public Color dateColor = new Color(0.45f, 0.45f, 0.45f);
    [Tooltip("Color del texto del mensaje.")]
    public Color messageColor = Color.black;
    [Tooltip("Color del texto de la hora.")]
    public Color timeColor = new Color(0.45f, 0.45f, 0.45f);
    [Tooltip("Color de la linea separadora.")]
    public Color separatorColor = new Color(0.8f, 0.8f, 0.8f);
    [Tooltip("Tamano de fuente del mensaje.")]
    public float messageFontSize = 18f;
    [Tooltip("Tamano de fuente de fecha y hora.")]
    public float metaFontSize = 12f;

    [Header("Badge")]
    [Tooltip("GameObject del badge en el HUD. Se muestra solo si hay no leidas.")]
    public GameObject badgeRoot;
    [Tooltip("Texto del numero de no leidas dentro del badge.")]
    public TMP_Text badgeText;

    [Header("Notificaciones de prueba")]
    public bool loadTestNotificationsOnStart = true;
    [TextArea(1, 2)]
    public List<string> testNotifications = new()
    {
        "Mariposa Colobura disponible!",
        "Nueva planta hospedera descubierta",
        "Has completado tu primer vuelo"
    };

    // ── Estado interno ─────────────────────────────────────────────
    private readonly List<NotificationData> _notifications = new();
    private int _unreadCount = 0;

    // Cada notificacion guarda titulo y momento en que llego
    private class NotificationData
    {
        public string title;
        public DateTime timestamp;
        public bool read;
        public NotificationData(string t) { title = t; timestamp = DateTime.Now; read = false; }
    }

    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (btnClose != null) btnClose.onClick.AddListener(Close);
    }

    private void Start()
    {
        if (loadTestNotificationsOnStart)
        {
            // Crea notificaciones de prueba con timestamps espaciados para que se vean distintos
            DateTime now = DateTime.Now;
            for (int i = 0; i < testNotifications.Count; i++)
            {
                var data = new NotificationData(testNotifications[i]);
                data.timestamp = now.AddMinutes(-i * 17);   // cada una unos minutos antes
                _notifications.Insert(0, data);
                _unreadCount++;
            }
            RefreshBadge();
        }

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════
    // ABRIR / CERRAR
    // ═══════════════════════════════════════════════════════════════

    public void Open()
    {
        if (panelRoot == null)
        {
            Debug.LogError("[NotificationsPanel] panelRoot ES NULL.");
            return;
        }

        panelRoot.SetActive(true);
        RebuildList();
        MarkAllAsRead();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (panelRoot == null) return;
        if (panelRoot.activeSelf) Close();
        else Open();
    }

    // ═══════════════════════════════════════════════════════════════
    // API PUBLICA
    // ═══════════════════════════════════════════════════════════════

    public void AddNotification(string title)
    {
        _notifications.Insert(0, new NotificationData(title));
        _unreadCount++;
        RefreshBadge();

        if (panelRoot != null && panelRoot.activeSelf)
            RebuildList();
    }

    public void ClearAll()
    {
        _notifications.Clear();
        _unreadCount = 0;
        RefreshBadge();
        RebuildList();
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDERIZADO DE LA LISTA
    // ═══════════════════════════════════════════════════════════════

    private void RebuildList()
    {
        if (content == null || notificationItemPrefab == null)
        {
            Debug.LogWarning("[NotificationsPanel] content o notificationItemPrefab no asignados.");
            return;
        }

        // Limpia items previos
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        // Crea un item por cada notificacion
        for (int i = 0; i < _notifications.Count; i++)
        {
            var data = _notifications[i];
            GameObject item = Instantiate(notificationItemPrefab, content);
            BuildItemContents(item, data);

            // Agrega separador despues de cada item excepto el ultimo
            if (i < _notifications.Count - 1)
                CreateSeparator(content);
        }
    }

    // ───────────────────────────────────────────────────────────────
    // CONSTRUYE EL CONTENIDO DE UN ITEM
    // ───────────────────────────────────────────────────────────────
    //
    // CLAVE DE LA SOLUCION:
    // En vez de posicionar fecha/mensaje/hora con anchors absolutos
    // (lo que hacia que se superpongan cuando el item era bajo),
    // se le agrega al item un Vertical Layout Group interno + Content Size Fitter,
    // y cada fila vive como hijo dentro de ese layout.
    //
    // Asi el alto del item se ajusta SOLO al contenido y nada se encima.
    //
    private void BuildItemContents(GameObject item, NotificationData data)
    {
        // 1. Limpia hijos previos por si el prefab traia algo
        for (int i = item.transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(item.transform.GetChild(i).gameObject);

        // 2. LayoutElement: solo flexibleWidth para que ocupe todo el ancho del Content
        LayoutElement le = item.GetComponent<LayoutElement>();
        if (le == null) le = item.AddComponent<LayoutElement>();
        le.minHeight = -1;        // que lo decida el contenido
        le.preferredHeight = -1;
        le.flexibleWidth = 1;

        // 3. Vertical Layout Group: apila fecha → mensaje → hora
        VerticalLayoutGroup vlg = item.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = item.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(
            itemPaddingHorizontal, itemPaddingHorizontal,
            itemPaddingVertical, itemPaddingVertical);
        vlg.spacing = itemInnerSpacing;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 4. Content Size Fitter: el item crece solo segun el alto total de sus hijos
        ContentSizeFitter csf = item.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = item.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 5. Crea las tres filas como hijos del Vertical Layout
        CreateRowText(item.transform,
            "Date",
            data.timestamp.ToString("dd/MM/yyyy"),
            dateColor, metaFontSize,
            TextAlignmentOptions.MidlineLeft);

        CreateRowText(item.transform,
            "Message",
            data.title,
            messageColor, messageFontSize,
            TextAlignmentOptions.MidlineLeft);

        CreateRowText(item.transform,
            "Time",
            data.timestamp.ToString("hh:mm tt"),
            timeColor, metaFontSize,
            TextAlignmentOptions.MidlineRight);
    }

    // Crea un TMP_Text como una "fila" del Vertical Layout del item.
    // No fija anchors absolutos: el Vertical Layout Group decide la posicion,
    // y el propio TMP define su altura preferida (auto-wrap incluido).
    private void CreateRowText(Transform parent, string goName, string text,
        Color color, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;

        // LayoutElement para que el Vertical Layout Group sepa cuanto alto pedir.
        // No fijamos altura: TMP reporta su preferredHeight segun el texto.
        LayoutElement rowLE = go.AddComponent<LayoutElement>();
        rowLE.flexibleWidth = 1;
    }

    // Crea una linea horizontal separadora entre items
    private void CreateSeparator(Transform parentContent)
    {
        GameObject sep = new GameObject("Separator", typeof(RectTransform));
        sep.transform.SetParent(parentContent, false);

        Image img = sep.AddComponent<Image>();
        img.color = separatorColor;
        img.raycastTarget = false;

        LayoutElement le = sep.AddComponent<LayoutElement>();
        le.minHeight = 2f;
        le.preferredHeight = 2f;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;
    }

    // ═══════════════════════════════════════════════════════════════
    // BADGE
    // ═══════════════════════════════════════════════════════════════

    private void RefreshBadge()
    {
        if (badgeRoot != null)
            badgeRoot.SetActive(_unreadCount > 0);

        if (badgeText != null)
            badgeText.text = _unreadCount > 9 ? "9+" : _unreadCount.ToString();
    }

    private void MarkAllAsRead()
    {
        foreach (var n in _notifications) n.read = true;
        _unreadCount = 0;
        RefreshBadge();
    }
}
