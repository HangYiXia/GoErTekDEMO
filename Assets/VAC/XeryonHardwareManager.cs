using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Xeryon 硬件管理器 (已解耦)
/// 职责：
/// 1. 封装所有 XeryonControl.dll 的 PInvoke 调用。
/// 2. 管理硬件连接的生命周期 (初始化, 启动, 停止, 销毁)。
/// 3. 提供 SetXeryonL/R 和 SetVariFocal 的公共接口。
/// 4. 加载和保存硬件相关的配置 (Ports, PlayerPrefs 状态)。
/// </summary>
public class XeryonHardwareManager : MonoBehaviour
{
    // 硬件实例指针
    private IntPtr ctrlPtrL, ctrlPtrR;
    
    // 硬件配置
    private int portL, portR;
    private string path;
    private string configTxt = "config.txt";

    // 硬件状态
    private int curXeryonL, curXeryonR, curVariFocal;

    #region Unity 生命周期

    void Awake()
    {
        path = Application.streamingAssetsPath + "/";
        LoadConfig(); // 1. 加载端口配置
        LoadStateFromPrefs(); // 2. 加载上次保存的状态
    }

    async void Start()
    {
        // 3. 异步初始化硬件
        await Task.Run(() =>
        {
            ctrlPtrL = XC_CreateInstance(portL);
            ctrlPtrR = XC_CreateInstance(portR);
        });
        await Task.Run(() =>
        {
            XC_IAddAxis(ctrlPtrL);
            XC_IAddAxis(ctrlPtrR);
        });
        await Task.Run(() =>
        {
            XC_IStart(ctrlPtrL);
            XC_IStart(ctrlPtrR);
        });

        // 4. 应用加载的状态
        //    (这复制了原 VACController.SetLoad() 中的逻辑)
        SetVariFocal(curVariFocal); // 恢复变焦模式
        SetXeryonL(0);              // 将硬件重置到逻辑 0 位置
        SetXeryonR(0);              // 将硬件重置到逻辑 0 位置
    }

    async void OnDestroy()
    {
        // 5. 保存当前状态
        SetSaveToPrefs();

        // 6. 异步停止和销毁硬件
        await Task.Run(() =>
        {
            XC_IStop(ctrlPtrL);
            XC_IStop(ctrlPtrR);
        });
        await Task.Run(() =>
        {
            XC_DestoryInstance(ctrlPtrL);
            XC_DestoryInstance(ctrlPtrR);
        });
    }

    #endregion

    #region 公共控制 API

    /// <summary>
    /// 设置左侧 Xeryon 硬件位置 (逻辑值)。
    /// 仅在 VariFocal 模式关闭时 (curVariFocal == 0) 才发送命令。
    /// </summary>
    /// <param name="value">逻辑位置值</param>
    public async void SetXeryonL(int value)
    {
        if (curVariFocal == 0)
        {
            curXeryonL = -6000 + value * 20; // 映射到物理微米值
            await Task.Run(() => { XC_ISetDPOS(ctrlPtrL, curXeryonL); });
        }
    }

    /// <summary>
    /// 设置右侧 Xeryon 硬件位置 (逻辑值)。
    /// 仅在 VariFocal 模式关闭时 (curVariFocal == 0) 才发送命令。
    /// </summary>
    /// <param name="value">逻辑位置值</param>
    public async void SetXeryonR(int value)
    {
        if (curVariFocal == 0)
        {
            curXeryonR = -6000 + value * 20; // 映射到物理微米值
            await Task.Run(() => { XC_ISetDPOS(ctrlPtrR, curXeryonR); });
        }
    }

    /// <summary>
    /// 启用或禁用可变焦模式。
    /// 启用时 (enable != 0)，将阻止 SetXeryonL/R 向硬件写入新位置。
    /// </summary>
    /// <param name="enable">0=禁用 (允许写入), 非0=启用 (禁止写入)</param>
    public void SetVariFocal(int enable)
    {
        curVariFocal = enable;
    }

    #endregion

    #region 配置与状态管理 (Config/PlayerPrefs)

