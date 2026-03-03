using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Quiz Data", menuName = "Quiz System")]
public class QuizData : ScriptableObject
{
    [System.Serializable]
    public class Question
    {
        public string Pregunta = "Nueva Pregunta";
        public List<string> Respuestas = new List<string>
        {
            "Respuesta 1",
            "Respuesta 2",
            "Respuesta 3",
            "Respuesta 4"
        };

        public int Respuesta_Correcta = 0;
    }

    public List<Question> questions = new List<Question>();
}