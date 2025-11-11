Shader "Custom/Gexiaobao/Outline Shader"
{
    Properties {
        [Header(Outline)]
        [Space(20)]
        _OutlineColor ("Outline Color", Color) = (0.0, 0.0, 0.0, 0.0)
        _OutlineWidth ("Outline Width", Range(0.0, 10.0)) = 1.0
    }

    SubShader {
        Pass {
            // NAME "OUTLINE"
            // Tags { "LightMode" = "Forward" }

            Cull Front

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            half4 _OutlineColor;
            float _OutlineWidth;
            
            struct a2v
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(a2v v) {
                v2f o;

                // float3 pos = TransformWorldToView(TransformObjectToWorld(v.vertex));
                // float3 normal = TransformWorldToView(TransformObjectToWorld(v.normal));
                float3 pos = TransformObjectToWorld(v.vertex);
                float3 normal = TransformObjectToWorld(v.normal);

                //normal.z = -0.5;
                pos = pos + _OutlineWidth * normalize(normal) * 0.01;

                // o.pos = mul(UNITY_MATRIX_P, pos);
                o.pos = TransformWorldToHClip(pos);
                // o.pos = TransformObjectToHClip(v.vertex);

                return o;
            }

            half4 frag(v2f i) : SV_Target {
                return half4(_OutlineColor.rgb, 1.0f);
            }

            ENDHLSL
        }
    }
}
