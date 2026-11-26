// Savanna theme water hole (it heals whoever stands in it: Assets/Scripts/Map/WaterHole.cs). Shallow muddy-green water
// at the edge, deeper blue-green in the middle (vertex alpha: 1 in the middle, 0 at the rim), slow bands of sky
// reflection, rings spreading from drops on a hashed grid, a band of wet mud along the edge, and small pale-green "+"
// glints twinkling on the surface: the healing reads at a glance. Unlit, opaque, cheap for mobile.
Shader "BrawlFortress/Pond"
{
    Properties
    {
        _Shallow ("Shallow water", Color) = (0.46, 0.62, 0.48, 1)
        _Deep ("Deep water", Color) = (0.20, 0.44, 0.50, 1)
        _Sky ("Sky reflection", Color) = (0.78, 0.90, 0.96, 1)
        _Mud ("Wet mud edge", Color) = (0.52, 0.36, 0.22, 1)
        _RimWidth ("Edge width (vertex alpha)", Range(0.02, 0.6)) = 0.2
        _Ripples ("Ripple cells per metre", Float) = 0.8
        _Glint ("Heal glints", Color) = (0.80, 1.0, 0.78, 1)
        _Glints ("Glint cells per metre", Float) = 1.1
        _GlintStrength ("Glint strength", Range(0, 1.5)) = 0.9
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            fixed4 _Shallow, _Deep, _Sky, _Mud, _Glint;
            float _RimWidth, _Ripples, _Glints, _GlintStrength;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float3 wp : TEXCOORD0; fixed edge : TEXCOORD1; UNITY_FOG_COORDS(2) };
            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.edge = v.color.a;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                half e = i.edge;
                fixed3 c = lerp(_Shallow.rgb, _Deep.rgb, smoothstep(0.15, 0.85, e));
                // slow bands of sky reflection drifting across
                float2 w = i.wp.xz;
                float band = sin(w.x * 0.9 + w.y * 0.55 + t * 0.5 + sin(w.y * 1.7 + t * 0.3) * 0.8);
                c = lerp(c, _Sky.rgb, smoothstep(0.75, 1.0, band) * 0.35);
                // rings spreading from drops: one per cell at most, at a random moment
                float2 g = w * _Ripples; float2 cell = floor(g); float2 f = frac(g) - 0.5;
                float h = hash(cell);
                float2 off = (float2(hash(cell + 3.1), hash(cell + 7.7)) - 0.5) * 0.5;
                float ph = frac(t * (0.25 + 0.1 * h) + h * 7.0);
                float r = length(f - off);
                float ring = (1 - smoothstep(0.0, 0.03, abs(r - ph * 0.42))) * (1 - ph) * step(h, 0.7);
                c = lerp(c, _Sky.rgb, saturate(ring * 0.8));
                // healing: small "+" glints fading in and out on their own grid
                float2 gg = w * _Glints + 0.37; float2 gc = floor(gg); float2 gf = frac(gg) - 0.5;
                float gh = hash(gc + 11.3);
                float2 go = (float2(hash(gc + 5.9), hash(gc + 2.4)) - 0.5) * 0.45;
                float2 q = abs(gf - go);
                float plus = max(step(q.x, 0.022) * step(q.y, 0.085), step(q.y, 0.022) * step(q.x, 0.085));
                float tw = saturate(sin((t * (0.7 + 0.5 * gh) + gh * 6.28) * 3.14159));
                c = lerp(c, _Glint.rgb, plus * tw * tw * _GlintStrength * smoothstep(0.1, 0.4, e));
                // edge: a band of wet mud, a darker wet line just inside it
                c = lerp(c, _Mud.rgb, 1 - smoothstep(0.0, _RimWidth, e));
                c *= 1 - 0.18 * (smoothstep(_RimWidth, _RimWidth * 1.5, e) * (1 - smoothstep(_RimWidth * 1.5, _RimWidth * 2.6, e)));
                fixed4 col = fixed4(c, 1);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
