using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner de las 40 mariposas NPC (5 por especie × 8 especies).
/// Usa los ButterflyData existentes de Assets/Characters/ — no necesita nuevos assets.
///
/// SETUP (solo 4 pasos):
///   1. Crea un GameObject vacío "NPC_Manager" en la escena MapaMariposario_conPlantas.
///   2. Adjunta este script.
///   3. En el Inspector, arrastra los 8 assets de Assets/Characters/ al campo "species":
///        Caligo · Colobura · Danaus · Hamadryas · mechantis · Morpho
///        · "Siproeta epaphus" · "Siproeta Stelenes"
///   4. Ajusta boundsCenter y boundsSize para que abarquen el espacio visible del mariposario.
///      (Activa el Gizmo en Scene View para visualizar el volumen con el cubo cian.)
///
///   LAYER OPCIONAL:
///   Si quieres aislamiento de físicas, crea la layer "ButterflyNPC" en
///   Edit > Project Settings > Tags and Layers y desmarca sus colisiones.
///   Si la layer no existe, las NPC funcionan igual pero comparten física con el resto.
/// </summary>
public class ButterflyNPCManager : MonoBehaviour
{
    public const int INSTANCES_PER_SPECIES = 5;
    private const string NPC_LAYER_NAME   = "ButterflyNPC";

    [Header("Especies — arrastra los assets de Assets/Characters/")]
    public List<ButterflyData> species = new();

    [Header("Volumen de vuelo del mariposario")]
    [Tooltip("Centro del espacio donde vuelan las NPC")]
    public Vector3 boundsCenter = new(0f, 4f, 0f);
    [Tooltip("Dimensiones del volumen (ancho, alto, profundidad)")]
    public Vector3 boundsSize   = new(20f, 8f, 20f);

    // ── Pool ──────────────────────────────────────────────────────
    private readonly List<ButterflyNPCBehavior> _pool = new();
    public IReadOnlyList<ButterflyNPCBehavior> AllNPCs => _pool;

    private Transform _npcRoot;

    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        _npcRoot = new GameObject("NPC_Butterflies").transform;
        _npcRoot.SetParent(transform);
    }

    private void Start()
    {
        if (species == null || species.Count == 0)
        {
            Debug.LogError("[ButterflyNPCManager] No hay especies asignadas. " +
                           "Arrastra los ButterflyData de Assets/Characters/ al campo 'species'.", this);
            return;
        }

        ConfigureLayerCollisions();
        SpawnAll();
    }

    // ── Instancia las 40 NPC ──────────────────────────────────────
    private void SpawnAll()
    {
        Bounds bounds    = new Bounds(boundsCenter, boundsSize);
        int totalSpecies = species.Count;

        for (int s = 0; s < totalSpecies; s++)
        {
            ButterflyData data = species[s];

            if (data == null)
            {
                Debug.LogWarning($"[ButterflyNPCManager] Especie [{s}] es null. Se omite.");
                continue;
            }

            if (data.prefabButterfly == null)
            {
                Debug.LogWarning($"[ButterflyNPCManager] '{data.speciesName}' no tiene prefabButterfly asignado. Se omite.");
                continue;
            }

            for (int i = 0; i < INSTANCES_PER_SPECIES; i++)
            {
                Vector3    spawnPos = DistributedPosition(s, i, totalSpecies, bounds);
                Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                GameObject go = Instantiate(data.prefabButterfly, spawnPos, spawnRot, _npcRoot);
                go.name = $"NPC_{data.speciesName}_{i + 1}";

                TryAssignNPCLayer(go);

                var behavior = go.AddComponent<ButterflyNPCBehavior>();
                behavior.Initialize(data, bounds);
                _pool.Add(behavior);
            }
        }

        Debug.Log($"[ButterflyNPCManager] {_pool.Count} mariposas NPC spawneadas.");
    }

    // ── Distribución con jitter para aspecto natural ──────────────
    private static Vector3 DistributedPosition(int speciesIdx, int instanceIdx,
                                                int totalSpecies, Bounds b)
    {
        float zoneW = b.size.x / Mathf.Max(1, totalSpecies);
        float baseX = b.min.x + zoneW * speciesIdx + zoneW * 0.5f;
        float baseZ = b.min.z + b.size.z * ((float)instanceIdx / INSTANCES_PER_SPECIES);

        return new Vector3(
            baseX + Random.Range(-zoneW * 0.4f, zoneW * 0.4f),
            Random.Range(b.min.y + 1f, b.max.y - 1f),
            baseZ + Random.Range(-b.size.z * 0.15f, b.size.z * 0.15f));
    }

    // ── Asigna la layer NPC (si existe) ───────────────────────────
    private static void TryAssignNPCLayer(GameObject go)
    {
        int layer = LayerMask.NameToLayer(NPC_LAYER_NAME);
        if (layer < 0) return;   // layer no existe — se ignora sin error

        foreach (Transform t in go.GetComponentsInChildren<Transform>(includeInactive: true))
            t.gameObject.layer = layer;
    }

    // ── Aislamiento de físicas entre NPC y jugador (si existe la layer) ─
    private static void ConfigureLayerCollisions()
    {
        int npcLayer = LayerMask.NameToLayer(NPC_LAYER_NAME);
        if (npcLayer < 0) return;

        Physics.IgnoreLayerCollision(npcLayer, npcLayer, true);

        int playerLayer = LayerMask.NameToLayer("Butterfly");
        if (playerLayer >= 0)
            Physics.IgnoreLayerCollision(npcLayer, playerLayer, true);
    }

    // ── Gizmo: volumen de vuelo visible en Scene View ─────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.12f);
        Gizmos.DrawCube(boundsCenter, boundsSize);
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireCube(boundsCenter, boundsSize);
    }
}
