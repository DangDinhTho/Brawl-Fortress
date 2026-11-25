// Glowing faces of the Volcano props (cracks, fire crystals, embers, the lava on the volcano): the shared 128 px palette
// drawn unlit, with a slow shimmer that differs from place to place, and a brightness the eruption vents raise before
// they erupt (_Boost, set per renderer with a MaterialPropertyBlock). Two-sided: thin cracks and drips show from any side.
Shader "BrawlFortress/LavaGlow"
{
    Properties
    {
        _MainTex ("Palette", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Boost ("Brightness", Float) = 1
        _Flicker ("Shimmer", Range(0, 0.5)) = 0.12
        _Hot ("Hot colour (high boost)", Color) = (1, 0.95, 0.6, 1)
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
            fixed4 _Color, _Hot;
            half _Boost, _Flicker;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wp : TEXCOORD1; UNITY_FOG_COORDS(2) };
            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv;
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 c = tex2D(_MainTex, i.uv).rgb * _Color.rgb;
                half s = 1 + _Flicker * (sin(_Time.y * 2.3 + i.wp.x * 1.7 + i.wp.z * 1.3) * 0.6 + sin(_Time.y * 5.1 + i.wp.y * 2.9) * 0.4);
                // above 1 the colour runs to a hot yellow-white instead of just clipping
                half over = saturate(_Boost - 1);
                c = lerp(c * s * min(_Boost, 1), _Hot.rgb, over * 0.65);
                fixed4 col = fixed4(c, 1);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
