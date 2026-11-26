// Ocean theme: an ocean current lane on the sea floor (Assets/Scripts/Map/OceanCurrent.cs). A transparent additive
// strip: chevrons and streaks drifting along the object's forward axis (+Z), so players read the direction at a
// glance; soft edges across the lane and at its ends. The quad lies flat (rotated 90 on X): its local X is the
// width, its local Y the flow (the parent's +Z). Laid out in world space from the object's own axes, so one
// material serves lanes of any size (no per-instance properties: works with batching).
Shader "BrawlFortress/Current"
{
    Properties
    {
        _Color ("Colour", Color) = (0.75, 1.0, 1.0, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0.6
        _Spacing ("Chevron spacing (m)", Float) = 1.6
        _Speed ("Flow speed (m/s)", Float) = 2.2
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-5" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color; float _Intensity, _Spacing, _Speed;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float2 lane : TEXCOORD0; float2 half_ : TEXCOORD1; };
            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 c = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 ax = mul((float3x3)unity_ObjectToWorld, float3(1, 0, 0));   // lane width axis (scale x)
                float3 az = mul((float3x3)unity_ObjectToWorld, float3(0, 1, 0));   // flow axis (the quad's local Y)
                float lx = length(ax), lz = length(az);
                o.lane = float2(dot(wp - c, ax / lx), dot(wp - c, az / lz));        // metres across / along
                o.half_ = float2(lx, lz) * 0.5;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float u = i.lane.x / i.half_.x;                    // -1 .. 1 across
                float along = i.lane.y;
                float edge = saturate((1 - abs(u)) * 3) * saturate((i.half_.y - abs(along)) / 1.2);
                float s = (along + abs(u) * i.half_.x * 0.6) / _Spacing - _Time.y * _Speed / _Spacing;   // chevrons point downstream
                float chev = smoothstep(0.0, 0.08, frac(s)) * (1 - smoothstep(0.18, 0.32, frac(s)));
                float streak = smoothstep(0.75, 1.0, sin((along * 1.3 - _Time.y * _Speed * 1.3) + sin(u * 9.0) * 2.0)) * 0.35;
                float c = (chev + streak + 0.12) * edge;
                return fixed4(_Color.rgb * c * _Intensity, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
