using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode tests para el proyecto Mariposario Virtual Interactivo.
///
/// Cubre tres flujos criticos:
///   1. ProfileManager: creacion y persistencia de perfiles.
///   2. ProfileManager.LoseLife: equivalente publico de
///      ScoreManager.RegisterPredatorHit (privado) — la logica real que
///      reduce vidas vive aqui.
///   3. MinimapMarkerSystem.ResetDiscoveriesForSpecies: reinicia los flags
///      de markers descubiertos.
///
/// COMO CORRER:
///   Unity > Window > General > Test Runner > PlayMode > Run All
/// </summary>
public class MariposarioPlayModeTests
{
    private GameObject _temp;

    [TearDown]
    public void TearDown()
    {
        if (_temp != null) Object.Destroy(_temp);
        // Limpieza de PlayerPrefs y archivos de perfil para que cada
        // test corra limpio.
        PlayerPrefs.DeleteAll();
    }

    // ═══════════════════════════════════════════════════════════════
    // TEST 1: ProfileManager — CreateProfile + persistencia
    // ═══════════════════════════════════════════════════════════════

    [UnityTest]
    public IEnumerator ProfileManager_CreateProfile_AddsToListAndPersists()
    {
        PlayerPrefs.DeleteAll();

        // Instancia A
        _temp = new GameObject("TestProfileManagerA");
        var pmA = _temp.AddComponent<ProfileManager>();
        yield return null; // un frame para Awake/Start

        bool created = pmA.CreateProfile("TestUser01");
        Assert.IsTrue(created, "CreateProfile debio devolver true para un nombre nuevo");

        var profiles = pmA.GetAllProfiles();
        Assert.IsTrue(profiles.Exists(p => p.userName == "TestUser01"),
            "El perfil debe aparecer en GetAllProfiles");
        Assert.AreEqual("TestUser01", pmA.Active?.userName,
            "El perfil recien creado debe quedar activo");

        // Crear el mismo nombre dos veces → debe devolver false
        bool duplicate = pmA.CreateProfile("TestUser01");
        Assert.IsFalse(duplicate, "CreateProfile no debe permitir duplicados");

        // Nombre vacio → false
        bool empty = pmA.CreateProfile("   ");
        Assert.IsFalse(empty, "CreateProfile debe rechazar nombres vacios");

        // Destruir y recrear para probar persistencia en disco
        Object.DestroyImmediate(_temp);
        yield return null;

        _temp = new GameObject("TestProfileManagerB");
        var pmB = _temp.AddComponent<ProfileManager>();
        yield return null;

        var profilesB = pmB.GetAllProfiles();
        Assert.IsTrue(profilesB.Exists(p => p.userName == "TestUser01"),
            "El perfil debe persistir entre instancias de ProfileManager");
    }

    // ═══════════════════════════════════════════════════════════════
    // TEST 2: LoseLife (logica real de RegisterPredatorHit)
    // ═══════════════════════════════════════════════════════════════
    //
    // ScoreManager.RegisterPredatorHit es PRIVADO y solo llama a
    // ProfileManager.LoseLife. El comportamiento publico observable
    // (vidas bajan, muerte en 0) se prueba aqui directo sobre LoseLife.

    [UnityTest]
    public IEnumerator ProfileManager_LoseLife_DecrementsAndDiesAtZero()
    {
        PlayerPrefs.DeleteAll();
        _temp = new GameObject("TestProfileManager");
        var pm = _temp.AddComponent<ProfileManager>();
        yield return null;

        Assert.IsTrue(pm.CreateProfile("DeathTester"), "Setup: crear perfil");

        const string sp = "TestSpecies";
        var species = pm.Active.GetOrCreate(sp);
        species.lives = 3;

        // Golpe 1: vidas 3 -> 2
        bool died1 = pm.LoseLife(sp);
        Assert.IsFalse(died1, "Primera vida perdida no debe matar");
        Assert.AreEqual(2, pm.Active.GetOrCreate(sp).lives, "Vidas deben quedar en 2");

        // Golpe 2: 2 -> 1
        bool died2 = pm.LoseLife(sp);
        Assert.IsFalse(died2, "Segunda vida perdida no debe matar");
        Assert.AreEqual(1, pm.Active.GetOrCreate(sp).lives, "Vidas deben quedar en 1");

        // Golpe 3: 1 -> 0 = muerte
        bool died3 = pm.LoseLife(sp);
        Assert.IsTrue(died3, "Tercera vida perdida debe disparar muerte");
    }

