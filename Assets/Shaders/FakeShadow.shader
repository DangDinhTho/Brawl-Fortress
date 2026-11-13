// Fake blob shadow under characters (FakeShadow.cs): procedural soft dark disc, no texture.
Shader "BrawlFortress/FakeShadow"
{
    Properties
    {
        _Color ("Color (alpha = darkness)", Color) = (0, 0, 0, 0.85)
        _Softness ("Softness (0 = hard edge, 1 = all gradient)", Range(0.05, 1)) = 1
        _Falloff ("Falloff (higher = darker core, softer edge)", Range(1, 4)) = 2
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
            #include "UnityCG.cginc"
            fixed4 _Color;
            half _Softness;
            half _Falloff;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv * 2 - 1; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                half d = length(i.uv);
                half a = 1 - smoothstep(1 - _Softness, 1, d);
                a = pow(a, _Falloff); // fades out gradually towards the rim, no visible edge
                return fixed4(_Color.rgb, _Color.a * a);
            }
            ENDCG
        }
    }
}
