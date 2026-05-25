using UnityEngine;

public class PersistenciaMusica : MonoBehaviour
{
    private static PersistenciaMusica instance;

    void Awake()
    {
        // Si ya existe una instancia de la música, destruye esta nueva para no duplicarla
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            // Evita que este objeto se destruya al cargar una nueva escena
            DontDestroyOnLoad(gameObject);
        }
    }
}
