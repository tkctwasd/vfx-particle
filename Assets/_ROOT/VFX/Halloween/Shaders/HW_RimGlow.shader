// Viền phát sáng Fresnel, gắn thêm làm material phụ lên renderer của animal (additive, không ghi depth)
Shader "Halloween/RimGlow"
{
    Properties
    {
        [HDR] _RimColor ("Rim Color", Color) = (1, 0.5, 0.1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _Intensity ("Intensity", Range(0, 5)) = 1.5
        _PulseSpeed ("Pulse Speed", Float) = 2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "RimGlow"
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _RimColor;
                half _RimPower;
                half _Intensity;
                half _PulseSpeed;
                half _PulseAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewDirWS = GetWorldSpaceViewDir(p.positionWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 vd = normalize(i.viewDirWS);
                half rim = pow(1.0 - saturate(dot(n, vd)), _RimPower);
                half pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                return half4(_RimColor.rgb * rim * _Intensity * pulse, 1);
            }
            ENDHLSL
        }
    }
}
