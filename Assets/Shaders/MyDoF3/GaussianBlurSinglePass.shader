Shader "Hidden/Shader/GaussianBlurSinglePass"
{
    Properties
    {
        _MainTex ("Main Texture", 2DArray) = "white" {}
        _Radius ("Blur Radius", Range(0, 60)) = 3
    }

    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 d3d12 vulkan metal
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    TEXTURE2D_X(_MainTex);
    
    float4 _MainTex_TexelSize;
    float _Radius;
    
    float _NearStart;
    float _NearEnd;
    float _FarStart;
    float _FarEnd;

    
    float _F; // 对焦距离
    float _f; // 镜头焦距
    float _A; // 镜头光圈直径
    float _MaxCocSize; // 最大CoC直径
    

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    float CacBlurFactor(float linearEyeDepth)
    {
        // 近景
        if(linearEyeDepth >= _NearStart && linearEyeDepth <= _NearEnd)
        {
            return 1.0;
            //return 1.0 - smoothstep(_NearStart, _NearEnd, linearEyeDepth);
        }

        // 远景
        if(linearEyeDepth >= _FarStart && linearEyeDepth <= _FarEnd)
        {
            return 1.0;
            //return smoothstep(_FarStart, _FarEnd, linearEyeDepth);
        }
        
        return 0.0;
    }

    float CacBlurFactor_V2(float linearEyeDepth)
    {
        // 计算近景模糊因子
        // smoothstep(_NearEnd, _NearStart, depth) 的意思是：
        // 当 depth <= _NearStart 时, 结果为 1.0 (完全模糊)
        // 当 depth >= _NearEnd 时,   结果为 0.0 (清晰)
        // 在两者之间平滑插值
        float nearFactor = smoothstep(_NearEnd, _NearStart, linearEyeDepth);

        // 计算远景模糊因子
        // smoothstep(_FarStart, _FarEnd, depth) 的意思是：
        // 当 depth <= _FarStart 时, 结果为 0.0 (清晰)
        // 当 depth >= _FarEnd 时,   结果为 1.0 (完全模糊)
        // 在两者之间平滑插值
        float farFactor = smoothstep(_FarStart, _FarEnd, linearEyeDepth);

        // 将两个因子合并。因为清晰区域nearFactor和farFactor都为0，所以取最大值即可。
        return max(nearFactor, farFactor);
    }

    float CacCoCSize(float linearEyeDepth)
    {
        return abs((linearEyeDepth - _F) / _F) * _A * _f;
    }

    bool FloatEqual(float a, float b)
    {
        return abs(a - b) < 1e-5f;
    }

    float CacCoCSize_V2(float linearEyeDepth)
    {
        if (linearEyeDepth <= _F)
        {
            return CacCoCSize(linearEyeDepth);
        }
        float delta = 10.0; // 建议将此变量提取为 Shader Property，方便在面板调节

        // 1. 计算当前深度与对焦距离的绝对差值
        float dist = linearEyeDepth - _F;

        // 2. 引入死区 (Dead Zone)
        // 如果 dist 小于 delta，结果为 0 (完全清晰)
        // 如果 dist 大于 delta，结果从 0 开始线性增加
        // 这样就形成了一个 flat 的底部： \__/ 形伏
        float effectiveDist = max(0.0, dist - delta);

        // 3. 代入原有公式
        // 注意：原公式是 abs(depth - F) / F ...
        // 现在我们用 effectiveDist 替换 abs(depth - F)
        return (effectiveDist / _F) * _A * _f;
    }

    float CacAlphaByCoCSize(float cocSize)
    {
        return saturate(cocSize / _MaxCocSize);
    }

    float CacBlurFactor_V3(float linearEyeDepth)
    {
        float cocSize = CacCoCSize(linearEyeDepth);
        return CacAlphaByCoCSize(cocSize);
    }

    float4 GaussianBlur(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord;

        float sigma = max(_Radius * 0.25, 0.5);
        int halfWidth = min((int)(sigma * 3.0), 10);  
        float3 blurColor = 0.0;
        float sum = 0.0;

        
        // --- 计算模糊因子 ---
        float depth = LoadCameraDepth(input.positionCS.xy);
        bool isSky = FloatEqual(depth , 0.0f);
        float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
        float blurFactor = isSky ? 0.0f : CacBlurFactor_V3(linearDepth);
        
        for (int dy = -halfWidth; dy <= halfWidth; ++dy)
        {
            for (int dx = -halfWidth; dx <= halfWidth; ++dx)
            {
                float2 offset = float2(dx, dy) * _MainTex_TexelSize.xy;
                float w = exp(-(dx*dx + dy*dy) / (2.0 * sigma * sigma));
                blurColor += LOAD_TEXTURE2D_X(_MainTex, uint2((uv + offset) * _ScreenSize.xy)).rgb * w;
                sum += w;
            }
        }
        blurColor /= sum;
        
        
        
        float3 oriColor = LOAD_TEXTURE2D_X(_MainTex, input.texcoord * _ScreenSize.xy).rgb;
        
        /*
        // --- 清晰mask ---
        float mask = 0.0f;
        if(linearDepth >= _NearEnd && linearDepth <= _FarStart)
            mask = 1.0f;
        */

        /*
        // --- 近景mask ---
        float mask = 0.0f;
        if(linearDepth <= _NearEnd)
            mask = 1.0f;
        */

        /*
        // --- 远景mask ---
        float mask = 0.0f;
        if(linearDepth >= _FarStart)
            mask = 1.0f;
        */

        /*
        // --- 可视化深度 ---
        float linear01Depth = Linear01Depth(depth, _ZBufferParams);
        return float4(linear01Depth, linear01Depth, linear01Depth, 1.0);
        */
        
        return float4(lerp(oriColor, blurColor, blurFactor), 1.0);
    }

    float4 GaussianBlur_V2(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord;

        float sigma = max(_Radius * 0.25, 0.5);
        int halfWidth = min((int)(sigma * 3.0), 10);  
        float3 blurColor = 0.0;
        float sum = 0.0;

        // --- 1. 获取中心点信息 (提前获取oriColor用于对比) ---
        float depth = LoadCameraDepth(input.positionCS.xy);
        float3 oriColor = LOAD_TEXTURE2D_X(_MainTex, input.texcoord * _ScreenSize.xy).rgb;
        
        // 线性深度转化
        float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
        bool isSky = FloatEqual(depth, 0.0f); // 注意：反向Z一般Sky是0，非反向Z可能是1，这里沿用你的逻辑
        
        // --- 2. 计算模糊混合因子 ---
        // 修改点：如果是Sky，我们强制给一个模糊强度（例如1.0或自定义），否则原逻辑是0导致看不见模糊
        // 如果你希望天空完全使用模糊结果，这里设为 1.0
        float blurFactor = isSky ? 1.0f : CacBlurFactor_V3(linearDepth);

        // 定义双边滤波的敏感度参数 (可以提升为Uniform变量)
        // 深度差异敏感度：值越小，对深度差异越敏感（越不容易跨越深度边缘模糊）
        float sigmaDepth = 10.0f; 
        // 颜色差异敏感度：值越小，对颜色差异越敏感
        float sigmaColor = 0.2f;  

        for (int dy = -halfWidth; dy <= halfWidth; ++dy)
        {
            for (int dx = -halfWidth; dx <= halfWidth; ++dx)
            {
                // 计算偏移坐标
                float2 offsetUV = float2(dx, dy) * _MainTex_TexelSize.xy;
                uint2 sampleCoord = uint2((uv + offsetUV) * _ScreenSize.xy);
                
                // 采样邻域颜色
                float3 sampleColor = LOAD_TEXTURE2D_X(_MainTex, sampleCoord).rgb;

                // A. 基础高斯空间权重 (Spatial Weight)
                float w = exp(-(dx*dx + dy*dy) / (2.0 * sigma * sigma));

                // B. 仅当 isSky 为 true 时，考虑颜色和深度差异 (Range Weight)
                if (isSky)
                {
                    // 1. 采样邻域深度
                    // 注意：需要根据当前像素坐标偏移去采深度
                    float sampleDepthRaw = LoadCameraDepth(input.positionCS.xy + float2(dx, dy));
                    float sampleLinearDepth = LinearEyeDepth(sampleDepthRaw, _ZBufferParams);

                    // 2. 计算差异
                    float depthDiff = abs(linearDepth - sampleLinearDepth);
                    float colorDiff = length(oriColor - sampleColor);

                    // 3. 计算双边权重 (高斯分布)
                    // 深度权重：差异越大，权重越趋近0
                    float w_depth = exp(-(depthDiff * depthDiff) / (2.0 * sigmaDepth * sigmaDepth));
                    // 颜色权重：差异越大，权重越趋近0
                    float w_color = exp(-(colorDiff * colorDiff) / (2.0 * sigmaColor * sigmaColor));

                    // 叠加权重
                    w *= w_depth * w_color;
                }

                blurColor += sampleColor * w;
                sum += w;
            }
        }
        
        // 防止除以0 (极小概率sum为0)
        blurColor /= max(sum, 0.0001f);

        return float4(lerp(oriColor, blurColor, blurFactor), 1.0);
    }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment GaussianBlur_V2
            ENDHLSL
        }
    }
    Fallback Off
}