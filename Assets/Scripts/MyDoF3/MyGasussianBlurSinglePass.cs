using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[System.Serializable, VolumeComponentMenu("Post-processing/Custom/My Gaussian Blur (Single Pass)")]
public sealed class MyGaussianBlurSinglePass : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("模糊半径（像素），值越大，模糊程度越高。")]
    public ClampedFloatParameter radius = new ClampedFloatParameter(10f, 0f, 60f);
    
    [Tooltip("近景模糊开始的距离。")]
    public MinFloatParameter nearBlurStart = new MinFloatParameter(0.1f, 0f);

    [Tooltip("近景模糊最强的距离（在此距离内模糊达到最大）。")]
    public MinFloatParameter nearBlurEnd = new MinFloatParameter(5f, 0f);

    [Tooltip("远景模糊开始的距离。")]
    public MinFloatParameter farBlurStart = new MinFloatParameter(20f, 0f);

    [Tooltip("远景模糊最强的距离（超过此距离模糊达到最大）。")]
    public MinFloatParameter farBlurEnd = new MinFloatParameter(50f, 0f);
    
    public BoolParameter enabled = new BoolParameter(true);
    

    public bool IsActive() => m_Material != null && 
                              radius.value > 0 && 
                              nearBlurEnd.value > nearBlurStart.value && 
                              farBlurStart.value < farBlurEnd.value && 
                              nearBlurEnd.value < farBlurStart.value &&
                              enabled.value;

    public override CustomPostProcessInjectionPoint injectionPoint =>
        CustomPostProcessInjectionPoint.AfterPostProcess;

    private Material m_Material;

    public override void Setup()
    {
        m_Material = CoreUtils.CreateEngineMaterial("Hidden/Shader/GaussianBlurSinglePass");
    }

public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle src, RTHandle dest)
{
    // 确保 using 语句后有 {
    using (new ProfilingScope(cmd, new ProfilingSampler("My Gaussian Blur (Two Pass)")))
    {
        if (m_Material == null)
        {
            HDUtils.BlitCameraTexture(cmd, src, dest);
            return;
        }

        // --- 1. 设置通用参数 ---
        m_Material.SetFloat("_Radius", radius.value);
        m_Material.SetFloat("_NearStart", nearBlurStart.value);
        m_Material.SetFloat("_NearEnd", nearBlurEnd.value);
        m_Material.SetFloat("_FarStart", farBlurStart.value);
        m_Material.SetFloat("_FarEnd", farBlurEnd.value);

        // --- 2. 分配临时 RT ---
        // 【关键修正】: 必须使用 src.descriptor, 不能用 src.rt.descriptor
        var desc = src.rt.descriptor;
        desc.depthBufferBits = 0;
        RTHandle tempRT = RTHandles.Alloc(desc, filterMode: FilterMode.Bilinear, name: "GaussianBlurTempRT");

        // --- 3. Pass 0 (Horizontal) [src -> tempRT] ---
        // 【关键修正】: 必须使用 src.referenceSize, 不能用 src.rt.width
        var srcSize = src.referenceSize;
        m_Material.SetVector("_MainTex_TexelSize", new Vector4(1.0f / srcSize.x, 1.0f / srcSize.y, srcSize.x, srcSize.y));
        m_Material.SetTexture("_MainTex", src);
        
        // (已移除 _Direction 设置，因为 Shader Pass 0 (GaussianBlurH) 已硬编码方向)
        HDUtils.DrawFullScreen(cmd, m_Material, tempRT, null, 0); // Pass 0 -> tempRT

        // --- 4. Pass 1 (Vertical) [tempRT -> dest] ---
        // 【关键修正】: 必须更新 _MainTex_TexelSize 以匹配新的输入 (tempRT)
        var tempSize = tempRT.referenceSize;
        m_Material.SetVector("_MainTex_TexelSize", new Vector4(1.0f / tempSize.x, 1.0f / tempSize.y, tempSize.x, tempSize.y));
        
        // 【关键修正】: Pass 1 的输入必须是 tempRT
        m_Material.SetTexture("_MainTex", src); 
        
        // (已移除 _Direction 设置，因为 Shader Pass 1 (GaussianBlurV) 已硬编码方向)
        HDUtils.DrawFullScreen(cmd, m_Material, dest, null, 1); // Pass 1 -> dest

        // --- 5. 清理 ---
        RTHandles.Release(tempRT);
    } 
}

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}