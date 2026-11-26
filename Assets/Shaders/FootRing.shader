// Brawl-Stars-style team circle under a character's feet (FootRing.cs) and under Engineer buildings (BuildableMarker.cs).
// Procedural, no texture. uv in -1..1 over the quad; the circle's edge is at _RingR.
// A translucent team-coloured disc that is strongest at its edge and fades towards the centre (pow falloff), with a thin
// light rim on the edge and a soft anti-aliased border. No dark outline (Tho: Brawl Stars has none).
// Optional dashes on the rim (hidden in a bush, sentry range), reload arc just outside (off) and glow (healed / boss).
Shader "BrawlFortress/FootRing"
{
    Properties
    {
        _Color ("Colour (a = overall alpha)", Color) = (0.3, 1, 0.3, 1)
        _FillAlpha ("Fill alpha at the edge", Range(0, 1)) = 0.55
        _CenterFill ("Fill alpha at the centre (share of the edge)", Range(0, 1)) = 0.04
        _Falloff ("Fill falloff (higher = thinner gradient)", Range(0.5, 6)) = 1.7
        _RingR ("Edge radius (uv)", Range(0.2, 1)) = 0.72
        _Width ("Rim width (uv)", Range(0.005, 0.4)) = 0.08
        _RimAlpha ("Rim alpha", Range(0, 1)) = 0.95
        _Outline ("(unused, kept for old callers)", Range(0, 0.15)) = 0
        _OutlineColor ("(unused)", Color) = (0, 0, 0, 0)
        _Dashed ("Dashed (0/1)", Float) = 0
        _DashCount ("Dash count", Float) = 14
        _Progress ("Reload arc 0..1 (<0 = off)", Float) = -1
        _ArcColor ("Reload arc colour", Color) = (1, 1, 1, 1)
        _Glow ("Glow 0..1", Range(0, 1)) = 0
        _GlowColor ("Glow colour", Color) = (0.5, 1, 0.6, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-4" "RenderType" = "Transparent" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color, _ArcColor, _GlowColor;
            half _FillAlpha, _CenterFill, _Falloff, _RingR, _Width, _RimAlpha, _Dashed, _DashCount, _Progress, _Glow;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv * 2 - 1; return o; }

            half band(half d, half r, half w, half aa) { return 1 - smoothstep(w * 0.5 - aa, w * 0.5 + aa, abs(d - r)); }

            // "over" blend of a colour with coverage a onto col
            void over(inout fixed4 col, fixed3 rgb, half a)
            {
                col.rgb = lerp(col.rgb, rgb, a / max(a + col.a * (1 - a), 1e-4));
                col.a = a + col.a * (1 - a);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                half d = length(i.uv);
                half aa = max(fwidth(d), 1e-3);
                half t = frac(atan2(i.uv.x, i.uv.y) / 6.2831853 + 1); // 0 at +Z (top of screen), clockwise

                // glow halo (healed / boss)
                half g = _Glow * exp(-pow((d - _RingR) / 0.18, 2));
                fixed4 col = fixed4(_GlowColor.rgb, saturate(g) * 0.75);

                // dashes (hidden in a bush): gaps in the rim, the fill a little fainter
                half dash = 1;
                if (_Dashed > 0.5)
                {
                    half s = frac(t * _DashCount);
                    dash = smoothstep(0.0, 0.04, s) * (1 - smoothstep(0.58, 0.62, s));
                }
                half fillK = _Dashed > 0.5 ? 0.6 : 1;

                // disc: strongest at the edge, fading towards the centre
                half inside = 1 - smoothstep(_RingR - aa, _RingR + aa, d);
                half k = saturate(d / max(_RingR, 1e-3));
                half fill = _FillAlpha * lerp(_CenterFill, 1, pow(k, _Falloff)) * inside * fillK * _Color.a;
                over(col, _Color.rgb, fill);

                // thin light rim on the edge (its inner side melts into the fill)
                half rimIn = smoothstep(_RingR - _Width - aa, _RingR - _Width * 0.35, d);
                half rim = rimIn * inside * dash * _RimAlpha * _Color.a;
                over(col, saturate(_Color.rgb * 1.05 + 0.18), rim);

                // reload progress arc, just outside the circle (off by default: the ammo bar shows the reload)
                if (_Progress >= 0)
                {
                    half r2 = _RingR + _Width * 1.5;
                    half bg = band(d, r2, _Width, aa);
                    half arc = bg * (1 - smoothstep(_Progress - 0.004, _Progress + 0.004, t));
                    over(col, fixed3(0, 0, 0), bg * 0.35 * (1 - arc));
                    over(col, _ArcColor.rgb, arc * _ArcColor.a);
                }
                return col;
            }
            ENDCG
        }
    }
}
