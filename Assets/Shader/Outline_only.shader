Shader "Custom/OutlineOnly"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness("Thickness", Float) = 0.02
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull Front        // 외곽선 표현을 위해 내부를 버리고 겉면만 그리기
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float _OutlineThickness;
            float4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 n = normalize(v.normal);
                v.vertex.xyz += n * _OutlineThickness;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
        FallBack Off
}