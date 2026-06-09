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
    [Header("Traduccion EN (opcional)")]
    [Tooltip("Nombre en ingles. Si esta vacio se usa speciesName.")]
    public string speciesNameEn = "";
    [TextArea(2, 4)]
    [Tooltip("Descripcion en ingles. Si esta vacio se usa description.")]
    public string descriptionEn = "";

    /// <summary>Devuelve el nombre segun el idioma activo de LocalizationManager.</summary>
    public string GetLocalizedSpeciesName()
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == 1
            && !string.IsNullOrEmpty(speciesNameEn))
            return speciesNameEn;
        return speciesName;
    }

    /// <summary>Devuelve la descripcion segun el idioma activo.</summary>
    public string GetLocalizedDescription()
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == 1
            && !string.IsNullOrEmpty(descriptionEn))
            return descriptionEn;
        return description;
    }
    public GameObject prefabButterfly;         // Prefab 3D de la mariposa en escena
    public GameObject prefabPlant;         // Prefab 3D de la mariposa en escena
    [Range(0.1f, 2f)] public float scale = 0.25f;
    [Header("Video � se reproduce en el panel derecho al inspeccionar")]
    public VideoClip videoClip;

    [Header("Planta hospedera")]
    [Tooltip("Identificador unico de la planta hospedera de esta especie. " +
             "Debe coincidir exactamente con el hostPlantID del HostPlant en escena.")]
    public string hostPlantID = "";

    [Header("Vuelo")]
    [Range(1f, 20f)] public float flightSpeed = 6f;
    [Range(1f, 20f)] public float turnSpeed = 4f;
    [Range(0f, 3f)] public float bobAmplitude = 0.5f; // oscilaci�n vertical suave
    [Range(0.1f, 5f)] public float bobFrequency = 1.5f;


    // Esta parte es la logica de los videos que van en el ciclo de vida
    [Header("Ciclo de Vida (Fase 2)")]
    public UnityEngine.Video.VideoClip videoDesove;
    public UnityEngine.Video.VideoClip videoNacimiento;
    public UnityEngine.Video.VideoClip videoCrisalida;    
    public UnityEngine.Video.VideoClip videoNacimientoMariposa;
}