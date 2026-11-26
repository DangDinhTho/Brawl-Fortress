// A black space sky full of stars (Lunar theme): procedural, no texture. Skybox shader: set as RenderSettings.skybox.
// Stars are hashed on a grid over the view direction, a few brighter ones; a faint blue glow towards the horizon.
Shader "BrawlFortress/StarSky"
{
    Properties
    {
        _Top ("Sky", Color) = (0.01, 0.01, 0.03, 1)
        _Horizon ("Horizon glow", Color) = (0.06, 0.08, 0.16, 1)
        _Density ("Star cells", Float) = 140
        _Amount ("Star amount", Range(0, 1)) = 0.06
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Top, _Horizon;
            float _Density, _Amount;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.dir = v.vertex.xyz; return o; }
            float hash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                fixed3 c = lerp(_Horizon.rgb, _Top.rgb, saturate(d.y * 2.5));
                float3 g = d * _Density; float3 cell = floor(g); float3 f = frac(g) - 0.5;
                float h = hash(cell);
                if (h < _Amount)
                {
                    float r = length(f);
                    float b = (1 - smoothstep(0.05, 0.20 + 0.15 * h / _Amount, r)) * (0.5 + 0.5 * hash(cell + 7.1));
                    c += b.xxx * float3(1.0, 0.97, 0.9);
                }
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
