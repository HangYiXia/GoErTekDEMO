using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using System.Text;
using System.IO;
using UnityEngine.Rendering.HighDefinition;

public class DOFController : MonoBehaviour
{

    public Volume globalVolume; // 用于在 Inspector 中直接指定 Volume，更高效
    public GameObject focusGameObject;
    public Camera dofCamera;
    // 等待EyeTracking bug修复
    public bool useEyeTracking = false;
    public Vector3 eyeTrackingPosition = Vector3.zero;

    // 调整焦距
    public GameObject xeryonManager;
    public int xeryonScale = 10;

    private XeryonHardwareManager xeryonHardwareManager = null;

    // 调整这两个参数，让模糊与清晰边界合适
    public float nearOffset = 1.0f;
    public float farOffset = 1.0f;

    public float halfDiopter = 0.05f;
    

    private MyGaussianBlurSinglePass myGaussianBlur; // 缓存自定义效果的引用
    private Vector3 focusPosition;

    // 在你的类顶部添加这个变量
    private float xeryonTimer = 0.0f;
    public float xeryonInterval = 1.0f; // 方便以后修改间隔
    private float lastStartTime = 0.0f;

    private bool ok = false;

    private string outputPath = "E:\\UnityProjects\\result\\xeryonTime.csv";

    struct Item
    {
        public float curDepth;
        public string state;
        public string curTime;
        public float opTime;

        Item(float _curDepth, string _state, string _curTime, float _opTime)
        {
            curDepth = _curDepth;
            state = _state;
            curTime = _curTime;
            opTime = _opTime;
        }
    }

    private Item curItem;

    private List<Item> itemList = new List<Item>();

    void SaveToDisk(string path)
    {
        // 1. 安全检查：如果列表为空，可以选择不保存或保存空表头
        if (itemList == null || itemList.Count == 0)
        {
            Debug.LogWarning("列表为空，没有数据被保存。");
            return;
        }

        // 2. 使用 StringBuilder 拼接数据（性能优于字符串直接相加）
        StringBuilder sb = new StringBuilder();

        // 3. 写入表头（列名）
        sb.AppendLine("depth,state,curTime,opTime");

        // 4. 遍历列表写入数据
        foreach (var item in itemList)
        {
            // 如果 state 或 curTime 中可能包含英文逗号，建议用双引号包裹：
            // string line = $"{item.curDepth},\"{item.state}\",\"{item.curTime}\",{item.opTime}";
        
            string line = $"{item.curDepth},{item.state},{item.curTime},{item.opTime}";
            sb.AppendLine(line);
        }

        try
        {
            // 5. 确保目录存在（可选，防止路径文件夹不存在报错）
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 6. 写入文件
            // new UTF8Encoding(true) 表示带BOM的UTF8，这对Excel正确识别中文至关重要
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        
            Debug.Log($"保存成功: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存失败: {e.Message}");
        }
    }
    void Start()
    {
        // 检查是否在 Inspector 中指定了 Volume
        if (globalVolume == null)
        {
            // 如果没有指定，尝试在场景中自动查找
            globalVolume = FindObjectOfType<Volume>();
        }

        if (globalVolume == null)
        {
            Debug.LogError("场景中没有找到 Volume 组件！请确保场景中有一个 Volume。");
            return;
        }

        // 从 Volume Profile 中获取我们的自定义后处理效果
        // 为了安全地在运行时修改，我们最好操作 profile 的一个实例 (profile)，
        // 而不是直接修改资源文件 (sharedProfile)。
        // 注意：第一次访问 .profile 会自动创建一个实例。
        if (globalVolume.profile.TryGet<MyGaussianBlurSinglePass>(out var customEffect))
        {
            myGaussianBlur = customEffect;
            Debug.Log("成功找到 MyGaussianBlurSinglePass 效果！");
        }
        else
        {
            Debug.LogError("在指定的 Volume Profile 中没有找到 MyGaussianBlurSinglePass！请检查 Volume Profile 的设置。");
        }

        if (xeryonManager != null)
        {
            xeryonHardwareManager = xeryonManager.GetComponent<XeryonHardwareManager>();
            if (xeryonHardwareManager != null)
            {
                Debug.Log("Get xeryonHardwareManager Successfully");
            }
        }
        else
        {
            Debug.LogError("xeryonManager is null");
        }

        ok = false;
    }

    public void IsOkToSetXeryon()
    {
        ok = true;
    }

    private float LinearMap(float x, float minV, float maxV)
    {
        return (x - minV) / (maxV - minV) * 600;
    }

    private float GetNearEnd(float curDepth)
    {
        float curDiopter = 1.0f / curDepth;
        if (curDiopter > 0.17f)
        {
            float nearStartDiopter = curDiopter + 0.02f;
            return 1.0f / nearStartDiopter;
        }
        else
        {
            float nearStartDiopter = curDiopter + 0.02f;
            return 1.0f / nearStartDiopter;
        }
    }

    private float GetNearStart(float curDepth)
    {
        float curDiopter = 1.0f / curDepth;
        if(curDiopter > 0.17f)
        {
            float nearStartDiopter = curDiopter + 0.02f + 0.001f;
            return 1.0f / nearStartDiopter;
        }
        else
        {
            float nearStartDiopter = curDiopter + 0.02f + 0.001f;
            return 1.0f / nearStartDiopter;
        }
    }

    private float GetFarStart(float curDepth)
    {
        float farStartDiopter = Mathf.Max(1.0f / curDepth - 0.05f, 0.0001f);
        return 1.0f / farStartDiopter;
    }

