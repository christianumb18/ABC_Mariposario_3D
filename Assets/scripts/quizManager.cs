using UnityEngine;

public class QuestionManager : MonoBehaviour
{
    public QuizData quizData;

    private int indexActual = 0;
    public QuizData.Question GetPreguntaActual()
    {
        if (quizData == null || quizData.questions.Count == 0) return null;
        return quizData.questions[indexActual];
    }

    public bool AvanzarPregunta()
    {
        indexActual++;
        return indexActual < quizData.questions.Count;
    }
}