    /// <summary>
    /// 从 config.txt 加载硬件端口配置。
    /// </summary>
    private void LoadConfig()
    {
        Debug.Log("XeryonHardwareManager: LoadConfig START");
        try
        {
            string configFilePath = Path.Combine(path, configTxt);
            if (!File.Exists(configFilePath))
            {
                Debug.LogWarning("not have path " + configFilePath);
                return;
            }

            string[] txt = File.ReadAllLines(configFilePath, Encoding.UTF8);
            if (txt.Length > 0)
            {
                string[] temp = txt[0].Split(',');
                portL = int.Parse(temp[0]);
                portR = int.Parse(temp[1]);
                // 注意：原 ConfigSet 中的 focusTime 和 moveSpeed 已被移除，它们不属于本管理器
                Debug.Log("portL :" + portL + " portR :" + portR);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("ConfigSet error :" + e.ToString());
        }
        Debug.Log("XeryonHardwareManager: LoadConfig END");
    }

    /// <summary>
    /// 将当前硬件状态保存到 PlayerPrefs。
    /// </summary>
    public void SetSaveToPrefs()
    {
        PlayerPrefs.SetInt("curXeryonL", curXeryonL);
        PlayerPrefs.SetInt("curXeryonR", curXeryonR);
        PlayerPrefs.SetInt("curVariFocal", curVariFocal);
        // 注意：原 SetSave 中的 curFoveated 已被移除
        Debug.Log("XeryonHardwareManager: Settings Saved.");
    }

    /// <summary>
    /// 从 PlayerPrefs 加载硬件状态。
    /// </summary>
    private void LoadStateFromPrefs()
    {
        curXeryonL = PlayerPrefs.GetInt("curXeryonL");
        curXeryonR = PlayerPrefs.GetInt("curXeryonR");
        curVariFocal = PlayerPrefs.GetInt("curVariFocal");
        // 注意：原 SetLoad 中的 curFoveated 已被移除

        Debug.Log("XeryonHardwareManager: Load setting "
            + " curXeryonL | " + curXeryonL
            + " curXeryonR | " + curXeryonR
            + " curVariFocal | " + curVariFocal
            );
        
        // 实际应用这些值 (SetXeryonL/R) 的逻辑移至 Start()，
        // 因为必须在硬件初始化 (IStart) 之后才能调用。
    }

    #endregion
    

    #region XeryonControl (DLLImport 和 C# 封装)
    // ---------------------------------------------------------
    // 这部分是从 VACController.cs 中完整剪切并粘贴过来的
    // ---------------------------------------------------------

    public IntPtr XC_CreateInstance(int port)
    {
        IntPtr ctrlPtr = IntPtr.Zero;
        try
        {
            ctrlPtr = CreateInstance(port, 115200);
            DebugInfo("CreateInstance " + port);
        }
        catch (Exception e)
        {
            DebugInfo(e.ToString());
        }
        return ctrlPtr;
    }

    public void XC_DestoryInstance(IntPtr ctrlPtr)
    {
        try
        {
            if (ctrlPtr != null) DestoryInstance(ctrlPtr);
            DebugInfo("DestoryInstance " + ctrlPtr);
        }
        catch (Exception e)
        {
            DebugInfo(e.ToString());
        }
    }

    public void XC_IStart(IntPtr ctrlPtr)
    {
        try
        {
            if (ctrlPtr != null) IStart(ctrlPtr);
            DebugInfo("IStart " + ctrlPtr);
        }
        catch (Exception e)
        {
            DebugInfo(e.ToString());
        }
    }

    public void XC_IReset(IntPtr ctrlPtr)
    {
        try
        {
            if (ctrlPtr != null) IReset(ctrlPtr);
            DebugInfo("IReset " + ctrlPtr);
        }
        catch (Exception e)
        {
            DebugInfo(e.ToString());
        }
    }

    public void XC_IStop(IntPtr ctrlPtr)
    {
        try
        {
            if (ctrlPtr != null) IStop(ctrlPtr);
            DebugInfo("IStop " + ctrlPtr);
        }
        catch (Exception e)
        {
            DebugInfo(e.ToString());
        }
    }

    public void XC_IAddAxis(IntPtr ctrlPtr)
    {
        try
        {
            if (ctrlPtr != null) IAddAxis(ctrlPtr, _Direction.DIR_X);
            DebugInfo("IAddAxis " + ctrlPtr);
        }
        catch (Exception e)
        {
            DebugInfo(e.ToString());
        }
    }

    public void XC_ISetDPOS(IntPtr ctrlPtr, double pos)
    {
        try
        {
            if (ctrlPtr != null) ISetDPOS(ctrlPtr, _Direction.DIR_X, pos);
            DebugInfo("ISetDPOS " + pos + " " + ctrlPtr);
        }
        catch (Exception e)
        {
            DebugInfo(e.ToString());
        }
    }

    public double XC_IGetDPOS(IntPtr ctrlPtr)
    {
        double value = 0;
        try
        {
            if (ctrlPtr != null)
            {
                value = IGetDPOS(ctrlPtr, _Direction.DIR_X);
            }
            DebugInfo("IGetDPOS " + value);
        }
        catch (Exception e)
        {
            DebugInfo(e.ToString());
        }
        return value;
    }

    public void XC_IStep(IntPtr ctrlPtr, double step)
    {
        try
        {
            if (ctrlPtr != null) IStep(ctrlPtr, _Direction.DIR_X, step);
            DebugInfo("IStep");
        }
        catch (Exception e)
        {
            DebugInfo(e.ToString());
        }
    }

    //  ҪָںͲ
    [DllImport("XeryonControl", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr CreateInstance(int port, int baudrate);
    [DllImport("XeryonControl", CallingConvention = CallingConvention.Cdecl)]
    private static extern void DestoryInstance(IntPtr ctrlPtr);

    // ʼͨţȡļ, ҪIAddAxisٵãЧ
    [DllImport("XeryonControl", CallingConvention = CallingConvention.Cdecl)]
    private static extern void IStart(IntPtr ctrlPtr);

    // λã¶ȡļ
    [DllImport("XeryonControl", CallingConvention = CallingConvention.Cdecl)]
    private static extern void IReset(IntPtr ctrlPtr);

    // λãرͨ
    [DllImport("XeryonControl", CallingConvention = CallingConvention.Cdecl)]
    private static extern void IStop(IntPtr ctrlPtr);

    // һ,Ҫָ豸
    [DllImport("XeryonControl", CallingConvention = CallingConvention.Cdecl)]
    private static extern void IAddAxis(IntPtr ctrlPtr, _Direction dir, _Stage stage = _Stage.XLA_1250);

    // λãλΪum, Direction: X Y Z
    [DllImport("XeryonControl", CallingConvention = CallingConvention.Cdecl)]
    private static extern void ISetDPOS(IntPtr ctrlPtr, _Direction dir, double val_um);

    // ȡλãλΪum, Direction: X Y Z
    [DllImport("XeryonControl", CallingConvention = CallingConvention.Cdecl)]
    private static extern double IGetDPOS(IntPtr ctrlPtr, _Direction dir);

    // ƶһstepλãλΪum, Direction: X Y Z
    [DllImport("XeryonControl", CallingConvention = CallingConvention.Cdecl)]
    private static extern void IStep(IntPtr ctrlPtr, _Direction dir, double val_um);


    // ķ
    enum _Direction
    {
        DIR_X = 0,
        DIR_Y,
        DIR_Z,
    };

    // ƽ̨
    enum _Stage
    {
        XLS_312 = 0,
        XLS_1250,
        XLS_78,
        XLS_5,
        XLS_1,
        XLS_312_3N,
        XLS_1250_3N,
        XLS_78_3N,
        XLS_5_3N,
        XLS_1_3N,
        XLA_312,
        XLA_1250,
        XLA_78,
        XRTA,
        XRTU_30_109,
        XRTU_40_73,
        XRTU_40_3,
    };

    public static void DebugInfo(string info)
    {
        Debug.Log(info);
    }
    #endregion
}