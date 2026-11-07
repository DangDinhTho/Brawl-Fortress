Shader "Hiker/HikerSpecNormalAlphaBlendDSAutoRimColor" 
{
Properties 
{
  _MainTex ("Texture", 2D) = "white" {}
    _MainColor("Main Color", Color) = (0.9,0.9,0.9,1)
  _SpecTex ("SpecMap", 2D) = "black" {}
  _SpecStr ("Specular", Float ) = 1
  _Shininess ("Shininess", Range (0.03, 10)) = 0.5
  _BumpMap ("Normalmap", 2D) 	= "bump" {}  
  //_EmissionMap ("EmissionMap", 2D ) = "black" {}  
  _SpecularColor("Specular Color", Color) = (0.9,0.9,0.9,1)
  _RimColorStr("Rim Color Str ( x LightColor )", Range(0, 3)) = 1
  _RimAmount("Rim Amount", Range(0, 1)) = 0.68
  _RimWidth("Rim Width", Range(0, 0.4)) = 0.1
  _RimThreshold("Rim Threshold", Range(0, 1)) = 0.8	
  //_EmissionFactor ("EmissionFactor", Vector ) = (0.0,1.0,4,1)
_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
	_TransparentStr ("Transparent Strength", Float ) = 1

	_EffTex ("Effect", 2D) = "black" {}
	_AlTex ("Effect Al", 2D) = "black" {}
	_NoiseTex ("Noise Effect", 2D) = "black" {}
	 _ScrollXSpeed("X scroll speed", Range(-10, 10)) = 0
    _ScrollYSpeed("Y scroll speed", Range(-10, 10)) = -0.4 
			_MosaicResolution("Mosaic Resolution", int) = 32
		_MosaicResolutionX("X Mosaic Resolution", int) = 1
		_MosaicResolutionY("Y Mosaic Resolution", int) = 1
}

SubShader 
{
	Cull  Off
	Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
	Tags { "RenderType" = "Transparent" }
		LOD 150
		    Blend SrcAlpha OneMinusSrcAlpha

	CGPROGRAM
//	#pragma surface surf ColoredSpecular
//	#pragma target 3.0
//	#pragma glsl 
//	#pragma exclude_renderers d3d11_9x
	 #pragma surface surf ColoredSpecular alphatest:_Cutoff
	
	struct MySurfaceOutput 
	{
	    half3 Albedo;
	    half3 Normal;
	    half3 Emission;
	    half Specular;
	    half Gloss;
	    half Alpha;
		half RimDot;
	};

	half _Shininess, _SpecStr;
	float4 _MainColor,_SpecularColor,_RimColor,_EmissionFactor;
	float _RimAmount,_RimColorStr;
	float _RimThreshold,_RimWidth;
	half _TransparentStr;
	fixed _ScrollXSpeed;
        fixed _ScrollYSpeed;
	sampler2D _MainTex, _BumpMap, _SpecTex, _EffTex,_AlTex,_NoiseTex;// , _EmissionMap;
			int _MosaicResolution;
		int _MosaicResolutionX;
		int _MosaicResolutionY;
	half4 LightingColoredSpecular (MySurfaceOutput s, fixed3 lightDir, fixed3 viewDir, fixed atten)
	{
		float NdotL = max (0, dot (s.Normal, lightDir));

		//float diff = smoothstep(0, 0.05, NdotL * atten);	
		float diff = NdotL * atten;	

		fixed3 halfDir = normalize(lightDir + viewDir);

		fixed nh = max (0, dot (s.Normal, halfDir));

		fixed spec = pow (nh, s.Specular*128) * s.Gloss;
		//spec = smoothstep(0.005, 0.01, spec);
		float3 specular = spec * _SpecularColor*s.Albedo*_SpecStr;	
		
		fixed4 c;

		// Rim
		float rimIntensity = s.RimDot* pow(NdotL, _RimThreshold);

		//c.rgb = float3(s.RimDot,s.RimDot,s.RimDot);
		//c.rgb = float3(rimIntensity,rimIntensity,rimIntensity);
		
		rimIntensity = smoothstep(_RimAmount - _RimWidth, _RimAmount + _RimWidth, rimIntensity);

		// Rim only
		//c.rgb = float3(rimIntensity,rimIntensity,rimIntensity);

		_RimColor = _LightColor0*_RimColorStr;

		float3 rim = rimIntensity * _RimColor;

		

		//c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * (atten * 2)*100;
		c.rgb = s.Albedo * _LightColor0.rgb * diff + specular + rim;
		// Rim instensity only
		//c.rgb = float3(rimIntensity,rimIntensity,rimIntensity);
		// Rim only
		//c.rgb = rim;
		c.a = s.Alpha + spec * _SpecularColor.a;
		//UNITY_OPAQUE_ALPHA(c.a);
		//c.a = 0;
		return c;
	}
	 	 
	 
	struct Input 
	{
		float2 uv_MainTex : TEXCOORD0;
		float3 viewDir;
		float3 worldPos;
		INTERNAL_DATA
	};
	#define MOSAIC (_MosaicResolution * float2(_MosaicResolutionX/4, _MosaicResolutionY))
	void surf (Input IN, inout MySurfaceOutput o)
	{
		fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _MainColor;
		o.Albedo = c.rgb;
		o.Alpha = c.a * _TransparentStr;


		fixed offsetX = _ScrollXSpeed * _Time;
            fixed offsetY = _ScrollYSpeed * _Time;
            fixed2 offsetUV = fixed2(offsetX, offsetY);
			float density = 1000;
			float2 uv;
		
			uv = floor(IN.uv_MainTex * _MosaicResolution)/ _MosaicResolution;
						float rd= frac(sin( dot(uv ,float3(12.9898,78.233,45.5432) )) * 43758.5453);

			//float2 uv = floor(IN.uv_MainTex * MOSAIC)/ MOSAIC ;//+float2(round(2 * rd),0);
			uv += offsetUV;
		float4 detail = tex2D(_EffTex, uv);
		//detail *= round(rd);
		float4 noise = tex2D(_NoiseTex, IN.uv_MainTex  );
		float4 al = tex2D(_AlTex, IN.uv_MainTex);
		o.Albedo +=  al.r * detail ;// * 3 * (noise);// * (p - floor(p)) ;
		//o.Albedo = detail;

		//		o.Albedo = fixed3(rd,rd, rd)  ;

		half spec = tex2D (_SpecTex, IN.uv_MainTex).g;
					
		o.Gloss = spec;

		o.Specular =  _Shininess;
		
		o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));		
		//o.RimDot = 1.0 - abs(dot (normalize(IN.viewDir), o.Normal));

		o.RimDot = 1.0 - abs(dot (normalize(IN.viewDir), o.Normal));

		/*half3 emission_val = tex2D (_EmissionMap, IN.uv_MainTex).rgb;

		o.Emission = emission_val* (_EmissionFactor.x + ( sin(_Time.y*2 + IN.worldPos.y*_EmissionFactor.z ) + 1)*0.5*(_EmissionFactor.y-_EmissionFactor.x));*/

	}
	ENDCG
}

fallback "Diffuse"

}