    private float GetFarEnd(float curDepth)
    {
        float farStartDiopter = Mathf.Max(1.0f / curDepth - 0.05f - 0.001f, 0.0001f);
        return 1.0f / farStartDiopter;
    }

    private float CacMaxCoCSize(float focalLength, float kernelRadius, ref Camera mainCamera)
    {
        if (focalLength <= 0 || kernelRadius < 0 || mainCamera == null)
        {
            Debug.LogError("Invalid parameters in CacMaxCoCSize");
            return 1e-5f;
        }
        
        int rtHeight = mainCamera.pixelHeight; // 渲染目标宽度（像素）
        float fov = mainCamera.fieldOfView;  // 相机视场角（度，默认垂直视场角）
        float focusDistance = focalLength;
        
        float halfFovRad = fov * Mathf.Deg2Rad / 2f; // 视场角的一半（弧度）
        float focusPlaneHeight = 2 * focusDistance * Mathf.Tan(halfFovRad); // 对焦平面的屏幕总宽度（米）
        float pixelWorldSize = focusPlaneHeight / rtHeight; // 像素世界尺寸（米/像素）
        float blurRangePixel = 2 * kernelRadius;
        
        float maxCoCSize = blurRangePixel * pixelWorldSize * 0.00002f;
        
        Debug.Log($"Max CoC Size = {maxCoCSize} meter");
        maxCoCSize = 4e-5f;
        return maxCoCSize;
    }

    void OnCompeleteSetXeryon(float finishedTime)
    {
        curItem.curTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        curItem.state = "end";
        curItem.opTime = finishedTime - lastStartTime;
        
        lastStartTime = finishedTime;
        itemList.Add(curItem);
    }

    void SetXeryon(int value)
    {
        return;
        Debug.Log("SetXeryon is called");
        lastStartTime = Time.realtimeSinceStartup * 1000.0f;
        curItem.curTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        curItem.state = "Start";
        curItem.opTime = 0;
        
        itemList.Add(curItem);
        if(!ok)return;
        return; // de-comment it when crashing
        if (xeryonHardwareManager != null)
        {
            xeryonHardwareManager.SetXeryonL(value);
            xeryonHardwareManager.SetXeryonR(value, OnCompeleteSetXeryon);
        }
        else
        {
            Debug.LogError("xeryonHardwareManager is null");
        }
    }
    void Update()
    {
        focusPosition = useEyeTracking ? eyeTrackingPosition : focusGameObject.GetComponent<Transform>().position;
        float depth = CalcDepthFromDOFCamera(dofCamera, focusPosition);
        curItem.curDepth = depth;
        //Debug.Log("focus game object's depth = " + depth);

        // --- 2. 使用计时器控制 SetXeryon ---
        xeryonTimer += Time.deltaTime; // 累加每帧的时间

        if (xeryonTimer >= xeryonInterval)
        {
            // 时间间隔（1秒）已到，执行函数
            SetXeryon(Mathf.Clamp(Mathf.CeilToInt(LinearMap(depth, 3.0f, 35.0f)), 0, 600));

            // 重置计时器
            xeryonTimer -= xeryonInterval;
        }


        /*
        myGaussianBlur.nearBlurEnd.value = GetNearEnd(depth);
        myGaussianBlur.nearBlurStart.value = myGaussianBlur.nearBlurEnd.value - 8.0f;
        Debug.Log("myGaussianBlur.nearBlurEnd.value = " + myGaussianBlur.nearBlurEnd.value);
        Debug.Log("myGaussianBlur.nearBlurStart.value = " + myGaussianBlur.nearBlurStart.value);

        myGaussianBlur.farBlurStart.value = GetFarStart(depth);
        myGaussianBlur.farBlurEnd.value = myGaussianBlur.farBlurStart.value + 8.0f;
        Debug.Log("myGaussianBlur.farBlurStart.value = " + myGaussianBlur.farBlurStart.value);
        Debug.Log("myGaussianBlur.farBlurEnd.value = " + myGaussianBlur.farBlurEnd.value);
        */
        
        myGaussianBlur.focalLength.value = depth;
        myGaussianBlur.maxCoCsize.value = CacMaxCoCSize(depth, myGaussianBlur.radius.value, ref dofCamera);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SaveToDisk(outputPath);
        }
    }

    float CalcDepthFromDOFCamera(Camera dofCamera, Vector3 worldPosition)
    {
        // 1. 获取摄像机的 Transform 组件
        Transform cameraTransform = dofCamera.transform;

        // 2. 计算从摄像机位置指向目标世界位置的向量
        //    worldPosition: 目标点的位置
        //    cameraTransform.position: 摄像机的位置
        Vector3 cameraToWorldPosition = worldPosition - cameraTransform.position;

        // 3. 获取摄像机的前向向量 (Z 轴)
        //    这是一个已经标准化的单位向量，代表了摄像机正对着的方向。
        Vector3 cameraForward = cameraTransform.forward;

        // 4. 使用点积 (Dot Product) 计算投影距离
        //    点积 A·B 的几何意义是向量 A 在向量 B 上的投影长度乘以 B 的模长。
        //    因为 cameraForward 是一个单位向量（模长为 1），
        //    所以这里的点积结果直接就是 cameraToWorldPosition 在 cameraForward 上的投影长度。
        //    这个长度就是我们需要的线性深度。
        float depth = Vector3.Dot(cameraToWorldPosition, cameraForward);

        return depth;
    }

}