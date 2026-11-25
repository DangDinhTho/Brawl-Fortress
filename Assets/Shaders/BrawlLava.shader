// Volcano theme lava (lava lakes round the island, the eruption fountain): two layers of the LavaFlow texture drifting
// in different directions, the bright melt pulsing slowly. Unlit (it is the light source), opaque, cheap for mobile.
// _WorldUV 1: the texture is laid in world XZ (a lake of any size keeps the same scale); 0: mesh UVs (the fountain).
Shader "BrawlFortress/Lava"
{
    Properties
    {
        _MainTex ("Lava (LavaFlow.png)", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Tiling ("World tiling (1 / metres)", Float) = 0.12
        _WorldUV ("World UV (0/1)", Float) = 1
        _Scroll1 ("Layer 1 scroll (uv/s)", Vector) = (0.010, 0.006, 0, 0)
        _Scroll2 ("Layer 2 scroll (uv/s)", Vector) = (-0.007, 0.011, 0, 0)
        _Scale2 ("Layer 2 scale", Float) = 1.7
        _Pulse ("Pulse amount", Range(0, 1)) = 0.18
        _Boost ("Brightness", Float) = 1
        _HotColor ("Hot colour", Color) = (1, 0.6, 0.15, 1)
        _HotMix ("Hot mix (fountain: crust burns bright)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 _Tint, _HotColor;
            half _HotMix;
            float _Tiling, _WorldUV, _Scale2, _Pulse, _Boost;
            float4 _Scroll1, _Scroll2;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wp : TEXCOORD1; UNITY_FOG_COORDS(2) };
            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = lerp(v.uv, o.wp.xz * _Tiling, _WorldUV);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                fixed3 a = tex2D(_MainTex, i.uv + _Scroll1.xy * t).rgb;
                fixed3 b = tex2D(_MainTex, i.uv * _Scale2 + _Scroll2.xy * t + 0.37).rgb;
                // keep the brighter of the two layers: crust plates of one drift over the melt of the other
                fixed3 c = max(a, b * 0.92);
                c = lerp(c, _HotColor.rgb * (0.75 + 0.5 * c), _HotMix);
                half heat = saturate(dot(c, half3(0.2, 0.6, 0.2)) * 1.6 - 0.35);
                half pulse = 1 + _Pulse * heat * sin(t * 1.7 + i.wp.x * 0.35 + i.wp.z * 0.21);
                fixed4 col = fixed4(c * _Tint.rgb * pulse * _Boost, 1);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
