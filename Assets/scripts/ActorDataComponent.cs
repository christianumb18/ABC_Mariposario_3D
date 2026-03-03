using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorDataComponent : MonoBehaviour
{
    // Info de las pariposas
    [SerializeField] public string nombre;
    [SerializeField] public string familia;
    [SerializeField] public string genero;
    [SerializeField] public string especie;
    [SerializeField] public string subespecie;
    [SerializeField] public string habitat;
    [SerializeField] public string planta_hospedadora;
    [SerializeField] public string descripcion;
    [SerializeField] public string caracteristicas;
    [SerializeField] public QuizData quizData;

    //Métodos para obtener la informacion 

    public string GetNombre() { return nombre; }

    public string GetFamilia() { return familia; }

    public string GetGenero() { return genero; }

    public string GetEspecie() { return especie; }

    public string GetSubespecie() { return subespecie; }

    public string GetHabitat() { return habitat; }

    public string GetPlantaHospedadora() {return planta_hospedadora;}

    public string GetDescripcion() { return descripcion; }

    public string GetCaracteristicas() { return caracteristicas; }

    public QuizData GetQuizData() 
    {    
        if (quizData == null)
        {
            Debug.Log("El actor seleccionado no tiene el scriptable object 'QuizData'");
            return null;
        }
        return quizData;}
    }
