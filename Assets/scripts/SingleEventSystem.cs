using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventSystem))]
public class SingleEventSystem : MonoBehaviour
{
    private static SingleEventSystem _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(_instance.gameObject);   // mata al viejo, no al nuevo
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}