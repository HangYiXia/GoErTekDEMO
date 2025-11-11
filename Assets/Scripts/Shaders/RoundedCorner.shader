Shader "Custom/RoundedCorner" {
    Properties{
        _MainTex("Texture", 2D) = "white" {}
        _Radius("Corner Radius", Range(0, 0.5)) = 0.1
    }

        SubShader{
            Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
            Blend SrcAlpha OneMinusSrcAlpha

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f {
                    float2 uv : TEXCOORD0;
                    UNITY_FOG_COORDS(1)
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                float _Radius;

                v2f vert(appdata v) {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target{
                    float2 center = float2(0.5, 0.5);
                    float2 center00 = float2(_Radius, _Radius);
                    float2 center01 = float2(_Radius, 1 - _Radius);
                    float2 center10 = float2(1 - _Radius, _Radius);
                    float2 center11 = float2(1 - _Radius, 1 - _Radius);
                    float dist = min(min(length(i.uv - center00), length(i.uv - center01)), min(length(i.uv - center10), length(i.uv - center11))) - _Radius;
                    float d = length(center - float2(0, _Radius));
                    dist = min(dist, length(i.uv - center) - d);
                    float alpha = step(dist, 0);
                    fixed4 color = tex2D(_MainTex, i.uv);
                    color.a *= alpha;
                    return color;
                }
                ENDCG
            }
        }
}