using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class CicloDeVidaManager : MonoBehaviour
{
    // NUEVO: Agregamos las 4 fases nuevas al final
    public enum Fase { Video1, Huevos, Video2, Oruga, Video3, Crisalida, Video4, Mariposa }
    private Fase faseActual;

    [Header("UI - Video")]
    public GameObject panelVideo;
    public VideoPlayer videoPlayer;
    public Button btnPlayPause;
    public Slider sliderVideo; 
    
    [Header("UI - Controles Extra Video")]
    public Button btnAdelantarVideo; 
    public Button btnAtrasarVideo;   

    [Header("UI - Navegacion")]
    public GameObject panelNavegacion;
    public Button btnSiguiente;
    public Button btnAtras;
    public Button btnVolverMariposario;

    [Header("UI - Popup Salir")]
    public GameObject panelConfirmacion;
    public Button btnConfirmarSi;
    public Button btnConfirmarNo;

    [Header("Configuracion de Camaras (Por Especie)")]
    public Camera camaraPrincipal; 
    
    [System.Serializable]
    public struct PlantaSetup {
        public string hostPlantID;
        public Transform puntoCamaraHuevos; 
        public Transform puntoCamaraOruga;  
        // NUEVO: Los dos tripodes para las nuevas fases
        public Transform puntoCamaraCrisalida; 
        public Transform puntoCamaraMariposa;  
    }
    public List<PlantaSetup> plantasEnEscena;

    private ButterflyData dataActual;
    private PlantaSetup setupActual;

    private bool video1Visto = false;
    private bool video2Visto = false;
    private bool video3Visto = false; // NUEVO
    private bool video4Visto = false; // NUEVO

    private float fovOriginal = 60f;

    private void Start()
    {
        if (camaraPrincipal != null) fovOriginal = camaraPrincipal.fieldOfView;

        if (MariposarioGameManager.Instance != null)
            dataActual = MariposarioGameManager.Instance.SelectedSpecie;

        if (dataActual == null) return;

        ConfigurarEscenario();

        if(btnSiguiente != null) btnSiguiente.onClick.AddListener(Avanzar);
        if(btnAtras != null) btnAtras.onClick.AddListener(Retroceder);
        if(btnPlayPause != null) btnPlayPause.onClick.AddListener(TogglePlayPause);
        
        if (btnAdelantarVideo != null) btnAdelantarVideo.onClick.AddListener(() => SaltarVideo(5f));
        if (btnAtrasarVideo != null) btnAtrasarVideo.onClick.AddListener(() => SaltarVideo(-5f));

        if(panelConfirmacion != null) panelConfirmacion.SetActive(false);
        if(btnVolverMariposario != null) btnVolverMariposario.onClick.AddListener(() => panelConfirmacion.SetActive(true));
        if(btnConfirmarNo != null) btnConfirmarNo.onClick.AddListener(() => panelConfirmacion.SetActive(false));
        
        if(btnConfirmarSi != null) btnConfirmarSi.onClick.AddListener(() => {
            ControlarMusicaFondo(false);
            SceneManager.LoadScene("MapaMariposario_conPlantas");
        });

        if(videoPlayer != null) videoPlayer.loopPointReached += AlTerminarVideo;

        IrAFase(Fase.Video1);
    }

    private void Update()
    {
        if (panelVideo.activeSelf && videoPlayer.length > 0 && sliderVideo != null)
        {
            sliderVideo.value = (float)(videoPlayer.time / videoPlayer.length);
        }
    }

    private void SaltarVideo(float segundos)
    {
        if (videoPlayer != null && videoPlayer.length > 0)
        {
            double nuevoTiempo = videoPlayer.time + (double)segundos;
            nuevoTiempo = Mathf.Clamp((float)nuevoTiempo, 0f, (float)videoPlayer.length);
            videoPlayer.time = nuevoTiempo;
            
            if (!videoPlayer.isPlaying)
            {
                videoPlayer.Play();
                videoPlayer.time = nuevoTiempo;
                videoPlayer.Pause();
            }
        }
    }

    private void ConfigurarEscenario()
    {
        foreach (var planta in plantasEnEscena)
        {
            if (planta.hostPlantID == dataActual.hostPlantID)
            {
                setupActual = planta;
                return;
            }
        }
    }

    private void IrAFase(Fase nuevaFase)
    {
        faseActual = nuevaFase;
        
        if(panelVideo != null) panelVideo.SetActive(false);
        if(panelNavegacion != null) panelNavegacion.SetActive(false);
        if(btnPlayPause != null) btnPlayPause.gameObject.SetActive(false);
        if(btnAdelantarVideo != null) btnAdelantarVideo.gameObject.SetActive(false);
        if(btnAtrasarVideo != null) btnAtrasarVideo.gameObject.SetActive(false);
        if(sliderVideo != null) sliderVideo.gameObject.SetActive(false);
        if(videoPlayer != null) videoPlayer.Stop();
        
        if(btnAtras != null) btnAtras.gameObject.SetActive(faseActual != Fase.Video1);
        if(btnVolverMariposario != null) btnVolverMariposario.gameObject.SetActive(true); 

        if (camaraPrincipal != null) camaraPrincipal.fieldOfView = fovOriginal;

        // NUEVO: Añadidos los 4 casos adicionales al Switch
        switch (faseActual)
        {
            case Fase.Video1:
                ControlarMusicaFondo(true);
                ReproducirVideo(dataActual.videoDesove, video1Visto);
                break;
            case Fase.Huevos:
                ControlarMusicaFondo(false);
                if(panelNavegacion != null) panelNavegacion.SetActive(true);
                MoverCamara(setupActual.puntoCamaraHuevos);
                if(btnSiguiente != null) btnSiguiente.gameObject.SetActive(true);
                break;
            case Fase.Video2:
                ControlarMusicaFondo(true);
                ReproducirVideo(dataActual.videoNacimiento, video2Visto);
                break;
            case Fase.Oruga:
                ControlarMusicaFondo(false);
                if(panelNavegacion != null) panelNavegacion.SetActive(true);
                MoverCamara(setupActual.puntoCamaraOruga);
                if(btnSiguiente != null) btnSiguiente.gameObject.SetActive(true); // Oruga ya no es el final
                break;
            case Fase.Video3:
                ControlarMusicaFondo(true);
                ReproducirVideo(dataActual.videoCrisalida, video3Visto);
                break;
            case Fase.Crisalida:
                ControlarMusicaFondo(false);
                if(panelNavegacion != null) panelNavegacion.SetActive(true);
                MoverCamara(setupActual.puntoCamaraCrisalida);
                if(btnSiguiente != null) btnSiguiente.gameObject.SetActive(true);
                break;
            case Fase.Video4:
                ControlarMusicaFondo(true);
                ReproducirVideo(dataActual.videoNacimientoMariposa, video4Visto);
                break;
            case Fase.Mariposa:
                ControlarMusicaFondo(false);
                if(panelNavegacion != null) panelNavegacion.SetActive(true);
                MoverCamara(setupActual.puntoCamaraMariposa);
                if(btnSiguiente != null) btnSiguiente.gameObject.SetActive(false); // Ahora Mariposa es el verdadero final
                break;
        }
    }

    private void MoverCamara(Transform puntoDestino)
    {
        if (camaraPrincipal != null && puntoDestino != null)
        {
            camaraPrincipal.transform.position = puntoDestino.position;
            camaraPrincipal.transform.rotation = puntoDestino.rotation;
        }
    }

    private void ReproducirVideo(VideoClip clip, bool yaLoVio)
    {
        if(panelVideo != null) panelVideo.SetActive(true);
        if(panelNavegacion != null) panelNavegacion.SetActive(true);
        if(btnPlayPause != null) btnPlayPause.gameObject.SetActive(true);
        if(btnAdelantarVideo != null) btnAdelantarVideo.gameObject.SetActive(true);
        if(btnAtrasarVideo != null) btnAtrasarVideo.gameObject.SetActive(true);
        if(sliderVideo != null) sliderVideo.gameObject.SetActive(true); 

        if(btnSiguiente != null) btnSiguiente.gameObject.SetActive(yaLoVio); 

        if(videoPlayer != null && clip != null) // Añadida validacion por si no hay clip asignado aun
        {
            videoPlayer.clip = clip;
            videoPlayer.Play();
        }
    }

    private void AlTerminarVideo(VideoPlayer vp)
    {
        if (faseActual == Fase.Video1) video1Visto = true;
        if (faseActual == Fase.Video2) video2Visto = true;
        if (faseActual == Fase.Video3) video3Visto = true; // NUEVO
        if (faseActual == Fase.Video4) video4Visto = true; // NUEVO
        if(btnSiguiente != null) btnSiguiente.gameObject.SetActive(true);
    }

    private void TogglePlayPause()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
        else
        {
            videoPlayer.Play();
        }
    }

    private void Avanzar()
    {
        if (faseActual == Fase.Video1) IrAFase(Fase.Huevos);
        else if (faseActual == Fase.Huevos) IrAFase(Fase.Video2);
        else if (faseActual == Fase.Video2) IrAFase(Fase.Oruga);
        // NUEVO: Rutas de avance
        else if (faseActual == Fase.Oruga) IrAFase(Fase.Video3);
        else if (faseActual == Fase.Video3) IrAFase(Fase.Crisalida);
        else if (faseActual == Fase.Crisalida) IrAFase(Fase.Video4);
        else if (faseActual == Fase.Video4) IrAFase(Fase.Mariposa);
    }

    private void Retroceder()
    {
        if (faseActual == Fase.Huevos) IrAFase(Fase.Video1);
        else if (faseActual == Fase.Video2) IrAFase(Fase.Huevos);
        else if (faseActual == Fase.Oruga) IrAFase(Fase.Video2);
        // NUEVO: Rutas de retroceso
        else if (faseActual == Fase.Video3) IrAFase(Fase.Oruga);
        else if (faseActual == Fase.Crisalida) IrAFase(Fase.Video3);
        else if (faseActual == Fase.Video4) IrAFase(Fase.Crisalida);
        else if (faseActual == Fase.Mariposa) IrAFase(Fase.Video4);
    }

    private void ControlarMusicaFondo(bool silenciar)
    {
        PersistenciaMusica musicaObj = FindObjectOfType<PersistenciaMusica>();
        if (musicaObj != null)
        {
            AudioSource audio = musicaObj.GetComponent<AudioSource>();
            if (audio != null) audio.mute = silenciar;
        }
    }
}