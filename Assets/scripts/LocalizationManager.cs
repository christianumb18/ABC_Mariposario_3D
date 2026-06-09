using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de localizacion ligero. Singleton + DontDestroyOnLoad.
/// Mantiene diccionarios ES/EN hardcodeados (anadir entradas aqui).
/// Los componentes LocalizedText se suscriben a OnLanguageChanged y
/// se refrescan automaticamente. AutoLocalize escanea cada escena al
/// cargarla y traduce todos los TMP_Text que coincidan con el diccionario.
///
/// USO:
///   LocalizationManager.Instance.SetLanguage(1);   // 0=ES, 1=EN
///   string s = LocalizationManager.Instance.Get("ui.start");
///   string key = LocalizationManager.Instance.FindKey("INICIAR");
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public event Action OnLanguageChanged;

    public int CurrentLanguage { get; private set; } = 0; // 0=ES, 1=EN

    private const string KEY_LANGUAGE = "lang";

    // ── Diccionarios ──────────────────────────────────────────────
    // El AutoLocalize hace match por TEXTO (no por clave) contra estas
    // tablas, asi que el valor debe coincidir EXACTO con lo que aparece
    // en los prefabs/escenas (sin contar trimming).
    private static readonly Dictionary<string, string> ES = new()
    {
        // Menu principal
        { "ui.start",            "INICIAR" },
        { "ui.library",          "Ver biblioteca" },
        { "ui.main_menu",        "Menu \nPrincipal" },
        { "ui.exit",             "Salir" },
        { "ui.profile",          "Perfil" },
        { "ui.apply",            "Aplicar" },
        { "ui.accept",           "Aceptar" },
        { "ui.delete",           "Borrar" },
        { "ui.close",            "Cerrar" },
        { "ui.back",             "Regresar" },
        { "ui.info",             "Info" },
        { "ui.reset",            "Resetear" },
        { "ui.notifications",    "Notificaciones" },

        // Settings - encabezados
        { "settings.sound",      "Sonido" },
        { "settings.camera",     "Cámara" },
        { "settings.graphics",   "Gráficos" },
        { "settings.language",   "Idioma" },
        { "settings.performance","Rendimiento" },

        // Settings - opciones
        { "settings.volume",     "Volumen" },
        { "settings.sensitivity","Sensibilidad" },
        { "settings.invert_y",   "Invertir Y" },
        { "settings.touchscreen","Pantalla tactil" },
        { "settings.joystick",   "Joystick" },
        { "settings.quality",    "Calidad" },
        { "settings.quality.low","Baja" },
        { "settings.quality.med","Media" },
        { "settings.quality.high","Alta" },
        { "settings.yes",        "Si" },
        { "settings.no",         "No" },
        { "settings.spanish",    "Español" },
        { "settings.english",    "Ingles" },
        { "settings.reset_game", "Reestablecer el juego" },
        { "settings.delete_user","Borrar Datos de Usuario" },
        { "settings.left_handed","Modo zurdo" },
        { "settings.accessibility","Accesibilidad" },

        // Perfil
        { "profile.change_name", "Cambia tu nombre de Usuario...." },
        { "profile.choose",      "Elige tu perfil" },
        { "profile.create_new",  "+ Crear nuevo perfil" },
        { "profile.new_name",    "Nombre del nuevo perfil" },
        { "profile.your_name",   "Tu nombre…" },
        { "ui.cancel",           "Cancelar" },

        // Video panel - sin datos
        { "video.no_data",       "Sin datos" },
        { "video.no_data_help",  "Asigna un ButterflySpeciesData en el ButterflyIdentity de este prefab." },

        // Estadisticas
        { "stats.game_progress", "Progeso del Juego: " },
        { "stats.flight_time",   "Tiempo de Vuelo:" },
        { "stats.species_found", "Total especies descubiertas:" },

        // Juego - mensajes
        { "game.host_plant_ok",  "Planta Hospedera Identificada" },
        { "game.host_plant_bad", "Planta Hospedera Incorrecta" },
        { "game.lay_eggs",       "Poner Huevos" },
        { "game.return",         "Volver" },
        { "game.change_character","Cambiar personaje" },

        // Rendimiento (usados en codigo via L())
        { "perf.fps",            "FPS" },
        { "perf.battery",        "Bateria" },
        { "perf.temperature",    "Temperatura" },
        { "perf.battery_saving", "Ahorro de bateria" },
        { "perf.state",          "Estado" },
        { "perf.state.stable",   "Estable" },
        { "perf.state.medium",   "Medio consumo" },
        { "perf.state.high",     "Alto consumo" },
        { "perf.na",             "N/D" },

        // Ciclo de vida
        { "lifecycle.egg",       "Huevo" },
        { "lifecycle.larva",     "Oruga" },
        { "lifecycle.pupa",      "Crisalida" },
        { "lifecycle.butterfly", "Mariposa" },

        // Video / Reproductor
        { "video.back_to_butterfly", "Volver al mariposario" },
        { "video.pause_resume",      "Pausar/Reanudar" },
        { "video.next",              "Siguiente" },
        { "video.previous",          "Atrás" },
        { "video.confirm_return",    "¿Deseas volver al mariposario principal?" },
        { "video.minus_10",          "- 10 Seg" },
        { "video.plus_10",           "+10 Seg" },
        { "video.minus_5",           "- 5 Seg" },
        { "video.plus_5",            "+5 Seg" },

        // HUD juego
        { "hud.points",          "PUNTOS" },

        // Seleccion mariposa
        { "selection.fly_as",    "Volar como {0}" },

        // Profile analysis - estaticos
        { "profile.title",         "Tu progreso" },
        { "profile.total_score",   "Puntaje total" },
        { "profile.flight_time",   "Tiempo de vuelo" },
        { "profile.videos_seen",   "Especies con video visto" },
        { "profile.strength",      "★ Fortaleza" },
        { "profile.challenge",     "⚠ Mayor reto" },
        { "profile.strongest",     "Tu especie más fuerte: {0}  ({1} pts, récord {2})" },
        { "profile.no_data",       "Aún sin datos — explora y suma puntos." },
        { "profile.beaten_singular","{0} te ha vencido {1} vez. Ten cuidado con los depredadores." },
        { "profile.beaten_plural", "{0} te ha vencido {1} veces. Ten cuidado con los depredadores." },
        { "profile.never_beaten",  "No te ha vencido ningún depredador. ¡Buen trabajo!" },
        { "profile.col.species",   "Especie" },
        { "profile.col.score",     "Score" },
        { "profile.col.deaths",    "Muertes" },
        { "profile.col.lives",     "Vidas" },
        { "profile.col.video",     "Video" },
    };

    private static readonly Dictionary<string, string> EN = new()
    {
        { "ui.start",            "START" },
        { "ui.library",          "View Library" },
        { "ui.main_menu",        "Main \nMenu" },
        { "ui.exit",             "Exit" },
        { "ui.profile",          "Profile" },
        { "ui.apply",            "Apply" },
        { "ui.accept",           "Accept" },
        { "ui.delete",           "Delete" },
        { "ui.close",            "Close" },
        { "ui.back",             "Back" },
        { "ui.info",             "Info" },
        { "ui.reset",            "Reset" },
        { "ui.notifications",    "Notifications" },

        { "settings.sound",      "Sound" },
        { "settings.camera",     "Camera" },
        { "settings.graphics",   "Graphics" },
        { "settings.language",   "Language" },
        { "settings.performance","Performance" },

        { "settings.volume",     "Volume" },
        { "settings.sensitivity","Sensitivity" },
        { "settings.invert_y",   "Invert Y" },
        { "settings.touchscreen","TouchScreen" },
        { "settings.joystick",   "Joystick" },
        { "settings.quality",    "Quality" },
        { "settings.quality.low","Low" },
        { "settings.quality.med","Medium" },
        { "settings.quality.high","High" },
        { "settings.yes",        "Yes" },
        { "settings.no",         "No" },
        { "settings.spanish",    "Spanish" },
        { "settings.english",    "English" },
        { "settings.reset_game", "Reset Game" },
        { "settings.delete_user","Delete User Data" },
        { "settings.left_handed","Left-handed mode" },
        { "settings.accessibility","Accessibility" },

        { "profile.change_name", "Change your username...." },
        { "profile.choose",      "Choose your profile" },
        { "profile.create_new",  "+ Create new profile" },
        { "profile.new_name",    "New profile name" },
        { "profile.your_name",   "Your name…" },
        { "ui.cancel",           "Cancel" },

        { "video.no_data",       "No data" },
        { "video.no_data_help",  "Assign a ButterflySpeciesData in the ButterflyIdentity of this prefab." },

        { "stats.game_progress", "Game Progress: " },
        { "stats.flight_time",   "Flight Time:" },
        { "stats.species_found", "Total species discovered:" },

        { "game.host_plant_ok",  "Host Plant Identified" },
        { "game.host_plant_bad", "Wrong Host Plant" },
        { "game.lay_eggs",       "Lay Eggs" },
        { "game.return",         "Back" },
        { "game.change_character","Change character" },

        { "perf.fps",            "FPS" },
        { "perf.battery",        "Battery" },
        { "perf.temperature",    "Temperature" },
        { "perf.battery_saving", "Battery Saving" },
        { "perf.state",          "State" },
        { "perf.state.stable",   "Stable" },
        { "perf.state.medium",   "Medium load" },
        { "perf.state.high",     "High load" },
        { "perf.na",             "N/A" },

        { "lifecycle.egg",       "Egg" },
        { "lifecycle.larva",     "Caterpillar" },
        { "lifecycle.pupa",      "Chrysalis" },
        { "lifecycle.butterfly", "Butterfly" },

        { "video.back_to_butterfly", "Back to butterfly garden" },
        { "video.pause_resume",      "Pause/Resume" },
        { "video.next",              "Next" },
        { "video.previous",          "Back" },
        { "video.confirm_return",    "Return to main butterfly garden?" },
        { "video.minus_10",          "- 10 Sec" },
        { "video.plus_10",           "+10 Sec" },
        { "video.minus_5",           "- 5 Sec" },
        { "video.plus_5",            "+5 Sec" },

        { "hud.points",          "POINTS" },

        { "selection.fly_as",    "Fly as {0}" },

        { "profile.title",         "Your progress" },
        { "profile.total_score",   "Total score" },
        { "profile.flight_time",   "Flight time" },
        { "profile.videos_seen",   "Species with video seen" },
        { "profile.strength",      "★ Strength" },
        { "profile.challenge",     "⚠ Biggest challenge" },
        { "profile.strongest",     "Your strongest species: {0}  ({1} pts, record {2})" },
        { "profile.no_data",       "No data yet — explore and earn points." },
        { "profile.beaten_singular","{0} has beaten you {1} time. Watch out for predators." },
        { "profile.beaten_plural", "{0} has beaten you {1} times. Watch out for predators." },
        { "profile.never_beaten",  "No predator has beaten you. Good job!" },
        { "profile.col.species",   "Species" },
        { "profile.col.score",     "Score" },
        { "profile.col.deaths",    "Deaths" },
        { "profile.col.lives",     "Lives" },
        { "profile.col.video",     "Video" },
    };

    private static readonly Dictionary<string, string>[] Tables = { ES, EN };

    // Reverse lookup: cualquier texto en ES o EN -> clave.
    private static Dictionary<string, string> _reverseLookup;

    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentLanguage = PlayerPrefs.GetInt(KEY_LANGUAGE, 0);
        BuildReverseLookup();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetLanguage(int index)
    {
        index = Mathf.Clamp(index, 0, Tables.Length - 1);
        if (index == CurrentLanguage) return;

        CurrentLanguage = index;
        PlayerPrefs.SetInt(KEY_LANGUAGE, index);
        OnLanguageChanged?.Invoke();
    }

    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        var primary = Tables[CurrentLanguage];
        if (primary.TryGetValue(key, out var value)) return value;
        var fallback = Tables[1 - CurrentLanguage];
        if (fallback.TryGetValue(key, out value)) return value;
        return key;
    }

    /// <summary>Busca la clave de un texto en cualquier idioma. Null si no esta.</summary>
    public string FindKey(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        if (_reverseLookup == null) BuildReverseLookup();
        if (_reverseLookup.TryGetValue(text, out var key)) return key;
        // Match flexible: ignora espacios/saltos al inicio y final
        var trimmed = text.Trim();
        if (trimmed != text && _reverseLookup.TryGetValue(trimmed, out key)) return key;
        return null;
    }

    private static void BuildReverseLookup()
    {
        _reverseLookup = new Dictionary<string, string>();
        foreach (var table in Tables)
        {
            foreach (var kv in table)
            {
                var trimmed = kv.Value.Trim();
                if (!_reverseLookup.ContainsKey(trimmed))
                    _reverseLookup[trimmed] = kv.Key;
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[LocalizationManager]");
        go.AddComponent<LocalizationManager>();
    }
}
