Shader "Hiker/HikerSpecWeather" 
{
Properties 
{
  _MainTex ("Texture", 2D) = "white" {}
  _SpecTex ("SpecMap", 2D) = "white" {}
  _WeatherSize ("WeatherTexSize", Range (0.1, 10)) = 1
  _SpecStr ("Specular", Float ) = 1
  _Shininess ("Shininess", Range (0.03, 10)) = 0.078125
}

SubShader 
{
    LOD 200 
    Tags { "RenderType" = "Opaque" }
	CGPROGRAM
	#pragma surface surf ColoredSpecular noforwardadd halfasview interpolateview
	#pragma target 3.0
	#pragma glsl 
	#pragma exclude_renderers d3d11_9x
	 
	struct MySurfaceOutput 
	{
	    half3 Albedo;
	    half3 Normal;
	    half3 Emission;
	    half Specular;
	    half Gloss;
	    half Alpha;
	};

	half4 LightingColoredSpecular (MySurfaceOutput s, half3 lightDir, half3 halfDir, half atten)
	{
		half3 normal = normalize(s.Normal);

		half diff = max (0, dot (normal, lightDir));

		half nh = max (0, dot (normal, halfDir));

		half spec = pow (nh, s.Specular*128) * s.Gloss;

		half4 c;

		c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * atten;
		
		UNITY_OPAQUE_ALPHA(c.a);

		return c;
	}
	 	 
	 
	struct Input 
	{
		float2 uv_MainTex;		
		INTERNAL_DATA
	};
	
	half _Shininess, _SpecStr,_WeatherSize;
	
	sampler2D _MainTex, _WeatherShareTex, _SpecTex;	
	
	void surf (Input IN, inout MySurfaceOutput o)
	{
		o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb;

		half3 spec_weather = tex2D (_SpecTex, IN.uv_MainTex).rgb;
		
		half spec = spec_weather.g;

		half weather = spec_weather.b;

		half3 weather_share = tex2D (_WeatherShareTex, IN.uv_MainTex*_WeatherSize).rgb;

		o.Albedo =  lerp(o.Albedo,weather_share,weather);
				
		o.Gloss = spec*_SpecStr;

		o.Specular =  _Shininess;
	}
ENDCG
}

fallback "Diffuse"

}