// Swamp theme bog puddle (it slows whoever wades through: Assets/Scripts/Map/BogPuddle.cs). Two layers of the BogMud
// texture drifting slowly in world space, a wet sheen, bubbles that rise and pop on a hashed grid, and a band of
// lighter wet mud along the edge (vertex alpha: 1 in the middle, 0 at the rim) so the slow zone reads clearly.
// Unlit, opaque, cheap for mobile.
Shader "BrawlFortress/Bog"
{
    Properties
    {
        _MainTex ("Mud (BogMud.png)", 2D) = "white" {}
        _Tint ("Tint", Color) = (1.1, 1.15, 1.0, 1)
        _Tiling ("World tiling (1 / metres)", Float) = 0.28
        _Scroll1 ("Layer 1 scroll (uv/s)", Vector) = (0.012, 0.007, 0, 0)
        _Scroll2 ("Layer 2 scroll (uv/s)", Vector) = (-0.008, 0.010, 0, 0)
        _RimColor ("Wet edge", Color) = (0.62, 0.52, 0.30, 1)
        _RimWidth ("Edge width (vertex alpha)", Range(0.02, 0.6)) = 0.2
        _BubbleColor ("Bubbles", Color) = (0.82, 0.88, 0.48, 1)
        _Bubbles ("Bubble cells per metre", Float) = 1.5
        _Sheen ("Sheen", Range(0, 1)) = 0.5
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
            sampler2D _MainTex;
            fixed4 _Tint, _RimColor, _BubbleColor;
            float _Tiling, _RimWidth, _Bubbles, _Sheen;
            float4 _Scroll1, _Scroll2;
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
                float2 uv = i.wp.xz * _Tiling;
                fixed3 a = tex2D(_MainTex, uv + _Scroll1.xy * t).rgb;
                fixed3 b = tex2D(_MainTex, uv * 1.6 + _Scroll2.xy * t + 0.37).rgb;
                fixed3 c = (a + b) * 0.5 * _Tint.rgb;
                c += _Sheen * pow(saturate(b.g * 2.2 - 0.6), 3) * fixed3(0.6, 0.62, 0.45);   // wet glints
                // bubbles: one per cell at most, rising (a dome) then popping (a ring that widens and fades)
                float2 g = i.wp.xz * _Bubbles; float2 cell = floor(g); float2 f = frac(g) - 0.5;
                float h = hash(cell);
                float2 off = (float2(hash(cell + 3.1), hash(cell + 7.7)) - 0.5) * 0.5;
                float ph = frac(t * (0.22 + 0.12 * h) + h * 5.0);
                float r = length(f - off);
                float dome = (1 - smoothstep(0.03, 0.09, r)) * smoothstep(0.0, 0.5, ph) * step(ph, 0.62);
                float rp = (ph - 0.62) / 0.38;
                float ring = (1 - smoothstep(0.0, 0.025, abs(r - 0.06 - rp * 0.22))) * (1 - rp) * step(0.62, ph);
                float live = step(h, 0.55);
                c = lerp(c, _BubbleColor.rgb, saturate(dome * 0.7 + ring * 0.8) * live);
                // edge: a band of lighter wet mud, a darker line just inside it
                half e = i.edge;
                c = lerp(c, _RimColor.rgb, 1 - smoothstep(0.0, _RimWidth, e));
                c *= 1 - 0.25 * (smoothstep(_RimWidth, _RimWidth * 1.6, e) * (1 - smoothstep(_RimWidth * 1.6, _RimWidth * 3.0, e)));
                fixed4 col = fixed4(c, 1);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
