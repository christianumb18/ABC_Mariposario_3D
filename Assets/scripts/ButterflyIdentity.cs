using UnityEngine;

/// <summary>
/// Componente que enlaza un prefab de mariposa con su ButterflySpeciesData.
/// Adjúntalo al prefab raíz de cada especie y arrastra el SpeciesData correspondiente.
///
/// El GameStateManager lo busca con GetComponent al inspeccionar la mariposa.
/// </summary>
public class ButterflyIdentity : MonoBehaviour
{
    [Tooltip("ScriptableObject con nombre, video y descripción de esta especie")]
    public ButterflySpeciesData speciesData;
}
