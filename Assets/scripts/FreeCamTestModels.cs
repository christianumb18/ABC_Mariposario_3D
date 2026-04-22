using UnityEngine;
using UnityEngine.InputSystem;

public class FreeCamTestModels : MonoBehaviour
{
    public float speed = 10f;
    public float sensitivity = 2f;
    float rotX, rotY;

    void Update()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            var delta = Mouse.current.delta.ReadValue();
            rotX -= delta.y * sensitivity * Time.deltaTime * 10f;
            rotY += delta.x * sensitivity * Time.deltaTime * 10f;
            transform.rotation = Quaternion.Euler(rotX, rotY, 0);
        }

        var kb = Keyboard.current;
        float x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        float z = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
        transform.Translate(new Vector3(x, 0, z) * speed * Time.deltaTime);
    }
}