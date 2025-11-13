using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoerboyAnimatorChanger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnRun(string stateName)
    {
        Debug.Log("OnRun!");
    }

    void OnIdle(string stateName)
    {
        Debug.Log("OnIdle!");
    }
}
