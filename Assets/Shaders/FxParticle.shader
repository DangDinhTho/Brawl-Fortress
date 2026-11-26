// Brawl-Stars-style little effects (BrawlFx.cs): procedural particle shapes, no texture, vertex colour (the particle's
// colour) times the shape's coverage. _Shape: 0 = soft puff (round, a little denser and lighter on top, for dust, mud,
// bubbles and stretched speed streaks), 1 = plus sign (healing), 2 = four-point spark (hits).
Shader "BrawlFortress/FxParticle"
{
    Properties
    {
        _Shape ("Shape (0 puff, 1 plus, 2 spark)", Float) = 0
        _Softness ("Edge softness", Range(0.01, 1)) = 0.45
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            half _Shape, _Softness;
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv * 2 - 1; return o; }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 p = i.uv;
                half a; fixed3 rgb = i.color.rgb;
                if (_Shape < 0.5)
                {
                    half d = length(p);
                    a = 1 - smoothstep(1 - _Softness, 1, d);
                    a *= lerp(0.75, 1, saturate(1 - d));          // denser middle
                    rgb *= lerp(0.82, 1.04, saturate(p.y * 0.5 + 0.5));   // lighter top, like a lit ball of dust
                }
                else if (_Shape < 1.5)
                {
                    // plus: two rounded bars with a dark rim so it reads on any ground
                    float2 q = abs(p);
                    half bar = min(max(q.x - 0.26, q.y - 0.86), max(q.y - 0.26, q.x - 0.86));
                    half aa = fwidth(bar) + 0.02;
                    half inner = 1 - smoothstep(-aa, aa, bar);
                    half outer = 1 - smoothstep(-aa, aa, bar - 0.14);
                    a = outer;
                    rgb = lerp(rgb * 0.35, rgb, inner);
                }
                else
                {
                    // four-point star: thin cross with a bright core
                    float2 q = abs(p);
                    half star = min(q.x * 7 + q.y, q.y * 7 + q.x);
                    half core = 1 - smoothstep(0.0, 0.45, length(p));
                    a = saturate((1 - smoothstep(0.75, 1.0, star)) + core);
                    rgb = lerp(rgb, fixed3(1, 1, 1), core * 0.8);
                }
                return fixed4(rgb, a * i.color.a);
            }
            ENDCG
        }
    }
}
