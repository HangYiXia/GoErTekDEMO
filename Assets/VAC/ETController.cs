using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ETController : MonoBehaviour
{
    public Transform focusPoint;
    public Transform eyeGaze;
    public VACController vacController;
    public float test = 1f;

    #region 
    private float eyeGazeX;
    private float eyeGazeY;
    private Vector3 origin, direction;
    public float Max;
    public float Min;
    #endregion         
    public Transform testpoint;
    // Update is called once per frame
    void Update()
    {


        ETSDK.EtResult3D result = new ETSDK.EtResult3D();
        if (ETSDK.ET_GetTrackResult(out result))
        {
            //eyeGaze.localPosition = result.gazeOrigin;

            origin = result.gazeOrigin / 1000;
            direction = result.gazeDirection / 1000;


            testpoint.localPosition = origin + direction * (test - origin.z) / direction.z;

            eyeGazeX = Mathf.Atan(testpoint.localPosition.x / testpoint.localPosition.z) * 180 / Mathf.PI;
            eyeGazeY = -Mathf.Atan(testpoint.localPosition.y / testpoint.localPosition.z) * 180 / Mathf.PI;

            eyeGaze.transform.LookAt(testpoint);
            eyeGaze.localPosition = origin;
        }
        else
        {
            eyeGazeX += Input.GetAxis("Mouse X");
            eyeGazeY -= Input.GetAxis("Mouse Y");

            eyeGazeX = Mathf.Clamp(eyeGazeX, Min, Max);
            eyeGazeY = Mathf.Clamp(eyeGazeY, Min, Max);

            eyeGaze.localEulerAngles = new Vector3(eyeGazeY, eyeGazeX, 0);
        }




        RaycastHit hitInfo;
        if (Physics.Raycast(eyeGaze.position, eyeGaze.forward, out hitInfo))
        {
            //Debug.Log(hitInfo.point);
            //Debug.Log(hitInfo.collider.name);
            FocusTarget focusTarget = hitInfo.collider.GetComponent<FocusTarget>();
            if (focusTarget)
            {
                focusTarget.FoucsOn();
            }
        }
        Debug.DrawRay(eyeGaze.position, eyeGaze.position + eyeGaze.forward * 10, Color.red);

        focusPoint.position = hitInfo.point;
    }
}
