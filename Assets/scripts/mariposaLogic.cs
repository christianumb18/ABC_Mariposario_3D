using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class mariposaLogic : MonoBehaviour
{
    public GameObject sisNodes;
    public newPlayerLogic playerLogic;
    public int maxNode;
    int randomNu;
    public List<Transform> nodes;  //esto es para tener el valore de los nodos en un struct
    Transform current_node; //es el nodo en el cual actualmente se estaria dirigiendo la mariposa
    public float velocidad = 2f;
    public float rotVelocidad = 50f;//velocidad de rotacion
    Vector3 direction;
    public int gamestate;

    public mariposaLogic()
    {
        nodes = new List<Transform>();
    }

    
    private void OnValidate()//no toquen esto, por alguna razon la logica no funciona sin esta funcion
    {
        sistemaNodos();
    }

    //logica para hacer la lista 
    public void sistemaNodos()
    {
        if (sisNodes != null)
        {
            // Obtener el componente de transformacion del padre y crear lista
            Transform parentTransform = sisNodes.transform;
            nodes.Clear();
            maxNode = sisNodes.transform.childCount;

            // un for loop de todos los hijos (empieza en 0)
            for (int i = 0; i < parentTransform.transform.childCount; i++)
            {
                // Obtenemos el Transform del hijo en el índice 'i'
                Transform nodo = parentTransform.GetChild(i);

                nodes.Add(nodo);
            }
        }
        
    }

    void Start()
    {
        //generacion aleatorea de numero y selecciona uno nuevo a partir de eso
        randomNu = Random.Range(0, maxNode);
        current_node = nodes[randomNu];
    }

    void Update()
    {
            if (gameObject.transform.position != current_node.transform.position && gamestate != 1)
            {
                //busca el nuevo nodo
                transform.position = Vector3.MoveTowards(transform.position, current_node.position, velocidad * Time.deltaTime);

                //eso es para que rote hacia el nuevo nodo
                direction = current_node.position - transform.position;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), rotVelocidad * 100 * Time.deltaTime);
            }
            else
            {
                //si lla llego aplica este codigo
                randomNu = Random.Range(0, maxNode);
                current_node = nodes[randomNu];

            }
    }
}