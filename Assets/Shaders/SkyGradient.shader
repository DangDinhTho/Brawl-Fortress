// A plain two-tone gradient sky (Volcano theme: a smoky dusk). Skybox shader: set as RenderSettings.skybox. Cheap.
Shader "BrawlFortress/SkyGradient"
{
    Properties
    {
        _Top ("Top", Color) = (0.26, 0.16, 0.18, 1)
        _Horizon ("Horizon", Color) = (0.78, 0.44, 0.32, 1)
        _Bottom ("Below the horizon", Color) = (0.45, 0.22, 0.16, 1)
        _Exponent ("Falloff", Range(0.2, 4)) = 0.7
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Top, _Horizon, _Bottom;
            half _Exponent;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.dir = v.vertex.xyz; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                half y = normalize(i.dir).y;
                fixed3 up = lerp(_Horizon.rgb, _Top.rgb, pow(saturate(y), _Exponent));
                fixed3 c = y >= 0 ? up : lerp(_Horizon.rgb, _Bottom.rgb, saturate(-y * 4));
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
