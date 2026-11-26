// Brawl-Stars-style team ring under a character's feet (FootRing.cs). Procedural, no texture.
// uv in -1..1 over the quad; the ring sits at _RingR: a thick team-coloured band (lighter on its inner half) with a
// dark outline on both edges, over a soft team-coloured disc that gets stronger toward the ring.
// Optional reload arc just outside it, dashes (hidden in a bush) and glow (healed / boss).
Shader "BrawlFortress/FootRing"
{
    Properties
    {
        _Color ("Ring colour", Color) = (0.3, 1, 0.3, 1)
        _FillAlpha ("Inner fill alpha", Range(0, 1)) = 0.28
        _RingR ("Ring radius (uv)", Range(0.2, 1)) = 0.72
        _Width ("Ring width (uv)", Range(0.01, 0.4)) = 0.14
        _Outline ("Outline width (uv)", Range(0, 0.15)) = 0.035
        _OutlineColor ("Outline colour", Color) = (0.047, 0.031, 0.071, 0.85)
        _Dashed ("Dashed (0/1)", Float) = 0
        _DashCount ("Dash count", Float) = 14
        _Progress ("Reload arc 0..1 (<0 = off)", Float) = -1
        _ArcColor ("Reload arc colour", Color) = (1, 1, 1, 1)
        _Glow ("Glow 0..1", Range(0, 1)) = 0
        _GlowColor ("Glow colour", Color) = (0.5, 1, 0.6, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-9" "RenderType" = "Transparent" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
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
            fixed4 _Color, _ArcColor, _GlowColor, _OutlineColor;
            half _FillAlpha, _RingR, _Width, _Outline, _Dashed, _DashCount, _Progress, _Glow;
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

                // soft disc inside the ring, stronger toward the ring
                half inner = _RingR - _Width * 0.5 - _Outline;
                half disc = (1 - smoothstep(inner - aa, inner + aa, d)) * _FillAlpha * lerp(0.35, 1, saturate(d / max(inner, 1e-3))) * _Color.a;
                over(col, _Color.rgb, disc);

                // dashes while hidden in a bush
                half dash = 1;
                if (_Dashed > 0.5)
                {
                    half s = frac(t * _DashCount);
                    dash = smoothstep(0.0, 0.04, s) * (1 - smoothstep(0.58, 0.62, s));
                }

                // dark outline band, then the coloured band on top (lighter on its inner half)
                half outlineBand = band(d, _RingR, _Width + 2 * _Outline, aa) * dash * _OutlineColor.a * _Color.a;
                over(col, _OutlineColor.rgb, outlineBand);
                half core = band(d, _RingR, _Width, aa) * dash * _Color.a;
                half innerHalf = 1 - smoothstep(_RingR - _Width * 0.1, _RingR + _Width * 0.15, d);
                fixed3 ringRgb = lerp(_Color.rgb * 0.82, saturate(_Color.rgb * 1.08 + 0.12), innerHalf);
                over(col, ringRgb, core);

                // reload progress arc, just outside the ring (off by default: the ammo bar shows the reload)
                if (_Progress >= 0)
                {
                    half r2 = _RingR + _Width * 0.5 + _Outline + _Width * 0.5;
                    half bg = band(d, r2, _Width * 0.7, aa);
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
