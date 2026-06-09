using System;
using UnityEngine;

/// <summary>
/// Singleton de perfiles MULTI-USUARIO. Persiste todo en PlayerPrefs como un
/// único JSON (clave Profiles_Data_V2). Cada perfil tiene su nombre, tiempo de
/// vuelo, score total y progreso por especie (score, vidas, muertes, video,
/// targets descubiertos).
///
/// MANTIENE la API antigua (UserName, FlightTimeSeconds, Progress,
/// FlightTimeFormatted, ProgressPercent, SetUserName, StartCounting…) para que
/// el ProfilePanel existente siga funcionando.
///
/// AGREGA:
///   - GetAllProfiles(), CreateProfile, DeleteProfile, SwitchProfile
///   - GetSpecies(id), AddScoreToSpecies, LoseLife, MarkVideoSeen, …
///   - Eventos: OnProfileSwitched, OnScoreChanged, OnLifeLost, OnSpeciesReset
///
/// SETUP:
///   - Sigue siendo el mismo: un GameObject "ProfileManager" en la escena de
///     menú con este script. Auto-migra del formato viejo si existe.
/// </summary>
public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }

    // ── Config ────────────────────────────────────────────────────────
    [Header("Configuracion")]
    public bool   countTimeOnStart = true;
    public string defaultUserName  = "User01";
    [Tooltip("Total de especies del juego (usado por Progress/ProgressPercent).")]
    public int    totalSpecies     = 8;

    // ── Constantes ────────────────────────────────────────────────────
    private const string KEY_STORE     = "Profiles_Data_V2";
    private const string KEY_OLD_NAME  = "Profile_UserName";       // legacy V1
    private const string KEY_OLD_TIME  = "Profile_FlightTimeSeconds";
    private const string KEY_OLD_PROG  = "Profile_Progress";
    private const float  SAVE_INTERVAL = 5f;

    // ── Estado ────────────────────────────────────────────────────────
    private ProfilesStore _store = new();
    private bool          _countingActive;
    private float         _saveTimer;

    // ── Eventos ───────────────────────────────────────────────────────
    public event Action OnNameChanged;
    public event Action OnProgressChanged;
    public event Action OnProfileSwitched;
    public event Action<string,int> OnScoreChanged;   // (speciesID, newScore)
    public event Action<string,int> OnLifeLost;       // (speciesID, livesRemaining)
    public event Action<string>     OnSpeciesReset;   // (speciesID)

    // ═════════════════════════════════════════════════════════════════
    // API ANTIGUA — el ProfilePanel la sigue usando
    // ═════════════════════════════════════════════════════════════════

    public string UserName          => Active != null ? Active.userName : "";
    public float  FlightTimeSeconds => Active != null ? Active.flightTimeSeconds : 0f;
    public int    Progress          => Active != null ? Active.CountSpeciesWithVideoSeen() : 0;
    public int    TotalSpecies      => totalSpecies;

    public string FlightTimeFormatted
    {
        get
        {
            TimeSpan ts = TimeSpan.FromSeconds(FlightTimeSeconds);
            int hours = (int)ts.TotalHours;
            return $"{hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }

    public float ProgressPercent =>
        totalSpecies <= 0 ? 0f : (Progress / (float)totalSpecies) * 100f;

    // ═════════════════════════════════════════════════════════════════
    // CICLO DE VIDA
    // ═════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromDisk();
    }

    private void Start()
    {
        if (countTimeOnStart) StartCounting();
    }

    private void Update()
    {
        if (!_countingActive || Active == null) return;

        Active.flightTimeSeconds += Time.unscaledDeltaTime;

        _saveTimer += Time.unscaledDeltaTime;
        if (_saveTimer >= SAVE_INTERVAL)
        {
            _saveTimer = 0f;
            SaveToDisk();
        }
    }

    private void OnApplicationPause(bool pause) { if (pause) SaveToDisk(); }
    private void OnApplicationQuit()            { SaveToDisk(); }

    // ═════════════════════════════════════════════════════════════════
    // CARGA / GUARDADO
    // ═════════════════════════════════════════════════════════════════

    private void LoadFromDisk()
    {
        string json = PlayerPrefs.GetString(KEY_STORE, "");
        if (!string.IsNullOrEmpty(json))
        {
            try { _store = JsonUtility.FromJson<ProfilesStore>(json) ?? new ProfilesStore(); }
            catch { _store = new ProfilesStore(); }
        }
        else
        {
            _store = new ProfilesStore();
            // Migración del formato V1 (nombre + tiempo + progreso global)
            if (PlayerPrefs.HasKey(KEY_OLD_NAME) || PlayerPrefs.HasKey(KEY_OLD_TIME))
            {
                var legacy = new ProfileData(PlayerPrefs.GetString(KEY_OLD_NAME, defaultUserName));
                legacy.flightTimeSeconds = PlayerPrefs.GetFloat(KEY_OLD_TIME, 0f);
                _store.profiles.Add(legacy);
                _store.activeProfileName = legacy.userName;
            }
        }

        // Si no hay ningún perfil aún, crea uno por defecto
        if (_store.profiles.Count == 0)
        {
            var p = new ProfileData(defaultUserName);
            _store.profiles.Add(p);
            _store.activeProfileName = defaultUserName;
            SaveToDisk();
        }

        // Si activeProfileName apunta a un perfil que no existe, repara
        if (_store.ActiveProfile == null && _store.profiles.Count > 0)
            _store.activeProfileName = _store.profiles[0].userName;
    }

    public void SaveToDisk()
    {
        if (Active != null) Active.RecomputeTotalScore();
        string json = JsonUtility.ToJson(_store);
        PlayerPrefs.SetString(KEY_STORE, json);
        PlayerPrefs.Save();
    }

    public ProfileData Active => _store?.ActiveProfile;

    // ═════════════════════════════════════════════════════════════════
    // MULTI-PERFIL
    // ═════════════════════════════════════════════════════════════════

    public System.Collections.Generic.List<ProfileData> GetAllProfiles() => _store.profiles;

    public bool CreateProfile(string name)
    {
        name = (name ?? "").Trim();
        if (string.IsNullOrEmpty(name)) return false;
        if (_store.FindByName(name) != null) return false;
        _store.profiles.Add(new ProfileData(name));
        _store.activeProfileName = name;
        SaveToDisk();
        OnProfileSwitched?.Invoke();
        OnNameChanged?.Invoke();
        OnProgressChanged?.Invoke();
        return true;
    }

    public bool SwitchProfile(string name)
    {
        var p = _store.FindByName(name);
        if (p == null) return false;
        _store.activeProfileName = name;
        SaveToDisk();
        OnProfileSwitched?.Invoke();
        OnNameChanged?.Invoke();
        OnProgressChanged?.Invoke();
        return true;
    }

    public bool DeleteProfile(string name)
    {
        var p = _store.FindByName(name);
        if (p == null) return false;
        return DeleteProfile(p);
    }

    /// <summary>Borra por referencia. Funciona aunque el perfil no tenga nombre.</summary>
    public bool DeleteProfile(ProfileData profile)
    {
        if (profile == null) return false;
        if (!_store.profiles.Contains(profile)) return false;

        bool wasActive = (_store.activeProfileName == profile.userName);
        _store.profiles.Remove(profile);
        if (wasActive)
            _store.activeProfileName = _store.profiles.Count > 0 ? _store.profiles[0].userName : null;
        SaveToDisk();
        OnProfileSwitched?.Invoke();
        OnNameChanged?.Invoke();
        OnProgressChanged?.Invoke();
        return true;
    }

    /// <summary>Borra por índice. Útil cuando hay perfiles con el mismo nombre.</summary>
    public bool DeleteProfileAt(int index)
    {
        var all = GetAllProfiles();
        if (index < 0 || index >= all.Count) return false;
        return DeleteProfile(all[index]);
    }

    // ═════════════════════════════════════════════════════════════════
    // API ANTIGUA (mantener compatibilidad con ProfilePanel)
    // ═════════════════════════════════════════════════════════════════

    public void SetUserName(string newName)
    {
        if (Active == null) return;
        newName = newName ?? string.Empty;

        // Si cambian el nombre del perfil activo, hay que actualizar activeProfileName
        string old = Active.userName;
        Active.userName = newName;
        if (_store.activeProfileName == old) _store.activeProfileName = newName;
        SaveToDisk();
        OnNameChanged?.Invoke();
    }

    public void ClearUserName() => SetUserName(string.Empty);

    public void StartCounting() { _countingActive = true; _saveTimer = 0f; }
    public void StopCounting()  { if (!_countingActive) return; _countingActive = false; SaveToDisk(); }

    public void ResetFlightTime()
    {
        if (Active != null) Active.flightTimeSeconds = 0f;
        SaveToDisk();
    }

    /// <summary>Setter legacy de progreso — recalcula con base en CountSpeciesWithVideoSeen, sin efecto directo.</summary>
    public void SetProgress(int discovered) { OnProgressChanged?.Invoke(); }
    public void IncrementProgress()          { OnProgressChanged?.Invoke(); }
    public void ResetProgress()              { /* reset por especie está abajo */ OnProgressChanged?.Invoke(); }

    public void ResetStats()
    {
        if (Active == null) return;
        Active.flightTimeSeconds = 0f;
        Active.totalScore = 0;
        Active.species.Clear();
        SaveToDisk();
        OnProgressChanged?.Invoke();
        OnScoreChanged?.Invoke(null, 0);
    }

    // ═════════════════════════════════════════════════════════════════
    // API NUEVA — por especie
    // ═════════════════════════════════════════════════════════════════

    public SpeciesProgress GetSpecies(string speciesID)
    {
        return Active?.GetOrCreate(speciesID);
    }

    public void MarkVideoSeen(string speciesID, int pointsIfFirstTime)
    {
        if (Active == null || string.IsNullOrEmpty(speciesID)) return;
        var sp = Active.GetOrCreate(speciesID);
        if (sp.videoSeen) return;
        sp.videoSeen = true;
        AddScoreToSpecies(speciesID, pointsIfFirstTime);
        OnProgressChanged?.Invoke();
    }

    public void AddScoreToSpecies(string speciesID, int delta)
    {
        if (Active == null || string.IsNullOrEmpty(speciesID) || delta == 0) return;
        var sp = Active.GetOrCreate(speciesID);
        sp.score = Mathf.Max(0, sp.score + delta);
        if (sp.score > sp.highScore) sp.highScore = sp.score;
        Active.RecomputeTotalScore();
        SaveToDisk();
        OnScoreChanged?.Invoke(speciesID, sp.score);
    }

    public void MarkTargetDiscovered(string speciesID, string targetID)
    {
        if (Active == null || string.IsNullOrEmpty(speciesID) || string.IsNullOrEmpty(targetID)) return;
        var sp = Active.GetOrCreate(speciesID);
        if (!sp.discoveredTargets.Contains(targetID))
        {
            sp.discoveredTargets.Add(targetID);
            SaveToDisk();
        }
    }

    public bool WasTargetDiscovered(string speciesID, string targetID)
    {
        var sp = GetSpecies(speciesID);
        return sp != null && sp.discoveredTargets.Contains(targetID);
    }

    /// <summary>Resta 1 vida. Devuelve true si llegó a 0 (provocó reset).</summary>
    public bool LoseLife(string speciesID)
    {
        if (Active == null || string.IsNullOrEmpty(speciesID)) return false;
        var sp = Active.GetOrCreate(speciesID);
        sp.lives = Mathf.Max(0, sp.lives - 1);
        sp.totalDeaths += 1;
        OnLifeLost?.Invoke(speciesID, sp.lives);

        if (sp.lives <= 0)
        {
            ResetSpeciesProgress(speciesID);
            return true;
        }
        SaveToDisk();
        return false;
    }

    /// <summary>Suma 1 vida hasta un máximo de 6. Devuelve true si efectivamente subió.</summary>
    public bool GainLife(string speciesID)
    {
        if (Active == null || string.IsNullOrEmpty(speciesID)) return false;
        var sp = Active.GetOrCreate(speciesID);
        if (sp.lives >= 3) return false;            // ya está al tope, no hace nada
        sp.lives = Mathf.Min(3, sp.lives + 1);
        OnLifeLost?.Invoke(speciesID, sp.lives);    // reusa el evento para refrescar el HUD
        SaveToDisk();
        return true;
    }

    /// <summary>Pone score=0, videoSeen=false, discoveredTargets vacío, lives=3.
    /// El highScore y totalDeaths NO se borran.</summary>
    public void ResetSpeciesProgress(string speciesID)
    {
        if (Active == null) return;
        var sp = Active.GetOrCreate(speciesID);
        sp.score = 0;
        sp.videoSeen = false;
        sp.discoveredTargets.Clear();
        sp.lives = 3;
        Active.RecomputeTotalScore();
        SaveToDisk();
        OnSpeciesReset?.Invoke(speciesID);
        OnScoreChanged?.Invoke(speciesID, 0);
        OnProgressChanged?.Invoke();
    }

    // ═════════════════════════════════════════════════════════════════
    // ANÁLISIS — fortalezas/fallas
    // ═════════════════════════════════════════════════════════════════

    /// <summary>Especie con más score actual.</summary>
    public SpeciesProgress GetStrongest()
    {
        if (Active == null || Active.species.Count == 0) return null;
        SpeciesProgress best = null;
        for (int i = 0; i < Active.species.Count; i++)
            if (best == null || Active.species[i].score > best.score) best = Active.species[i];
        return best != null && best.score > 0 ? best : null;
    }

    /// <summary>Especie con más muertes totales.</summary>
    public SpeciesProgress GetMostChallenging()
    {
        if (Active == null || Active.species.Count == 0) return null;
        SpeciesProgress worst = null;
        for (int i = 0; i < Active.species.Count; i++)
            if (worst == null || Active.species[i].totalDeaths > worst.totalDeaths) worst = Active.species[i];
        return worst != null && worst.totalDeaths > 0 ? worst : null;
    }
}