    // ═══════════════════════════════════════════════════════════════
    // TEST 3: MinimapMarkerSystem.ResetDiscoveriesForSpecies
    // ═══════════════════════════════════════════════════════════════
    //
    // _markers es privado (List<MarkerEntry>) y MarkerEntry es una clase
    // privada anidada. Usamos reflection para inyectar entradas de
    // prueba sin depender del flujo completo de discovery.

    [UnityTest]
    public IEnumerator MinimapMarkerSystem_ResetDiscoveriesForSpecies_ResetsMatchingFlags()
    {
        // El componente loguea LogError cuando faltan RawImage/MinimapCamera
        // en la escena. En este test usamos reflection sobre el estado interno,
        // no necesitamos esas dependencias.
        LogAssert.ignoreFailingMessages = true;

        _temp = new GameObject("TestMinimap");
        var mm = _temp.AddComponent<MinimapMarkerSystem>();
        yield return null;

        // Reflection: campos y tipos privados
        var t = typeof(MinimapMarkerSystem);
        var markersField = t.GetField("_markers",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(markersField, "Campo _markers debe existir");

        var entryType = t.GetNestedType("MarkerEntry", BindingFlags.NonPublic);
        Assert.IsNotNull(entryType, "Clase MarkerEntry debe existir como nested private");

        var typeField    = entryType.GetField("type");
        var hostIDField  = entryType.GetField("hostPlantID");
        var discField    = entryType.GetField("discovered");
        Assert.IsNotNull(typeField);
        Assert.IsNotNull(hostIDField);
        Assert.IsNotNull(discField);

        var enumType = typeField.FieldType;
        object hostPlantValue = System.Enum.Parse(enumType, "HostPlant");
        object nectarValue    = System.Enum.Parse(enumType, "Nectar");

        var markersList = markersField.GetValue(mm);
        var addMethod = markersList.GetType().GetMethod("Add");

        // 2 host plants de la especie "Caligo" descubiertos
        for (int i = 0; i < 2; i++)
        {
            var e = System.Activator.CreateInstance(entryType);
            typeField.SetValue(e, hostPlantValue);
            hostIDField.SetValue(e, "Caligo");
            discField.SetValue(e, true);
            addMethod.Invoke(markersList, new[] { e });
        }

        // 1 nectar de "Caligo" descubierto
        var n = System.Activator.CreateInstance(entryType);
        typeField.SetValue(n, nectarValue);
        hostIDField.SetValue(n, "Caligo");
        discField.SetValue(n, true);
        addMethod.Invoke(markersList, new[] { n });

        // 1 host de otra especie ("Morpho") descubierto — NO debe reiniciarse
        var other = System.Activator.CreateInstance(entryType);
        typeField.SetValue(other, hostPlantValue);
        hostIDField.SetValue(other, "Morpho");
        discField.SetValue(other, true);
        addMethod.Invoke(markersList, new[] { other });

        // Acto: reiniciar solo Caligo
        mm.ResetDiscoveriesForSpecies("Caligo");
        yield return null;

        // Verificar resultados
        int caligoReset = 0, otherIntact = 0;
        foreach (var e in (System.Collections.IEnumerable)markersList)
        {
            string id = (string)hostIDField.GetValue(e);
            bool disc = (bool)discField.GetValue(e);
            if (id == "Caligo")
            {
                Assert.IsFalse(disc, $"Marker de Caligo debio reiniciarse, sigue discovered=true");
                caligoReset++;
            }
            else if (id == "Morpho")
            {
                Assert.IsTrue(disc, "Marker de Morpho NO debio tocarse");
                otherIntact++;
            }
        }
        Assert.AreEqual(3, caligoReset, "Los 3 markers de Caligo deben quedar en false");
        Assert.AreEqual(1, otherIntact, "El marker de Morpho debe quedar intacto");
    }
}
