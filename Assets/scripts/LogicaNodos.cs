using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogicaNodos : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        {
            foreach (Transform t in transform)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(t.position, 0.2f);
            }

        }
    }
}
