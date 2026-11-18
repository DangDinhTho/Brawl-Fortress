Shader "Industrial Wave/Character Cartoon"
{
    Properties
    {
        _MainTex ("Character texture", 2D) = "white" {}
        _TeamMask ("Team paint mask", 2D) = "black" {}
        _TeamTint ("Team paint", Color) = (0.72,0.24,0.20,1)
        _Brightness ("Brightness", Range(0.8,1.5)) = 1.20
        _Saturation ("Saturation", Range(0.8,1.6)) = 1.23
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Cartoon fullforwardshadows noforwardadd noambient
        #include "UnityCG.cginc"
        sampler2D _MainTex, _TeamMask;
        fixed4 _TeamTint;
        half _Brightness, _Saturation;
        struct Input { float2 uv_MainTex; };
        half4 LightingCartoon(SurfaceOutput s, half3 lightDir, half atten)
        {
            half ndl=dot(s.Normal,lightDir);
            half band=0.66+0.18*smoothstep(-0.08,0.04,ndl)+0.16*smoothstep(0.48,0.60,ndl);
            half3 light=lerp(half3(0.72,0.76,0.82),max(_LightColor0.rgb,half3(0.85,0.85,0.85)),saturate(atten));
            return half4(s.Albedo*band*light,1);
        }
        void surf(Input IN,inout SurfaceOutput o)
        {
            half3 c=tex2D(_MainTex,IN.uv_MainTex).rgb;
            half mask=tex2D(_TeamMask,IN.uv_MainTex).r;
            c*=lerp(half3(1,1,1),_TeamTint.rgb,mask);
            c=pow(max(c,0.001),0.91)*_Brightness;
            half grey=dot(c,half3(0.299,0.587,0.114));
            o.Albedo=saturate(lerp(grey.xxx,c,_Saturation));o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Mobile/Diffuse"
}
