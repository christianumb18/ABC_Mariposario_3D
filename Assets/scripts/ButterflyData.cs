using UnityEngine;
using UnityEngine.Video;

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
    public GameObject prefabButterfly;         // Prefab 3D de la mariposa en escena
    public GameObject prefabPlant;         // Prefab 3D de la mariposa en escena
    [Range(0.1f, 2f)] public float scale = 0.25f;
    [Header("Video — se reproduce en el panel derecho al inspeccionar")]
    public VideoClip videoClip;

    [Header("Planta hospedera")]
    [Tooltip("Identificador unico de la planta hospedera de esta especie. " +
             "Debe coincidir exactamente con el hostPlantID del HostPlant en escena.")]
    public string hostPlantID = "";

    [Header("Vuelo")]
    [Range(1f, 20f)] public float flightSpeed = 6f;
    [Range(1f, 20f)] public float turnSpeed = 4f;
    [Range(0f, 3f)] public float bobAmplitude = 0.5f; // oscilación vertical suave
    [Range(0.1f, 5f)] public float bobFrequency = 1.5f;
}