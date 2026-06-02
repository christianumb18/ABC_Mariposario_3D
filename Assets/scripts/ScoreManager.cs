using UnityEngine;

/// <summary>
/// Lógica de score y vidas dentro de UNA partida. Observa el estado del juego
/// y dispara los eventos relevantes al ProfileManager:
///   - Al entrar a Inspecting → otorga puntos por video (primera vez por especie).
///   - Cada frame: mide distancia entre la mariposa-jugador y los depredadores;
///     si está a ≤ predatorDamageRange metros → registra una muerte (con 2s de
///     invulnerabilidad para no perder 3 vidas en 2 segundos).
///   - Cuando una especie llega a 0 vidas → reset (gestionado por ProfileManager)
///     y se piden a MinimapMarkerSystem que limpie los descubrimientos.
///
/// SETUP:
///   1. Adjunta este script al "InteractionManager" en MapaMariposario_conPlantas.
///   2. (Opcional) Ajusta videoPoints, predatorDamageRange y invulnerabilityTime.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Puntos")]
    [Tooltip("Puntos al ver el video de una especie por primera vez")]
    public int videoPoints = 80;

    [Header("Depredadores")]
    [Tooltip("Distancia (m) a la que un depredador descuenta 1 vida")]
    [Range(0.2f, 5f)] public float predatorDamageRange = 1f;

    [Tooltip("Segundos de invulnerabilidad después de recibir daño")]
    [Range(0.5f, 5f)] public float invulnerabilityTime = 2f;

    [Header("Referencias (auto-detección si están vacías)")]
    public MariposarioSpawner spawner;
    public GameStateManager   gameState;

    // ── Estado interno ────────────────────────────────────────────────
    private GameState _lastState  = GameState.Exploring;
    private float     _invulnUntil = 0f;

    // ═════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (spawner == null)   spawner   = FindFirstObjectByType<MariposarioSpawner>();
        if (gameState == null) gameState = GameStateManager.Instance ?? FindFirstObjectByType<GameStateManager>();
    }

    private void Update()
    {
        TickStateTransition();
        TickPredatorProximity();
    }

    // ── Eventos: ver video de la especie inspeccionada ────────────────

    private void TickStateTransition()
    {
        if (gameState == null) return;
        GameState current = gameState.CurrentState;
        if (current == _lastState) return;

        if (current == GameState.Inspecting)
            TryAwardVideoPoints();

        _lastState = current;
    }

    private void TryAwardVideoPoints()
    {
        if (ProfileManager.Instance == null) return;
        var npc = gameState.SelectedNPC;
        if (npc == null || npc.Data == null) return;
        string speciesID = npc.Data.speciesName;
        if (string.IsNullOrEmpty(speciesID)) return;

        ProfileManager.Instance.MarkVideoSeen(speciesID, videoPoints);
    }

    // ── Eventos: depredador a ≤1m → quitar vida ───────────────────────

    private void TickPredatorProximity()
    {
        if (spawner == null || spawner.ActiveButterfly == null) return;
        if (Time.time < _invulnUntil) return;

        var data = spawner.ActiveButterfly.GetData();
        if (data == null) return;
        string speciesID = data.speciesName;
        if (string.IsNullOrEmpty(speciesID)) return;

        Vector3 pos = spawner.ActiveButterfly.transform.position;
        float dmgSqr = predatorDamageRange * predatorDamageRange;

        var targets = FindObjectsByType<MinimapTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < targets.Length; i++)
        {
            var t = targets[i];
            if (t.type != MinimapTargetType.Predator) continue;

            float dx = t.transform.position.x - pos.x;
            float dy = t.transform.position.y - pos.y;
            float dz = t.transform.position.z - pos.z;
            if (dx * dx + dy * dy + dz * dz <= dmgSqr)
            {
                RegisterPredatorHit(speciesID);
                _invulnUntil = Time.time + invulnerabilityTime;
                return;
            }
        }
    }

    private void RegisterPredatorHit(string speciesID)
    {
        if (ProfileManager.Instance == null) return;
        bool died = ProfileManager.Instance.LoseLife(speciesID);

        if (died)
        {
            // Reset también de los markers descubiertos en el minimapa
            var minimap = FindFirstObjectByType<MinimapMarkerSystem>();
            minimap?.ResetDiscoveriesForSpecies(speciesID);
        }
    }

    // ── API pública para puntos manuales (futuros eventos) ────────────

    public void AwardSpecies(string speciesID, int points)
    {
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.AddScoreToSpecies(speciesID, points);
    }
}
