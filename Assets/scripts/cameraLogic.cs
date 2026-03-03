using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    float xRotation;
    float yRotation;

    public Transform orientation;

    public float sensX = 200f;
    public float sensY = 200f;

    // Start is called before the first frame update
    void Start()
    {
        //esto crea el cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;

    }

    // Update is called once per frame
    void Update()
    {
        //input de mouse
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;

        //esto limita la vista a 90 grados verticalmente
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Rotar camara
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);

        //debuging
        Debug.Log("valor x = "+mouseX);
        Debug.Log("valyor y = "+mouseY);

    }
}
