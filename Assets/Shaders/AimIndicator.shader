// Brawl-style aim indicator: flat translucent shape on the ground (depth tested, so characters stay on top).
// Colour comes from vertex colour (fill / border) * _Tint. Stencil stops overlapping
// triangles (border corners) from blending twice.
Shader "BrawlFortress/AimIndicator"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _StencilRef ("Stencil Ref", Float) = 77
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+100" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        ZWrite Off
        ZTest LEqual
        Offset -1, -1
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        Stencil { Ref [_StencilRef] Comp NotEqual Pass Replace }   // the auto-aim target ring uses its own ref (78)

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Tint;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color * _Tint; return o; }
            fixed4 frag (v2f i) : SV_Target { return i.color; }
            ENDCG
        }
    }
}
