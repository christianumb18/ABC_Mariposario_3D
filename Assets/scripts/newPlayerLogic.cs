using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(CharacterController))]
public class newPlayerLogic : MonoBehaviour
{
    //elementos basicos que del movimiento, se pueden editar directamente en el editor.
    public Camera playerCamera;
    public float walkSpeed = 6f;
    public float lookSpeed = 2f;
    public float lookXLimit = 90f;
    public float defaultHeight = 2f;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0f;
    private CharacterController characterController;
    public bool canMove = true;
    Vector3 playerPosition;
    public Vector3 positionOffset = new Vector3(1,0,2);
    public Vector3 RotationOffset = new Vector3 (0,1,0);
    Vector3 lookatObject;

    //variables diseñadas para el tiempo
    bool limitcheck = false;
    float elapsedTime;//funciona en segundos multiplicar por 60 para convertirlo en minutos.
    public float limitTime;//se hara en minutos.

    //elementos de variable de raycast para obtener informacion de objeto
    public float maxDistancia = 40f;

    //objeto para obtener informacion de actores y clases externas
    ActorDataComponent actorData;
    UIsystem ui;
    Canvas canvas;

    //elementos para el sistema de logica de quiz
    public QuizData quizData;
    private int preguntaActual = 0;
    private bool quizEnCurso = false;

    //estos numeros representan el gamestate 0 es normal 1 es observando info de mariposas y 2 son para respuestas 3 es para juego terminado
    static public int Gamestate = 0;

    public void setGameState(int state)
    {
        Gamestate = state;
    }

    public void disableMovment()//ingresa al modo de info de mariposas
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        canMove = false;
        canvas.enabled = false;
    }

    //funcion para exportar la data del actor al UI
    public ActorDataComponent GetActorInfo()
    {
        return actorData;
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        ui = FindObjectOfType<UIsystem>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        canvas = FindObjectOfType<Canvas>();
        playerCamera.transform.position = playerPosition;
    }

    void Update()
    {
        //establece situaciones de ver adelante
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        //logica de movimiento basica
        float curSpeedX = canMove ? walkSpeed * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? walkSpeed * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);
        moveDirection.y = movementDirectionY;

        characterController.Move(moveDirection * Time.deltaTime);

        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * maxDistancia, Color.red, 1f);


        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }

        //estos elementos se encargan del tiempo mientras se ejecuta el programa
        elapsedTime += Time.deltaTime;
        /*Debug.Log("el tiempo transcurrido es:"+elapsedTime);*/

        /*if (elapsedTime == limitTime && limitcheck == true)
        {
            Gamestate = 3;
        } esto es un codigo que puede servir para poner limite de tiempo*/

        //esta genera un raycast para obtener informacion sobre un objeto
        if (Input.GetMouseButtonDown(0) && Gamestate == 0)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            playerPosition = playerCamera.transform.position;

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistancia))
            {
                // Buscar el componente en el objeto golpeado o sus padres
                actorData = hit.collider.GetComponentInParent<ActorDataComponent>();

                if (actorData != null)
                {
                    Gamestate = 1;
                    Debug.Log("familia: " + actorData.familia); //esto esta para ver que si esta leyendo la info de las mariposas correctamente, tambien se podria ver dentro de las carillas del editor de unity
                }
                else
                {
                    Debug.Log("Impactó un objeto, o no tiene el script de informacion");
                }
            }
        }

        //esta funcion resetea el estado de juego al roaming
        if (Input.GetMouseButtonDown(1))
        {
            Gamestate = 0;
        }

        

        Debug.Log("el game state es " + Gamestate);
    }
    private void LateUpdate()
    {
        switch (Gamestate)
        {
            case 0: //modo roaming 
                canMove = true;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                canvas.enabled = true;

                playerCamera.transform.SetParent(transform);
                playerCamera.transform.localPosition = new Vector3(0, 0.38f, 0);

                ui.LimpiarPantalla();

                break;
            case 1: //modo informacion mariposas
                disableMovment();

                ui.SetActorInfo(actorData);
                ui.MostrarInfo();
                if (actorData != null)
                {
                    playerCamera.transform.SetParent(null);
                    playerCamera.transform.position = actorData.transform.position + positionOffset;
                    playerCamera.transform.rotation = Quaternion.LookRotation(actorData.transform.forward);
                    playerCamera.transform.LookAt(actorData.transform.position + RotationOffset);
                }

                break;
            case 2: //modo preguntas mariposas
                disableMovment();

                if (actorData != null)
                {
                    playerCamera.transform.SetParent(null);
                    playerCamera.transform.position = actorData.transform.position + positionOffset;
                    playerCamera.transform.rotation = Quaternion.LookRotation(actorData.transform.forward);
                    playerCamera.transform.LookAt(actorData.transform.position + RotationOffset);
                }

                //logica de preguntas

                break;
        }
    }
}