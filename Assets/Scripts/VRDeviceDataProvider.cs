using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRDeviceDataProvider : MonoBehaviour
{
    static private VRDeviceDataProvider _instance;
    static public VRDeviceDataProvider Instance
    {
        get
        {
            if (_instance==null)
            {
                _instance = FindObjectOfType<VRDeviceDataProvider>();
                if(_instance==null)
                {
                    Debug.LogError("场景中没有放置VRDeviceProvider");
                }
            }
            return _instance;
        }
    }

    [SerializeField]
    private Transform VRCamera;

    public Quaternion GetVRCameraWorldRoataion()
    {
        return VRCamera.transform.rotation;
    }

    public Vector3 GetVRCameraWorldPosition()
    {
        return VRCamera.transform.position;
    }


}
