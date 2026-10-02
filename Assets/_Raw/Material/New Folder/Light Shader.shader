Shader "Custom/BlinkInvisibleMaskMobile"
{
    Properties
    {
        // Chỉnh màu sắc (Mặc định là trắng - không đổi màu)
        _Color ("Tint Color", Color) = (1,1,1,1)
        
        _MainTex ("Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture (Black & White)", 2D) = "white" {}
        _BlinkSpeed ("Blink Speed", Range(0.1, 30.0)) = 5.0 
        
        // Thanh chỉnh độ mờ tổng thể (0.0 là tàng hình hoàn toàn, 1.0 là rõ nhất)
        _Opacity ("Master Opacity", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST; 
            sampler2D _MaskTex; 
            
            // Khai báo biến
            fixed4 _Color;
            float _BlinkSpeed; 
            float _Opacity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex); 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 mask = tex2D(_MaskTex, i.uv);
                
                // 1. CHỈNH MÀU: Nhân màu của Texture với Tint Color
                col *= _Color;
                
                float blink = abs(sin(_Time.y * _BlinkSpeed));
                
                // 2. CẮT MASK: Dùng kênh Đỏ (Red) của ảnh đen trắng
                col.a *= mask.r; 
                
                // 3. NHẤP NHÁY: Tác động vào Alpha
                col.a *= blink;
                
                // 4. OPACITY: Nhân với độ mờ tổng thể
                col.a *= _Opacity;
                
                return col;
            }
            ENDCG
        }
    }
}