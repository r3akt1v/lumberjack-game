Shader "Custom/GrassWind"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.4,0.6,0.2,1)
        _WindStrength ("Wind Strength", Float) = 0.15
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        CGPROGRAM
        #pragma surface surf Standard vertex:vert fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float3 color : COLOR;
        };

        half _WindStrength;
        fixed4 _BaseColor;

        void vert(inout appdata_full v)
        {
            float sway = sin(_Time.y * 2.0 + v.vertex.xyz.x * 0.5 + v.vertex.xyz.z * 0.3) * _WindStrength * v.color.g;
            // sway more at top: use uv.y approx via vertex height (y)
            sway *= saturate(v.vertex.xyz.y * 2.0);
            v.vertex.xyz.x += sway;
            v.vertex.xyz.z += sway * 0.6;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = _BaseColor * IN.color;
            o.Albedo = c.rgb;
            o.Smoothness = 0.1;
            o.Metallic = 0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
