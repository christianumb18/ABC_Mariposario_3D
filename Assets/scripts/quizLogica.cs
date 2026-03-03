using UnityEngine;
using UnityEngine.UIElements;

public class quizLogic : MonoBehaviour
{
    public QuestionManager questionManager;
    public UIDocument quizUIDocument;
    newPlayerLogic mainPlayerLogic;
    QuizData quizActual;

    public int preguntaActual;
    private int puntaje = 0;

    void Start()
    {

    }

    private void Update()
    {
        if (preguntaActual > quizActual.questions.Count)
        {
            
        }
    }
}
