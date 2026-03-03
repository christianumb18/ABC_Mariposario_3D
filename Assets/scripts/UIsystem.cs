using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class UIsystem : MonoBehaviour
{
    public UIDocument uiDocument;
    public VisualTreeAsset layoutInfo;
    public VisualTreeAsset layoutPregunta;
    public VisualTreeAsset layoutMenu;
    private VisualElement content;
    public QuizData quizActual;
    public int preguntaActual;
    public int respuestaSeleccionada;
    public int preguntaIndex;
    int puntaje = 0;
    newPlayerLogic mainPlayerlogic = new newPlayerLogic();

    //estas son las variables las cuales van a leer para imprimirlas en pantalla
    public string nombre;
    public string familia;
    public string genero;
    public string especie;
    public string subespecie;
    public string habitat;
    public string planta_hospedadora;
    public string descripcion;
    public string caracteristicas;

    public void SetActorInfo(ActorDataComponent actor)
    {
        nombre = actor.GetNombre();
        familia = actor.GetFamilia();
        genero = actor.GetGenero();
        especie = actor.GetEspecie();
        subespecie = actor.GetSubespecie();
        habitat = actor.GetHabitat();
        planta_hospedadora = actor.GetPlantaHospedadora();
        descripcion = actor.GetDescripcion();
        caracteristicas = actor.GetCaracteristicas();
        quizActual = actor.GetQuizData();
    }

    private void Start()
    {
        // Buscar el contenedor
        content = uiDocument.rootVisualElement.Q<VisualElement>("content");
    }

    public void LoadScreen(VisualTreeAsset screen)
    {
        content.Clear();        
        content.Add(screen.Instantiate());
        Debug.Log("el visual tree asset actual: " + screen);
    }

    // Métodos para botones u otras acciones
    public void MostrarInfo()
    {

        LoadScreen(layoutInfo);
        

        VisualElement infoScreen = content.Children().First();

        //Usar la referencia 'infoScreen' para buscar las etiquetas y asignar los valores
        infoScreen.Q<Label>("Nombre").text = nombre;
        infoScreen.Q<Label>("familia").text = "FAMILIA: " + familia;
        infoScreen.Q<Label>("genero").text = "GENERO:" + genero;
        infoScreen.Q<Label>("especie").text = "ESPECIE: " + especie;
        infoScreen.Q<Label>("subespecie").text = "SUB-ESPECIE: " + subespecie;
        infoScreen.Q<Label>("habitat").text = "HABITAT: " + habitat;
        infoScreen.Q<Label>("planta_hospedadora").text = "PLANTA HOSPEDADORA: " + planta_hospedadora;
        infoScreen.Q<Label>("descripcion").text = "DESCRIPCIÓN: " + descripcion;
        infoScreen.Q<Label>("caracteristicas").text = "CARACTERISTICAS: " + caracteristicas;

        /*var btnQuestionario = infoScreen.Q<Button>("cuestionario");
        btnQuestionario.RegisterCallback<MouseUpEvent>(CuestionarioClicked);*/
    }

    //este codigo activa el boton de cuestionario, no se utiliza para la build de mi trabajo

    /*public void CuestionarioClicked(MouseUpEvent pantCuestionario)
    {
        mainPlayerlogic.setGameState(2);
        LoadScreen(layoutPregunta);
        SetPregunta(quizActual, 0);
        
    }*/

    public void SetPregunta(QuizData quizdata, int index)
    {
        VisualElement preguntaScreen = content.Children().First();
        index = preguntaActual;
        var preguntaData = quizdata.questions[index];

        // Asignar textos
        preguntaScreen.Q<Label>("Pregunta").text = preguntaData.Pregunta;

        var btn1 = preguntaScreen.Q<Button>("respuesta1");
        var btn2 = preguntaScreen.Q<Button>("respuesta2");
        var btn3 = preguntaScreen.Q<Button>("respuesta3");
        var btn4 = preguntaScreen.Q<Button>("respuesta4");

        btn1.text = preguntaData.Respuestas[0];
        btn2.text = preguntaData.Respuestas[1];
        btn3.text = preguntaData.Respuestas[2];
        btn4.text = preguntaData.Respuestas[3];

        // Limpiar callbacks anteriores para evitar duplicados
        btn1.UnregisterCallback<ClickEvent>(respuestaCheck);
        btn2.UnregisterCallback<ClickEvent>(respuestaCheck);
        btn3.UnregisterCallback<ClickEvent>(respuestaCheck);
        btn4.UnregisterCallback<ClickEvent>(respuestaCheck);

        // Registrar callbacks de cada botón
        btn1.RegisterCallback<ClickEvent>(respuestaCheck);
        btn2.RegisterCallback<ClickEvent>(respuestaCheck);
        btn3.RegisterCallback<ClickEvent>(respuestaCheck);
        btn4.RegisterCallback<ClickEvent>(respuestaCheck);
    }

    public void respuestaCheck(ClickEvent checkRespuesta)
    {
        Button btn = checkRespuesta.target as Button;

        // Recuperar el índice guardado
        int indiceRespuesta = (int)btn.userData;
        respuestaSeleccionada = indiceRespuesta;

        if (quizActual.questions[preguntaActual].Respuesta_Correcta == respuestaSeleccionada)
        {
            puntaje++;
        }

        preguntaActual++;

        Debug.Log("Presionaste la respuesta: " + indiceRespuesta);   
    }

    public int getAnswer()
    {
        return respuestaSeleccionada;
    }

    public void LimpiarPantalla()
    {
        content.Clear();
    }

    public void Update()
    {
        
    }
}