// Ocean theme: the moving web of light on the sea floor (caustics). A transparent additive quad laid over the arena
// floor; procedural (no texture): two warped sine fields whose zero lines form the bright web, drifting slowly.
// Cheap for mobile: a handful of sin per pixel.
Shader "BrawlFortress/Caustics"
{
    Properties
    {
        _Color ("Light colour", Color) = (0.75, 0.95, 1.0, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0.35
        _Scale ("Scale (1 / metres)", Float) = 0.9
        _Speed ("Speed", Float) = 0.6
        _Sharp ("Sharpness", Range(1, 16)) = 7
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color; float _Intensity, _Scale, _Speed, _Sharp;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 wp : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.wp = mul(unity_ObjectToWorld, v.vertex).xyz; return o; }
            float web(float2 p, float t)
            {
                float a = sin(p.x + 1.3 * sin(p.y * 1.27 + t) + t * 0.7);
                float b = sin(p.y + 1.3 * sin(p.x * 1.13 - t * 0.8) - t * 0.5);
                return pow(saturate(1 - abs(a + b) * 0.5), _Sharp);
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float2 p = i.wp.xz * _Scale;
                float c = web(p, t) + 0.6 * web(p * 1.7 + 3.1, t * 1.3);
                return fixed4(_Color.rgb * c * _Intensity, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
