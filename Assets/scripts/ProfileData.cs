using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Progreso de UNA especie dentro del perfil de un usuario.
/// Score, vidas, muertes, video visto y qué hospederas/nectaríferas/depredadores
/// ha descubierto (IDs estables — se usa el nombre del GameObject).
/// </summary>
[Serializable]
public class SpeciesProgress
{
    public string speciesID;                           // ButterflyData.speciesName
    public int    score;
    public int    highScore;
    public int    lives = 3;
    public int    totalDeaths;
    public bool   videoSeen;
    public List<string> discoveredTargets = new();

    public SpeciesProgress() { }
    public SpeciesProgress(string id) { speciesID = id; }
}

/// <summary>
/// Datos persistentes de UN usuario.
/// </summary>
[Serializable]
public class ProfileData
{
    public string userName;
    public float  flightTimeSeconds;
    public int    totalScore;
    public List<SpeciesProgress> species = new();

    public ProfileData() { }
    public ProfileData(string name) { userName = name; }

    public SpeciesProgress GetOrCreate(string speciesID)
    {
        if (string.IsNullOrEmpty(speciesID)) return null;
        for (int i = 0; i < species.Count; i++)
            if (species[i].speciesID == speciesID) return species[i];
        var sp = new SpeciesProgress(speciesID);
        species.Add(sp);
        return sp;
    }

    public int CountSpeciesWithVideoSeen()
    {
        int n = 0;
        for (int i = 0; i < species.Count; i++) if (species[i].videoSeen) n++;
        return n;
    }

    public void RecomputeTotalScore()
    {
        int t = 0;
        for (int i = 0; i < species.Count; i++) t += species[i].score;
        totalScore = t;
    }
}

/// <summary>
/// Contenedor de TODOS los perfiles + cuál está activo. Se serializa a JSON
/// y se guarda como string en PlayerPrefs.
/// </summary>
[Serializable]
public class ProfilesStore
{
    public List<ProfileData> profiles = new();
    public string            activeProfileName;

    public ProfileData FindByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < profiles.Count; i++)
            if (profiles[i].userName == name) return profiles[i];
        return null;
    }

    public ProfileData ActiveProfile => FindByName(activeProfileName);
}
