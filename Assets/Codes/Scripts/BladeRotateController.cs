using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeRotateController : MonoBehaviour
{
    private Transform bladeTransform;
    private Vector3 rotateAxis = new Vector3(0.0f, -1.0f, 0.0f);
    public float angularSpeed = 0.1f;
    void Start()
    {
        bladeTransform = transform.GetChild(1);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        bladeTransform.Rotate(rotateAxis * angularSpeed * Time.deltaTime);
    }
}
