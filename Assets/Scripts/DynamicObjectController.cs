using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicObjectController : MonoBehaviour
{
    [Range(0.1f, 100.0f)]
    public float ControllerForceFactor = 1.0f;
    Rigidbody Rigidbody;
    private void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.I))
        {
            Rigidbody.velocity = (new Vector3(1.0f, 0.0f, 0.0f) * ControllerForceFactor);
        }
        else if (Input.GetKey(KeyCode.K))
        {
            Rigidbody.velocity = (new Vector3(-1.0f, 0.0f, 0.0f) * ControllerForceFactor);
        }
        else if (Input.GetKey(KeyCode.J))
        {
            Rigidbody.velocity = (new Vector3(0.0f, 0.0f, 1.0f) * ControllerForceFactor);
        }
        else if (Input.GetKey(KeyCode.L))
        {
            Rigidbody.velocity = (new Vector3(0.0f, 0.0f, -1.0f) * ControllerForceFactor);
        }
        else if (Input.GetKey(KeyCode.U))
        {
            Rigidbody.velocity = (new Vector3(0.0f, 1.0f, 0.0f) * ControllerForceFactor);
        }
        else if (Input.GetKey(KeyCode.O))
        {
            Rigidbody.velocity = (new Vector3(0.0f, -1.0f, 0.0f) * ControllerForceFactor);
        }
        else
        {
            Rigidbody.velocity = new Vector3(0.0f, 0.0f, 0.0f);
        }
    }
}
