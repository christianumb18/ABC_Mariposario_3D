using System;
using UnityEngine;

/// <summary>
/// Singleton de perfil. Persiste entre escenas y guarda en PlayerPrefs:
///   - Nombre del usuario
///   - Tiempo total de vuelo acumulado (segundos)
///   - Progreso del juego (especies descubiertas)
///
/// El cronometro de vuelo corre SIEMPRE que esta instancia exista,
/// no solo cuando el panel de perfil esta abierto. Asi el tiempo
/// se acumula mientras el jugador esta jugando.
///
/// SETUP:
///   1. Crea un GameObject vacio "ProfileManager" en la escena del menu.
///   2. Adjunta este script.
///   3. Marca "Count Time On Start" si quieres que el cronometro arranque
///      automaticamente al abrir el juego. Si solo quieres contar dentro
///      del mariposario, dejalo desmarcado y llama StartCounting() /
///      StopCounting() desde donde corresponda.
///
/// El ProfilePanel solo LEE de aqui y llama a las funciones publicas.
/// </summary>
public class ProfileManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────
    public static ProfileManager Instance { get; private set; }

    // ── Configuracion ──────────────────────────────────────────────
    [Header("Configuracion")]
    [Tooltip("Si esta marcado, el cronometro de vuelo arranca apenas " +
             "se crea el ProfileManager. Si lo desmarcas, debes llamar " +
             "StartCounting() manualmente (por ejemplo al entrar al mariposario).")]
    public bool countTimeOnStart = true;

    [Tooltip("Nombre por defecto cuando no hay nada guardado todavia.")]
    public string defaultUserName = "User01";

    [Tooltip("Total de especies del juego. Cambialo cuando agregues mas.")]
    public int totalSpecies = 9;

    // ── Claves de PlayerPrefs ──────────────────────────────────────
    // Centralizadas como const para evitar typos y poder cambiarlas en un solo sitio.
    private const string KEY_NAME = "Profile_UserName";
    private const string KEY_FLIGHT_TIME = "Profile_FlightTimeSeconds";
    private const string KEY_PROGRESS = "Profile_Progress";

    // ── Estado en memoria ──────────────────────────────────────────
    // Se mantiene en RAM y se sincroniza con PlayerPrefs cuando hace falta,
    // para no escribir a disco cada frame (PlayerPrefs.SetX es caro en movil).
    private string _userName;
    private float _flightTimeSeconds;
    private int _progress;
    private bool _countingActive;

    // Cada cuanto persistimos el tiempo a disco mientras corre el cronometro.
    // 5s es suficiente para no perder progreso si el jugador cierra la app
    // de golpe, sin penalizar el rendimiento.
    private const float SAVE_INTERVAL = 5f;
    private float _saveTimer;

    // ── Eventos para que la UI se entere de cambios ────────────────
    // El ProfilePanel se suscribe en su OnEnable y refresca los textos
    // sin tener que hacer polling en Update.
    public event Action OnNameChanged;
    public event Action OnProgressChanged;
    // OnFlightTimeChanged no se dispara cada frame: el panel lee el getter
    // directamente cuando esta abierto. Disparar un evento por segundo es ruido.

    // ═══════════════════════════════════════════════════════════════
    // PROPIEDADES PUBLICAS
    // ═══════════════════════════════════════════════════════════════

    public string UserName => _userName;
    public float FlightTimeSeconds => _flightTimeSeconds;
    public int Progress => _progress;
    public int TotalSpecies => totalSpecies;

    /// <summary>Devuelve el tiempo formateado como HH:MM:SS.</summary>
    public string FlightTimeFormatted
    {
        get
        {
            TimeSpan ts = TimeSpan.FromSeconds(_flightTimeSeconds);
            // Total de horas (no solo 0-23) por si alguien juega 30 horas.
            int hours = (int)ts.TotalHours;
            return $"{hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }

    /// <summary>Porcentaje 0-100 segun progreso/totalSpecies.</summary>
    public float ProgressPercent
    {
        get
        {
            if (totalSpecies <= 0) return 0f;
            return (_progress / (float)totalSpecies) * 100f;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // CICLO DE VIDA
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Patron singleton (mismo que MariposarioGameManager)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadFromDisk();
    }

    private void Start()
    {
        if (countTimeOnStart)
            StartCounting();
    }

    private void Update()
    {
        if (!_countingActive) return;

        _flightTimeSeconds += Time.unscaledDeltaTime;
        // unscaledDeltaTime: cuenta tiempo real, no afectado por Time.timeScale.
        // Si pausas el juego con timeScale=0, el cronometro igual sigue:
        // cambia a Time.deltaTime si prefieres que se detenga en pausa.

        _saveTimer += Time.unscaledDeltaTime;
        if (_saveTimer >= SAVE_INTERVAL)
        {
            _saveTimer = 0f;
            SaveFlightTime();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        // En movil, OnApplicationPause(true) se dispara al mandar la app
        // a segundo plano. Guardamos para no perder los segundos del intervalo.
        if (pause) SaveFlightTime();
    }

    private void OnApplicationQuit()
    {
        SaveFlightTime();
    }

    // ═══════════════════════════════════════════════════════════════
    // CARGA / GUARDADO
    // ═══════════════════════════════════════════════════════════════

    private void LoadFromDisk()
    {
        _userName = PlayerPrefs.GetString(KEY_NAME, defaultUserName);
        _flightTimeSeconds = PlayerPrefs.GetFloat(KEY_FLIGHT_TIME, 0f);
        _progress = PlayerPrefs.GetInt(KEY_PROGRESS, 0);
    }

    private void SaveFlightTime()
    {
        PlayerPrefs.SetFloat(KEY_FLIGHT_TIME, _flightTimeSeconds);
        PlayerPrefs.Save();
    }

    // ═══════════════════════════════════════════════════════════════
    // API PUBLICA — NOMBRE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Cambia el nombre. Si pasas "" o null, queda como cadena vacia
    /// (el boton Borrar usa esto). NO usa el default en ese caso:
    /// la decision del usuario manda.
    /// </summary>
    public void SetUserName(string newName)
    {
        _userName = newName ?? string.Empty;
        PlayerPrefs.SetString(KEY_NAME, _userName);
        PlayerPrefs.Save();
        OnNameChanged?.Invoke();
    }

    /// <summary>Vacia el nombre. Equivalente a SetUserName("").</summary>
    public void ClearUserName()
    {
        SetUserName(string.Empty);
    }

    // ═══════════════════════════════════════════════════════════════
    // API PUBLICA — TIEMPO DE VUELO
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Arranca el cronometro. Idempotente.</summary>
    public void StartCounting()
    {
        _countingActive = true;
        _saveTimer = 0f;
    }

    /// <summary>Pausa el cronometro y guarda. Idempotente.</summary>
    public void StopCounting()
    {
        if (!_countingActive) return;
        _countingActive = false;
        SaveFlightTime();
    }

    /// <summary>Resetea solo el tiempo de vuelo. El nombre y el progreso quedan intactos.</summary>
    public void ResetFlightTime()
    {
        _flightTimeSeconds = 0f;
        SaveFlightTime();
    }

    // ═══════════════════════════════════════════════════════════════
    // API PUBLICA — PROGRESO
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Fija un valor absoluto de progreso (0..totalSpecies).</summary>
    public void SetProgress(int discovered)
    {
        _progress = Mathf.Clamp(discovered, 0, totalSpecies);
        PlayerPrefs.SetInt(KEY_PROGRESS, _progress);
        PlayerPrefs.Save();
        OnProgressChanged?.Invoke();
    }

    /// <summary>Suma 1 al progreso (para cuando se descubra una especie nueva).</summary>
    public void IncrementProgress()
    {
        SetProgress(_progress + 1);
    }

    /// <summary>Resetea el progreso a 0. Usado por el boton Resetear.</summary>
    public void ResetProgress()
    {
        SetProgress(0);
    }

    // ═══════════════════════════════════════════════════════════════
    // API PUBLICA — ACCIONES COMBINADAS DEL PANEL DE PERFIL
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Resetea todos los stats EXCEPTO el nombre.
    /// Lo llama el boton "Resetear" del panel de perfil.
    /// </summary>
    public void ResetStats()
    {
        ResetFlightTime();
        ResetProgress();
    }
}
