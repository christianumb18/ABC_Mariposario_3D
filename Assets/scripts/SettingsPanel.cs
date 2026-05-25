using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Panel de configuracion global. Persiste entre escenas via singleton + DontDestroyOnLoad.
/// Funciona desde Menu_3D y durante el juego en MapaMariposario (pausa con Time.timeScale).
///
/// SETUP RAPIDO:
///   1. Adjunta este script al Canvas_Settings.
///   2. Arrastra panelRoot = Panel_Configuracion.
///   3. Conecta cada slider/toggle/dropdown/text desde el Inspector.
///   4. Cualquier boton de la app que abra config debe llamar a
///      SettingsPanel.Instance.Open() (o conectarse a OpenConfiguracion()
///      desde MainMenu como un proxy).
///
/// NOTAS:
///   - Volumen unico: usa AudioListener.volume (no requiere AudioMixer).
///   - Sensibilidad proporcional: un solo slider escala ambos ejes.
///   - Touch/Joystick: ToggleGroup en Unity garantiza exclusividad.
///   - Las propiedades estaticas (SensitivityMultiplier, InvertY) leen de
///     PlayerPrefs aunque el panel no este instanciado en la escena actual.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────
    public static SettingsPanel Instance { get; private set; }

    [Header("Panel principal")]
    public GameObject panelRoot;            // Panel_Configuracion (con fondo oscurecido)

    [Header("Botones")]
    public Button btnClose;                 // Boton X
    public Button btnApply;                 // Boton Aplicar (tambien sirve como cerrar)
    public Button btnResetProgress;         // Opcional, si lo agregas

    [Header("Sonido")]
    public Slider sliderVolume;             // 0 – 1
    public TMP_Text textVolumeValue;
    public Toggle toggleSoundEnabled;       // Mute global

    [Header("Camara")]
    public TMP_Text textSensitivityValue;
    public Slider sliderSensitivity;        // 0.5 – 2 (multiplicador proporcional)
    public Toggle toggleInvertY;
    [Tooltip("Toggles excluyentes. Asegurate de poner ambos en un ToggleGroup en Unity.")]
    public Toggle toggleTouchScreen;
    public Toggle toggleJoystick;

    [Header("Grafica")]
    public TMP_Dropdown dropdownQuality;    // 0=Bajo, 1=Medio, 2=Alto

    [Header("Idioma")]
    public Toggle toggleSpanish;            // Tambien en ToggleGroup
    public Toggle toggleEnglish;

    [Header("Rendimiento (solo lectura)")]
    public TMP_Text textFPS;
    public TMP_Text textBattery;
    public TMP_Text textTemperature;
    public TMP_Text textBatterySaving;
    public TMP_Text textPerformanceState;
    [Tooltip("Cada cuantos segundos se refrescan los datos de rendimiento.")]
    [Range(0.2f, 2f)] public float refreshInterval = 0.5f;

    // ── Claves de PlayerPrefs ──────────────────────────────────────
    private const string KEY_VOLUME = "vol_master";
    private const string KEY_SOUND_ON = "sound_enabled";
    private const string KEY_SENSITIVITY = "cam_sensitivity";
    private const string KEY_INVERT_Y = "cam_invert_y";
    private const string KEY_INPUT_MODE = "input_mode";   // 0 = Touch, 1 = Joystick
    private const string KEY_QUALITY = "gfx_quality";
    private const string KEY_LANGUAGE = "lang";            // 0 = ES, 1 = EN

    // ── Valores base de sensibilidad ───────────────────────────────
    // El slider es un multiplicador (1 = normal, 2 = doble, 0.5 = mitad)
    // que se aplica a estos valores base de yaw y pitch.
    private const float YAW_BASE = 0.08f;
    private const float PITCH_BASE = 0.05f;

    // ── Valores estaticos accesibles desde otros scripts ───────────
    // SensitivityMultiplier: se inicializa de PlayerPrefs en el primer get
    // si nadie la asigno (caso: entrar directo al mariposario sin pasar
    // por el menu, donde el SettingsPanel no esta instanciado).
    private static float _sensitivityMultiplier = -1f;
    public static float SensitivityMultiplier
    {
        get
        {
            if (_sensitivityMultiplier < 0f)
                _sensitivityMultiplier = PlayerPrefs.GetFloat(KEY_SENSITIVITY, 1f);
            return _sensitivityMultiplier;
        }
        private set { _sensitivityMultiplier = value; }
    }

    private static int _invertY = -1;
    public static bool InvertY
    {
        get
        {
            if (_invertY < 0)
                _invertY = PlayerPrefs.GetInt(KEY_INVERT_Y, 0);
            return _invertY == 1;
        }
        private set { _invertY = value ? 1 : 0; }
    }

    public static float YawSensitivity => YAW_BASE * SensitivityMultiplier;
    public static float PitchSensitivity => PITCH_BASE * SensitivityMultiplier;
    public static int InputMode { get; private set; } = 0;   // 0 = Touch, 1 = Joystick
    public static int Language { get; private set; } = 0;
    public static bool SoundEnabled { get; private set; } = true;

    // ── Estado interno para FPS ────────────────────────────────────
    private float _fpsAccum;
    private int _fpsFrames;
    private float _fpsTimer;
    private float _currentFPS;

    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Botones
        if (btnClose != null) btnClose.onClick.AddListener(Close);
        if (btnApply != null) btnApply.onClick.AddListener(Close);
        if (btnResetProgress != null) btnResetProgress.onClick.AddListener(OnResetProgress);

        // Sonido
        if (sliderVolume != null) sliderVolume.onValueChanged.AddListener(SetVolume);
        if (toggleSoundEnabled != null) toggleSoundEnabled.onValueChanged.AddListener(SetSoundEnabled);

        // Camara
        if (sliderSensitivity != null) sliderSensitivity.onValueChanged.AddListener(SetSensitivity);
        if (toggleInvertY != null) toggleInvertY.onValueChanged.AddListener(SetInvertY);
        if (toggleTouchScreen != null) toggleTouchScreen.onValueChanged.AddListener(OnTouchToggle);
        if (toggleJoystick != null) toggleJoystick.onValueChanged.AddListener(OnJoystickToggle);

        // Grafica
        if (dropdownQuality != null) dropdownQuality.onValueChanged.AddListener(SetQuality);

        // Idioma
        if (toggleSpanish != null) toggleSpanish.onValueChanged.AddListener(OnSpanishToggle);
        if (toggleEnglish != null) toggleEnglish.onValueChanged.AddListener(OnEnglishToggle);
    }

    private void Start()
    {
        LoadSettings();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Update()
    {
        // FPS counter siempre activo. Usa unscaledDeltaTime para que no se
        // quede en 0 cuando el panel pausa el juego con Time.timeScale = 0.
        _fpsAccum += 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        _fpsFrames++;
        _fpsTimer += Time.unscaledDeltaTime;

        if (_fpsTimer >= refreshInterval)
        {
            _currentFPS = _fpsAccum / _fpsFrames;
            _fpsAccum = 0f;
            _fpsFrames = 0;
            _fpsTimer = 0f;

            // Solo refresca textos si el panel esta visible (evita work innecesario)
            if (panelRoot != null && panelRoot.activeSelf)
                RefreshPerformanceTexts();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // ABRIR / CERRAR
    // ═══════════════════════════════════════════════════════════════

    public void Open()
    {
        Debug.Log("[SettingsPanel] Open() fue llamado!");

        if (panelRoot == null)
        {
            Debug.LogError("[SettingsPanel] panelRoot ES NULL - no puedo abrir nada.");
            return;
        }

        Debug.Log($"[SettingsPanel] Activando panelRoot: {panelRoot.name}");
        panelRoot.SetActive(true);

        if (SceneManager.GetActiveScene().name != "Menu_3D")
            Time.timeScale = 0f;

        RefreshPerformanceTexts();
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

    // ═══════════════════════════════════════════════════════════════
    // CALLBACKS - SONIDO
    // ═══════════════════════════════════════════════════════════════

    public void SetVolume(float value)
    {
        PlayerPrefs.SetFloat(KEY_VOLUME, value);
        ApplyVolume();
        if (textVolumeValue != null) textVolumeValue.text = (value * 100f).ToString("0.0");
    }

    public void SetSoundEnabled(bool on)
    {
        SoundEnabled = on;
        PlayerPrefs.SetInt(KEY_SOUND_ON, on ? 1 : 0);
        ApplyVolume();
    }

    // Aplica volumen real considerando si el sonido esta habilitado
    private void ApplyVolume()
    {
        float v = sliderVolume != null ? sliderVolume.value : 1f;
        AudioListener.volume = SoundEnabled ? v : 0f;
    }

    // ═══════════════════════════════════════════════════════════════
    // CALLBACKS - CAMARA
    // ═══════════════════════════════════════════════════════════════

    public void SetSensitivity(float value)
    {
        SensitivityMultiplier = value;
        PlayerPrefs.SetFloat(KEY_SENSITIVITY, value);
        if (textSensitivityValue != null) textSensitivityValue.text = value.ToString("0.0");
    }

    public void SetInvertY(bool value)
    {
        InvertY = value;
        PlayerPrefs.SetInt(KEY_INVERT_Y, value ? 1 : 0);
    }

    // Exclusividad Touch / Joystick:
    // Unity ToggleGroup ya impide que se desmarquen ambos. Aqui solo
    // detectamos cual quedo encendido y guardamos el indice.
    private void OnTouchToggle(bool isOn)
    {
        if (isOn)
        {
            InputMode = 0;
            PlayerPrefs.SetInt(KEY_INPUT_MODE, 0);
        }
    }

    private void OnJoystickToggle(bool isOn)
    {
        if (isOn)
        {
            InputMode = 1;
            PlayerPrefs.SetInt(KEY_INPUT_MODE, 1);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // CALLBACKS - GRAFICA
    // ═══════════════════════════════════════════════════════════════

    public void SetQuality(int level)
    {
        // QualitySettings tiene los niveles definidos en Project Settings > Quality.
        // 0 = Bajo, 1 = Medio, 2 = Alto (segun el orden de los presets de Unity).
        // applyExpensiveChanges = true regenera shaders/texturas si hace falta.
        QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);
        PlayerPrefs.SetInt(KEY_QUALITY, level);
    }

    // ═══════════════════════════════════════════════════════════════
    // CALLBACKS - IDIOMA
    // ═══════════════════════════════════════════════════════════════

    private void OnSpanishToggle(bool isOn)
    {
        if (isOn) ApplyLanguage(0);
    }

    private void OnEnglishToggle(bool isOn)
    {
        if (isOn) ApplyLanguage(1);
    }

    private void ApplyLanguage(int index)
    {
        Language = index;
        PlayerPrefs.SetInt(KEY_LANGUAGE, index);

        // TODO: cuando instales Unity Localization (Package Manager):
        //   var locales = LocalizationSettings.AvailableLocales.Locales;
        //   LocalizationSettings.SelectedLocale = locales[index];
        Debug.Log($"[SettingsPanel] Idioma: {(index == 0 ? "Espanol" : "English")}");
    }

    // ═══════════════════════════════════════════════════════════════
    // RESET DE PROGRESO
    // ═══════════════════════════════════════════════════════════════

    private void OnResetProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[SettingsPanel] Progreso reiniciado.");
        LoadSettings();
    }

    // ═══════════════════════════════════════════════════════════════
    // PERSISTENCIA
    // ═══════════════════════════════════════════════════════════════

    private void LoadSettings()
    {
        // Sonido
        float vol = PlayerPrefs.GetFloat(KEY_VOLUME, 1f);
        SoundEnabled = PlayerPrefs.GetInt(KEY_SOUND_ON, 1) == 1;
        if (sliderVolume != null) sliderVolume.value = vol;
        if (textVolumeValue != null) textVolumeValue.text = (vol * 100f).ToString("0.0");
        if (toggleSoundEnabled != null) toggleSoundEnabled.isOn = SoundEnabled;
        ApplyVolume();

        // Camara
        SensitivityMultiplier = PlayerPrefs.GetFloat(KEY_SENSITIVITY, 1f);
        InvertY = PlayerPrefs.GetInt(KEY_INVERT_Y, 0) == 1;
        InputMode = PlayerPrefs.GetInt(KEY_INPUT_MODE, 0);   // default Touch
        if (sliderSensitivity != null) sliderSensitivity.value = SensitivityMultiplier;
        if (textSensitivityValue != null) textSensitivityValue.text = SensitivityMultiplier.ToString("0.0");
        if (toggleInvertY != null) toggleInvertY.isOn = InvertY;
        if (toggleTouchScreen != null) toggleTouchScreen.isOn = (InputMode == 0);
        if (toggleJoystick != null) toggleJoystick.isOn = (InputMode == 1);

        // Grafica - default ALTA (indice 2)
        int q = PlayerPrefs.GetInt(KEY_QUALITY, 2);
        QualitySettings.SetQualityLevel(q, true);
        if (dropdownQuality != null) dropdownQuality.value = q;

        // Idioma - default ESPANOL (indice 0)
        Language = PlayerPrefs.GetInt(KEY_LANGUAGE, 0);
        if (toggleSpanish != null) toggleSpanish.isOn = (Language == 0);
        if (toggleEnglish != null) toggleEnglish.isOn = (Language == 1);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDIMIENTO - lectura de datos del sistema
    // ═══════════════════════════════════════════════════════════════

    private void RefreshPerformanceTexts()
    {
        // FPS
        if (textFPS != null)
            textFPS.text = $"FPS: {_currentFPS:0}";

        // Bateria
        // SystemInfo.batteryLevel devuelve 0-1 en dispositivos reales,
        // o -1 si no esta disponible (editor o desktop sin bateria).
        if (textBattery != null)
        {
            float bat = SystemInfo.batteryLevel;
            textBattery.text = bat < 0f
                ? "Bateria: N/D"
                : $"Bateria: {Mathf.RoundToInt(bat * 100f)}%";
        }

        // Temperatura
        // Unity NO expone temperatura del CPU/GPU. Requiere plugin nativo
        // de Android. Por ahora siempre N/D.
        if (textTemperature != null)
            textTemperature.text = "Temperatura: N/D";

        // Ahorro de bateria
        // Heuristica: si esta descargandose y nivel < 20%, sugerimos modo
        // ahorro activo. Para deteccion real del modo "Battery Saver" de
        // Android se requiere plugin nativo (PowerManager.isPowerSaveMode).
        if (textBatterySaving != null)
        {
            BatteryStatus status = SystemInfo.batteryStatus;
            float bat = SystemInfo.batteryLevel;
            bool savingLikely = (status == BatteryStatus.Discharging) && bat > 0f && bat < 0.2f;
            textBatterySaving.text = savingLikely
                ? "Ahorro de bateria: Si"
                : "Ahorro de bateria: No";
        }

        // Estado general segun FPS
        if (textPerformanceState != null)
        {
            string state;
            if (_currentFPS >= 50f) state = "Estable";
            else if (_currentFPS >= 30f) state = "Medio consumo";
            else state = "Alto consumo";
            textPerformanceState.text = $"Estado: {state}";
        }
    }
}
