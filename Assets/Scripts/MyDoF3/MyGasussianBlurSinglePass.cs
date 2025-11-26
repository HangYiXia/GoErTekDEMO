using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[System.Serializable, VolumeComponentMenu("Post-processing/Custom/My Gaussian Blur (Single Pass)")]
public sealed class MyGaussianBlurSinglePass : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("模糊半径（像素），值越大，模糊程度越高。")]
    public ClampedFloatParameter radius = new ClampedFloatParameter(10f, 0f, 60f);
    
    /*
    [Tooltip("近景模糊开始的距离。")]
    public MinFloatParameter nearBlurStart = new MinFloatParameter(0.1f, 0f);

    [Tooltip("近景模糊最强的距离（在此距离内模糊达到最大）。")]
    public MinFloatParameter nearBlurEnd = new MinFloatParameter(5f, 0f);

    [Tooltip("远景模糊开始的距离。")]
    public MinFloatParameter farBlurStart = new MinFloatParameter(20f, 0f);

    [Tooltip("远景模糊最强的距离（超过此距离模糊达到最大）。")]
    public MinFloatParameter farBlurEnd = new MinFloatParameter(50f, 0f);
    */
    
    [Tooltip("对焦距离")]
    public MinFloatParameter focalLength = new MinFloatParameter(10f, 0f);
    
    [Tooltip("镜头焦距")]
    public MinFloatParameter f = new MinFloatParameter(0.017f, 0f);
    
    [Tooltip("光圈直径")]
    public MinFloatParameter A = new MinFloatParameter(0.002f, 0f);
    
    [Tooltip("最大弥散圆直径")]
    public MinFloatParameter maxCoCsize = new MinFloatParameter(120f, 0f);
    
    public BoolParameter enabled = new BoolParameter(true);


    public bool IsActive() => m_Material != null &&
                              radius.value > 0;

    public override CustomPostProcessInjectionPoint injectionPoint =>
        CustomPostProcessInjectionPoint.AfterPostProcess;

    private Material m_Material;

    public override void Setup()
    {
        m_Material = CoreUtils.CreateEngineMaterial("Hidden/Shader/GaussianBlurSinglePass");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle src, RTHandle dest)
    {
        using (new ProfilingScope(cmd, new ProfilingSampler("My Gaussian Blur (Single Pass)"))) ;
        if (m_Material == null)
        {
            HDUtils.BlitCameraTexture(cmd, src, dest);
            return;
        }

        m_Material.SetFloat("_Radius", radius.value);
        
        /*
        m_Material.SetFloat("_NearStart", nearBlurStart.value);
        m_Material.SetFloat("_NearEnd", nearBlurEnd.value);
        m_Material.SetFloat("_FarStart", farBlurStart.value);
        m_Material.SetFloat("_FarEnd", farBlurEnd.value);
        */
        
        m_Material.SetFloat("_F", focalLength.value);
        m_Material.SetFloat("_A", A.value);
        m_Material.SetFloat("_f", f.value);
        m_Material.SetFloat("_MaxCocSize", maxCoCsize.value);
        
        m_Material.SetTexture("_MainTex", src);

        Debug.Log("Is Active");
        HDUtils.DrawFullScreen(cmd, m_Material, dest);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}