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

    float4 GaussianBlur_BLFilter(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord;
        uint2 centerCoord = input.positionCS.xy;

        // ---------------------------------------------------------
        // 1. 准备中心数据 (Center Data)
        // ---------------------------------------------------------
        // 深度
        float centerDepth = LoadCameraDepth(centerCoord);
        float centerLinearDepth = LinearEyeDepth(centerDepth, _ZBufferParams);
        
        // 颜色 (为了计算颜色差异，我们需要先拿到中心点的颜色)
        float3 centerColor = LOAD_TEXTURE2D_X(_MainTex, centerCoord).rgb;

        // 计算模糊强度
        float blurFactor = CacBlurFactor_V3(centerLinearDepth);

        // 【优化】早退
        if(blurFactor < 0.01)
        {
            return float4(centerColor, 1.0);
        }

        // ---------------------------------------------------------
        // 2. 参数设置
        // ---------------------------------------------------------
        float sigma = max(_Radius * 0.25, 0.5);
        int halfWidth = min((int)(sigma * 3.0), 10);  
        
        float3 finalColor = 0.0;
        float sumWeight = 0.0;

        // [参数] 深度敏感度：控制前景/背景分离
        float depthSensitivity = 0.0; 
        
        // [参数] 颜色敏感度：控制是否保留纹理边缘
        // 建议值：
        // 0.0 = 不考虑颜色差异 (标准物理虚化)
        // 1.0 ~ 10.0 = 抑制高光溢出，轻微保留边缘
        // > 20.0 = 强烈的油画/磨皮效果，纹理内部不会糊
        float colorSensitivity = 0.0; 

        // ---------------------------------------------------------
        // 3. 联合双边滤波循环 (Joint Bilateral Loop)
        // ---------------------------------------------------------
        for (int dy = -halfWidth; dy <= halfWidth; ++dy)
        {
            for (int dx = -halfWidth; dx <= halfWidth; ++dx)
            {
                float2 offset = float2(dx, dy) * _MainTex_TexelSize.xy;
                uint2 sampleCoords = uint2((uv + offset) * _ScreenSize.xy);

                // A. 【空间权重】 (Spatial Weight) - 高斯分布
                float distSq = dx*dx + dy*dy;
                float spatialWeight = exp(-distSq / (2.0 * sigma * sigma));

                // 获取采样点数据
                float sampleDepth = LoadCameraDepth(sampleCoords);
                float sampleLinearDepth = LinearEyeDepth(sampleDepth, _ZBufferParams);
                float3 sampleColor = LOAD_TEXTURE2D_X(_MainTex, sampleCoords).rgb;

                // B. 【深度权重】 (Depth Weight) - 拒绝不同深度的像素
                float depthDiff = abs(centerLinearDepth - sampleLinearDepth);
                // 使用 exp 衰减会让边缘切割得更锐利
                float depthWeight = exp(-depthDiff * depthSensitivity);
                // 或者使用更温和的: 1.0 / (1.0 + depthDiff * depthSensitivity);

                // C. 【颜色权重】 (Color Weight) - 拒绝不同颜色的像素
                float3 colorDiffVec = centerColor - sampleColor;
                // 计算颜色距离的平方 (R^2 + G^2 + B^2)
                float colorDiffSq = dot(colorDiffVec, colorDiffVec); 
                // 颜色差异越大，权重越小
                float colorWeight = exp(-colorDiffSq * colorSensitivity);

                // D. 【综合权重】
                float totalWeight = spatialWeight * depthWeight * colorWeight;

                // 累加
                finalColor += sampleColor * totalWeight;
                sumWeight += totalWeight;
            }
        }

        // 归一化
        if (sumWeight > 0.0001)
            finalColor /= sumWeight;
        else
            finalColor = centerColor; // 容错：如果所有邻居都被拒绝了，保持原样
        
        // ---------------------------------------------------------
        // 4. 最终混合
        // ---------------------------------------------------------
        return float4(lerp(centerColor, finalColor, blurFactor), 1.0);
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
            #pragma fragment GaussianBlur
            ENDHLSL
        }
    }
    Fallback Off
}