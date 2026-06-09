using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Coloca el joystick automaticamente segun la preferencia "zurdo" (joystick a la derecha)
/// o "diestro" (joystick a la izquierda).
///
/// SETUP:
///   - Adjuntar a cualquier joystick (FixedJoystick) en la escena.
///   - O dejar que se aplique automaticamente: el sistema busca FixedJoysticks en cada
///     escena cargada y aplica la preferencia guardada en PlayerPrefs.
///
/// USO desde SettingsPanel:
///   JoystickPositioner.LeftHanded = true;   // joystick a la derecha
/// </summary>
public class JoystickPositioner : MonoBehaviour
{
    private const string KEY = "joystick_right";

    private static bool _leftHanded = false;
    private static bool _loaded = false;

    public static bool LeftHanded
    {
        get
        {
            if (!_loaded)
            {
                _leftHanded = PlayerPrefs.GetInt(KEY, 0) == 1;
                _loaded = true;
            }
            return _leftHanded;
        }
        set
        {
            _leftHanded = value;
            _loaded = true;
            PlayerPrefs.SetInt(KEY, value ? 1 : 0);
            ApplyToAll();
        }
    }

    private RectTransform _rt;
    private Vector2 _originalAnchorMin;
    private Vector2 _originalAnchorMax;
    private Vector2 _originalPivot;
    private Vector2 _originalAnchoredPos;
    private bool _captured;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        Capture();
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void Capture()
    {
        if (_captured || _rt == null) return;
        _originalAnchorMin   = _rt.anchorMin;
        _originalAnchorMax   = _rt.anchorMax;
        _originalPivot       = _rt.pivot;
        _originalAnchoredPos = _rt.anchoredPosition;
        _captured = true;
    }

    private void Apply()
    {
        if (_rt == null) return;
        Capture();

        // Importante: NO tocamos pivot — la matematica interna del joystick asume
        // pivot centrado, cambiarlo rompe la deteccion de input.
        if (LeftHanded)
        {
            _rt.anchorMin = new Vector2(1f - _originalAnchorMax.x, _originalAnchorMin.y);
            _rt.anchorMax = new Vector2(1f - _originalAnchorMin.x, _originalAnchorMax.y);
            _rt.anchoredPosition = new Vector2(-_originalAnchoredPos.x, _originalAnchoredPos.y);
        }
        else
        {
            _rt.anchorMin = _originalAnchorMin;
            _rt.anchorMax = _originalAnchorMax;
            _rt.anchoredPosition = _originalAnchoredPos;
        }
    }

    private static void ApplyToAll()
    {
        var all = FindObjectsByType<JoystickPositioner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var jp in all) jp.Apply();

        // Tambien busca FixedJoysticks que no tengan JoystickPositioner y les agrega uno
        AutoAttachAll();
    }

    // Auto-attach: agrega el componente a todos los FixedJoysticks y FixedTouchFields
    private static void AutoAttachAll()
    {
        var joysticks = FindObjectsByType<FixedJoystick>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var j in joysticks)
        {
            if (j.GetComponent<JoystickPositioner>() == null)
                j.gameObject.AddComponent<JoystickPositioner>();
        }

        // Tambien al TouchField — al mover el joystick a la derecha, el TouchField
        // (que cubre toda la pantalla excepto la izquierda) debe espejarse para no
        // capturar los toques del joystick.
        var touchFields = FindObjectsByType<FixedTouchField>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tf in touchFields)
        {
            if (tf.GetComponent<JoystickPositioner>() == null)
                tf.gameObject.AddComponent<JoystickPositioner>();
        }
    }

    // Auto-applica al cargar cada escena (incluso si no se modifica el prefab)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AutoAttachAll();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AutoAttachAll();
    }
}
