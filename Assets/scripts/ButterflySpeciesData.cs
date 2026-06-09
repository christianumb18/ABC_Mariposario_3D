using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Datos extendidos de una especie de mariposa para el panel de inspección
/// (nombre común, nombre científico, video, descripción).
///
/// Crea uno por especie: Assets > Create > Mariposario > Species Data
/// Después arrástralo al ButterflyIdentity del prefab correspondiente.
/// </summary>
[CreateAssetMenu(fileName = "NewSpeciesData", menuName = "Mariposario/Species Data")]
public class ButterflySpeciesData : ScriptableObject
{
    [Header("Identidad")]
    [Tooltip("Nombre común (ej. 'Monarca')")]
    public string commonName = "Mariposa";

    [Tooltip("Nombre científico (ej. 'Danaus plexippus')")]
    public string scientificName = "Genus species";

    [TextArea(3, 6)]
    public string description = "";

    [Header("Traduccion EN (opcional)")]
    [Tooltip("Nombre comun en ingles. Si esta vacio se usa commonName.")]
    public string commonNameEn = "";

    [TextArea(3, 6)]
    [Tooltip("Descripcion en ingles. Si esta vacio se usa description.")]
    public string descriptionEn = "";

    [Header("Video — se reproduce en el panel derecho al inspeccionar")]
    public VideoClip videoClip;

    public string GetLocalizedCommonName()
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == 1
            && !string.IsNullOrEmpty(commonNameEn))
            return commonNameEn;
        return commonName;
    }

    public string GetLocalizedDescription()
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == 1
            && !string.IsNullOrEmpty(descriptionEn))
            return descriptionEn;
        return description;
    }
}
