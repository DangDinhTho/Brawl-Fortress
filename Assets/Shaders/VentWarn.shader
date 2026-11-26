// Warning disc of an eruption vent (EruptionVent.cs): drawn flat on the ground over the area an eruption will hit.
// A bright rim marks the edge; the inside fills from the centre outward as the eruption comes (_Fill 0..1), with a
// hot leading edge, so players can read both where and when. Procedural, no texture. uv -1..1 over the quad.
Shader "BrawlFortress/VentWarn"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 0.25, 0.08, 1)
        _Edge ("Leading edge colour", Color) = (1, 0.85, 0.35, 1)
        _Fill ("Fill 0..1", Range(0, 1)) = 0
        _Alpha ("Alpha", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-8" "RenderType" = "Transparent" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
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
            // float, not half: with half the step() comparisons failed to compile for Metal / GLES3 / Vulkan
            // ("Incompatible min precision type ... Expected int or uint"): the disc was pink on phones (fine in the
            // D3D11 editor)
            float4 _Color, _Edge;
            float _Fill, _Alpha;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv * 2 - 1; return o; }
            float4 frag (v2f i) : SV_Target
            {
                float d = length(i.uv);
                float aa = max(fwidth(d), 1e-3);
                float inside = 1 - smoothstep(0.96 - aa, 0.96 + aa, d);
                float rim = (1 - smoothstep(0.075 - aa, 0.075 + aa, abs(d - 0.90))) * inside;
                // stripes rotating slowly inside the rim: reads as a hazard marking, not as a team ring
                float ang = atan2(i.uv.y, i.uv.x) / 6.2831853;
                float stripes = step(0.5, frac(ang * 12 + _Time.y * 0.6)) * 0.12;
                float filled = 1 - smoothstep(_Fill - aa, _Fill + aa, d);
                float lead = (1 - smoothstep(0.0, 0.06, abs(d - _Fill))) * step(0.02, _Fill) * step(_Fill, 0.995);
                float a = rim * 0.95 + (filled * 0.42 + stripes) * inside + lead * 0.9 * inside;
                float3 c = lerp(_Color.rgb, _Edge.rgb, saturate(lead + rim * 0.35));
                return float4(c, saturate(a) * _Alpha * _Color.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
