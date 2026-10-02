// Vòng tiến trình của zone: _Fill 0..1 chạy theo chiều kim đồng hồ từ đỉnh, phần chưa fill mờ theo _BgAlpha
Shader "Halloween/ZoneProgress"
{
    Properties
    {
        _MainTex ("Ring", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1, 0.5, 0.1, 1)
        _Fill ("Fill", Range(0, 1)) = 0
        _BgAlpha ("Background Alpha", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "ZoneProgress"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Fill;
                half _BgAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 d = i.uv - 0.5;
                float ang = frac(atan2(d.x, d.y) / (2.0 * PI) + 1.0);
                half mask = lerp(_BgAlpha, 1.0, step(ang, _Fill));
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                return half4(_Color.rgb, a * _Color.a * mask);
            }
            ENDHLSL
        }
    }
}
