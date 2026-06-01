using UnityEngine;

/// <summary>
/// Tipo de objetivo que se mostrará en el minimapa y la brújula.
/// </summary>
public enum MinimapTargetType
{
    HostPlant,  // Planta hospedera (la de tu especie pulsa)
    Nectar,     // Planta nectarífera
    Predator    // Depredador
}

/// <summary>
/// Agrega este componente a cualquier objeto del mundo que deba aparecer en el
/// minimapa. Las plantas hospederas existentes (HostPlant) se detectan
/// automáticamente, así que NO necesitas este componente en ellas — solo en
/// nectaríferas y depredadores futuros.
///
/// SETUP cuando crees una planta nectarífera o un depredador:
///   1. Add Component → MinimapTarget.
///   2. Elige type = Nectar o Predator.
///   3. (Opcional) displayName para que se vea en la brújula del HUD.
/// </summary>
[DisallowMultipleComponent]
public class MinimapTarget : MonoBehaviour
{
    [Tooltip("Tipo de objetivo — define color e ícono en el minimapa.")]
    public MinimapTargetType type = MinimapTargetType.Nectar;

    [Tooltip("Para HostPlant Y Nectar: identificador de la especie a la que pertenece. " +
             "Solo se mostrará en el minimapa si coincide con la especie del jugador. " +
             "Si lo dejas vacío y el GameObject tiene HostPlant, se copia de ahí. " +
             "Los Predator IGNORAN este campo (siempre se descubren).")]
    public string hostPlantID;

    [Tooltip("Nombre que muestra la brújula del HUD. Si vacío usa el nombre del GameObject.")]
    public string displayName;

    [Tooltip("Si quieres forzar un color específico, ponlo con alpha > 0. " +
             "Si alpha = 0 se usa el color por defecto del tipo.")]
    public Color iconColorOverride = new Color(0f, 0f, 0f, 0f);

    public string GetDisplayName() =>
        string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;
}
