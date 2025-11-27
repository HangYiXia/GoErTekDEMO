using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SetChestMat : MonoBehaviour
{
    

    public Material mat;
    public float cutoffHeight;
    public float dissolveSpeed = 0.01f;
    public bool appearFlag = false;

    void Start()
    {
        if (!mat)
        {
            Debug.LogError("Cannot find material of Chest!");
        }
    }

    void FixedUpdate()
    {
        if (appearFlag)
        {
            SetMatProperty();
        }

        mat.SetFloat("_CutoffHeight", cutoffHeight);
    }

    public void SetMatProperty()
    {
        appearFlag = true;

        cutoffHeight += dissolveSpeed;
    }
}
