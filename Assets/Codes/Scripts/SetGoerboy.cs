using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetGoerboy : MonoBehaviour
{
    Transform transfromChild;
    Vector3 savedPos = new Vector3();
    Quaternion relative;
    
    // Start is called before the first frame update
    void Start()
    {
        transfromChild = transform.GetChild(0);
        savedPos = transform.position;

        relative = Quaternion.Inverse(transfromChild.rotation) * transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        transfromChild.position += transform.position - savedPos;
        savedPos = transform.position;

        transfromChild.rotation = transform.rotation * relative;
    }
}
