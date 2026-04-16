using UnityEngine;

/// <summary>
/// ScriptableObject que define los datos de una especie de mariposa.
/// Crea una instancia por especie desde: Assets > Create > Mariposario > Butterfly Data
/// </summary>
[CreateAssetMenu(fileName = "NewButterfly", menuName = "Mariposario/Butterfly Data")]
public class ButterflyData : ScriptableObject
{
    [Header("Identidad")]
    public string speciesName = "Mariposa";
    [TextArea(2, 4)]
    public string description = "Una hermosa mariposa.";
    public Sprite icon;               // Icono para el menú de selección
    public GameObject prefabButterfly;         // Prefab 3D de la mariposa en escena
    public GameObject prefabEgg;         // Prefab 3D del huevo en escena
    public GameObject prefabCaterpillar;         // Prefab 3D de la oruga en escena

    [Header("Vuelo")]
    [Range(1f, 20f)] public float flightSpeed = 6f;
    [Range(1f, 20f)] public float turnSpeed = 4f;
    [Range(0f, 3f)] public float bobAmplitude = 0.3f; // oscilación vertical suave
    [Range(0.1f, 5f)] public float bobFrequency = 1.2f;
}