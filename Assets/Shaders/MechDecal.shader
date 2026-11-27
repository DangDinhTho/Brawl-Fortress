// Ground marks of the map mechanics (MapMechanics.cs, MechMesh.cs): gift rings, cannon landing targets, dart-trap
// warning stripes, meteor craters. Unlit, flat, alpha blended: vertex colour x _Color. uv.x runs 0..1 around a ring or
// along a stripe and _Fill shows that share at full strength (the rest faint): a ring fills up as a gift comes back, a
// stripe fills along the dart path. _Pulse > 0 makes it breathe. No texture.
Shader "BrawlFortress/MechDecal"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 1, 1, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Fill ("Fill 0..1 (uv.x)", Range(0, 1)) = 1
        _Faint ("Alpha of the unfilled part", Range(0, 1)) = 0.25
        _Pulse ("Pulse speed (0 = off)", Float) = 0
        _Glow ("Brightness", Range(0.5, 3)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-7" "RenderType" = "Transparent" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -2, -2
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            // float only (no half): half with step() / comparisons does not compile for Metal / GLES3 / Vulkan
            float4 _Color;
            float _Alpha, _Fill, _Faint, _Pulse, _Glow;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }
            float4 frag (v2f i) : SV_Target
            {
                float4 c = i.color * _Color;
                float filled = step(i.uv.x, _Fill + 0.0001);
                float a = c.a * lerp(_Faint, 1.0, filled);
                float pulse = 0.72 + 0.28 * sin(_Time.y * _Pulse);
                a *= lerp(1.0, pulse, step(0.001, _Pulse));
                return float4(c.rgb * _Glow, saturate(a * _Alpha));
            }
            ENDCG
        }
    }
    Fallback Off
}
