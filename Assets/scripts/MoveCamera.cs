using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform PlayerLook;
    void Update()
    {
                transform.position = PlayerLook.position;
    }
}
