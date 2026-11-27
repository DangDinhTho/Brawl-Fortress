// Ice maps: a patch of slick clear ice on the floor (SlickIce.cs): characters slide on it. Drawn over the ground,
// alpha blended so the floor shows through faintly (clear ice): a pale blue sheet with a frosty white rim (vertex alpha
// 1 inside, 0 at the rim), thin cracks, bright glint bands sweeping across and small sparkles: it reads as polished and
// slippery, unlike the matte ice plates of the decor. Unlit, procedural, cheap for mobile.
Shader "BrawlFortress/SlickIce"
{
    Properties
    {
        _Tint ("Ice", Color) = (0.62, 0.88, 1.0, 0.62)
        _Deep ("Ice middle", Color) = (0.42, 0.74, 0.98, 0.7)
        _Frost ("Frost rim", Color) = (0.96, 0.99, 1.0, 0.9)
        _Glint ("Glints", Color) = (1, 1, 1, 1)
        _Crack ("Cracks", Color) = (0.86, 0.96, 1.0, 0.8)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            float4 _Tint, _Deep, _Frost, _Glint, _Crack;
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float3 wp : TEXCOORD0; float edge : TEXCOORD1; UNITY_FOG_COORDS(2) };
            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.edge = v.color.a;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float e = i.edge;
                float2 w = i.wp.xz;
                float4 c = lerp(_Tint, _Deep, smoothstep(0.4, 1.0, e));
                // frosty white rim
                float rim = 1.0 - smoothstep(0.06, 0.42, e);
                // thin cracks: two wavy line families
                float l1 = abs(sin(w.x * 1.9 + sin(w.y * 1.3) * 1.6));
                float l2 = abs(sin(w.y * 2.3 - sin(w.x * 0.9) * 1.4 + 1.7));
                float crack = (1.0 - smoothstep(0.0, 0.05, l1)) * step(0.55, hash(floor(w * 0.7))) + (1.0 - smoothstep(0.0, 0.04, l2)) * step(0.6, hash(floor(w * 0.6) + 4.2));
                crack = saturate(crack) * smoothstep(0.25, 0.6, e);
                // glint bands sweeping diagonally (two speeds)
                float s1 = frac((w.x + w.y) * 0.22 - t * 0.18);
                float s2 = frac((w.x - w.y * 0.6) * 0.31 - t * 0.11 + 0.4);
                float band = (1.0 - smoothstep(0.0, 0.035, abs(s1 - 0.5))) + 0.6 * (1.0 - smoothstep(0.0, 0.02, abs(s2 - 0.5)));
                band *= smoothstep(0.2, 0.55, e);
                // sparkles on a hashed grid
                float2 g = w * 2.2; float2 cell = floor(g); float2 f = frac(g) - 0.5;
                float h = hash(cell);
                float2 off = (float2(hash(cell + 3.1), hash(cell + 7.7)) - 0.5) * 0.6;
                float tw = saturate(sin(t * (1.5 + h) + h * 6.28));
                float spark = (1.0 - smoothstep(0.0, 0.06, length(f - off))) * tw * tw * step(0.55, h);
                float3 col = c.rgb;
                col = lerp(col, _Crack.rgb, crack * _Crack.a);
                col = lerp(col, _Frost.rgb, rim * _Frost.a);
                col = lerp(col, _Glint.rgb, saturate(band * 0.8 + spark));
                float a = c.a + rim * 0.3 + band * 0.25 + spark * 0.5 + crack * 0.1;
                a *= smoothstep(0.0, 0.12, e);   // soft outer edge
                float4 o = float4(col, saturate(a));
                UNITY_APPLY_FOG(i.fogCoord, o);
                return o;
            }
            ENDCG
        }
    }
    Fallback Off
}
