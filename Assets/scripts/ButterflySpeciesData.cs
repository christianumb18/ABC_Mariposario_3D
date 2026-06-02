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

    [Header("Video — se reproduce en el panel derecho al inspeccionar")]
    public VideoClip videoClip;
}
