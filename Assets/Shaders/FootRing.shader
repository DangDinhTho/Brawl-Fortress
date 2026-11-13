// Brawl-Stars-style team ring under a character's feet (FootRing.cs). Procedural, no texture.
// uv in -1..1 over the quad; the ring sits at _RingR. Reload arc just outside it, optional dashes and glow.
Shader "BrawlFortress/FootRing"
{
    Properties
    {
        _Color ("Ring colour", Color) = (0.3, 1, 0.3, 0.9)
        _FillAlpha ("Inner fill alpha", Range(0, 1)) = 0.12
        _RingR ("Ring radius (uv)", Range(0.2, 1)) = 0.72
        _Width ("Ring width (uv)", Range(0.01, 0.3)) = 0.1
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
            fixed4 _Color, _ArcColor, _GlowColor;
            half _FillAlpha, _RingR, _Width, _Dashed, _DashCount, _Progress, _Glow;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv * 2 - 1; return o; }

            half band(half d, half r, half w, half aa) { return 1 - smoothstep(w * 0.5 - aa, w * 0.5 + aa, abs(d - r)); }

            fixed4 frag (v2f i) : SV_Target
            {
                half d = length(i.uv);
                half aa = max(fwidth(d), 1e-3);
                half t = frac(atan2(i.uv.x, i.uv.y) / 6.2831853 + 1); // 0 at +Z (top of screen), clockwise

                // glow halo (healed / uber)
                half g = _Glow * exp(-pow((d - _RingR) / 0.16, 2));
                fixed4 col = fixed4(_GlowColor.rgb, saturate(g) * 0.75);

                // faint inner fill
                half fill = (1 - smoothstep(_RingR - aa, _RingR + aa, d)) * _FillAlpha * _Color.a;
                col.rgb = lerp(col.rgb, _Color.rgb, fill / max(col.a + fill, 1e-4));
                col.a = col.a + fill * (1 - col.a);

                // main ring (dashed while hidden in a bush)
                half ring = band(d, _RingR, _Width, aa);
                if (_Dashed > 0.5)
                {
                    half s = frac(t * _DashCount);
                    ring *= smoothstep(0.0, 0.05, s) * (1 - smoothstep(0.55, 0.6, s));
                }
                ring *= _Color.a;
                col.rgb = lerp(col.rgb, _Color.rgb, ring);
                col.a = ring + col.a * (1 - ring);

                // reload progress arc, just outside the ring
                if (_Progress >= 0)
                {
                    half r2 = _RingR + _Width * 1.6;
                    half bg = band(d, r2, _Width * 0.9, aa);
                    half arc = bg * (1 - smoothstep(_Progress - 0.004, _Progress + 0.004, t));
                    half back = bg * 0.35;
                    col.rgb = lerp(col.rgb, fixed3(0, 0, 0), back * (1 - arc));
                    col.a = back + col.a * (1 - back);
                    arc *= _ArcColor.a;
                    col.rgb = lerp(col.rgb, _ArcColor.rgb, arc);
                    col.a = arc + col.a * (1 - arc);
                }
                return col;
            }
            ENDCG
        }
    }
}
