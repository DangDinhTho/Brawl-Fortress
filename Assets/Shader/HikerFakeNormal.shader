// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced tex2D unity_LightmapInd with UNITY_SAMPLE_TEX2D_SAMPLER

// Upgrade NOTE: commented out 'sampler2D unity_LightmapInd', a built-in variable

// Upgrade NOTE: commented out 'sampler2D unity_LightmapInd', a built-in variable

// Upgrade NOTE: commented out 'sampler2D unity_LightmapInd', a built-in variable

// Upgrade NOTE: commented out 'sampler2D unity_LightmapInd', a built-in variable

// Upgrade NOTE: commented out 'sampler2D unity_LightmapInd', a built-in variable

// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

Shader "Hiker/HikerFakeNormal" {

  Properties {
    _MainTex ("Texture 1", 2D) = "white" {}
	_BumpMap ("Normalmap", 2D) 	= "bump" {}	
	_SpecStr ("Specular Strength", Float ) = 1
	_Shininess ("Shininess", Range (0.03, 10)) = 0.078125
  }

  SubShader {
	LOD 400

    Tags { "RenderType" = "Opaque" }

    Pass {
	  Name "FORWARD"
		Tags { "LightMode" = "ForwardBase" }

      // Disable lighting, we're only using the lightmap
      Lighting Off

      CGPROGRAM
      // Must be a vert/frag shader, not a surface shader: the necessary variables
      // won't be defined yet for surface shaders.
      
	  #pragma vertex vert
      #pragma fragment frag
	  // make fog work
	  #pragma multi_compile_fwdbase
	  #pragma multi_compile_fog

	  #pragma debug

#ifdef LIGHTMAP_ON	  

		#include "UnityCG.cginc"

      struct v2f {
        float4 pos : SV_POSITION;
		float3 viewDir : COLOR;
        float4 uv0 : TEXCOORD0;
#ifdef DIRLIGHTMAP_OFF
#else
		float4 calcN1 : TEXCOORD1;
		float4 calcN2 : TEXCOORD2;
		float4 calcN3 : TEXCOORD3;		
#endif
		UNITY_FOG_COORDS(4)
      };

      struct appdata_lightmap {
        float4 vertex : POSITION;
		float3 normal : NORMAL;
		float4 tangent : TANGENT;
        float2 texcoord : TEXCOORD0;
        float2 texcoord1 : TEXCOORD1;
      };

      sampler2D _MainTex,_BumpMap;

      float4 _MainTex_ST; // Define this since its expected by TRANSFORM_TEX; it is also pre-populated by Unity.
	  
	  half _Shininess, _SpecStr;

      v2f vert(appdata_lightmap v) {
        v2f o;
		UNITY_INITIALIZE_OUTPUT(v2f, o);
        o.pos = UnityObjectToClipPos(v.vertex);

        // UnityCG.cginc - Transforms 2D UV by scale/bias property
        // #define TRANSFORM_TEX(tex,name) (tex.xy * name##_ST.xy + name##_ST.zw)
        o.uv0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);

        // Use `unity_LightmapST` NOT `unity_Lightmap_ST`
        o.uv0.zw = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
		
#ifdef DIRLIGHTMAP_OFF
#else
		float3 worldTangent_2;

		worldTangent_2 = normalize( mul( (float3x3)unity_ObjectToWorld ,v.tangent.xyz) );

		float3 worldNormal_3 = mul((float3x3)unity_ObjectToWorld, SCALED_NORMAL);

		float tangentSign_1;
	    tangentSign_1 = (v.tangent.w * unity_WorldTransformParams.w);

		float3 tmpvar_14;
		tmpvar_14 = (((worldNormal_3.yzx * worldTangent_2.zxy) - (worldNormal_3.zxy * worldTangent_2.yzx)) * tangentSign_1);

		float3 worldPos;
		worldPos = ( mul(unity_ObjectToWorld, v.vertex) ).xyz;

		o.calcN1.x = worldTangent_2.x;
		o.calcN1.y = tmpvar_14.x;
		o.calcN1.z = worldNormal_3.x;
		o.calcN1.w = worldPos.x;

		o.calcN2.x = worldTangent_2.y;
		o.calcN2.y = tmpvar_14.y;
		o.calcN2.z = worldNormal_3.y;
		o.calcN2.w = worldPos.y;

		o.calcN3.x = worldTangent_2.z;
		o.calcN3.y = tmpvar_14.z;
		o.calcN3.z = worldNormal_3.z;
		o.calcN3.w = worldPos.z;
		
		float3 binormal = cross( normalize(v.normal), normalize(v.tangent.xyz) ) * v.tangent.w;
		float3x3 rotation = float3x3( v.tangent.xyz, binormal, v.normal );

		o.viewDir = WorldSpaceViewDir(v.vertex);
#endif
		UNITY_TRANSFER_FOG(o,o.pos);

        return o;
      }

      half4 frag(v2f i) : COLOR {
        half4 main_color = tex2D(_MainTex, i.uv0.xy);

        // Decodes lightmaps:
		half3 light_map_val = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv0.zw));

#ifdef DIRLIGHTMAP_OFF
		main_color.rgb = main_color.rgb*light_map_val;
#else
		half spec_tex = _SpecStr;

		half3 normal = UnpackNormal(tex2D(_BumpMap, i.uv0.xy));

		// decode light dir
		half4 light_tex = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd,unity_Lightmap, i.uv0.zw);

		half3 normal_world;

		normal_world.x = dot (i.calcN1.xyz, normal);
		normal_world.y = dot (i.calcN2.xyz, normal);
		normal_world.z = dot (i.calcN3.xyz, normal);

		normal_world = normalize(normal_world); // Babe : Is right?


		half3 light_dir = DecodeDirectionalLightmap( light_map_val, light_tex, normal_world);

		half3 view_dir = normalize(half3(i.viewDir) );

		half3 h = normalize ( _WorldSpaceLightPos0 + view_dir);

		float nh = max (0, dot ( normalize(normal_world), h));

		float diffuseLight = dot(normal_world, _WorldSpaceLightPos0);
		if (diffuseLight <= 0.0f)
			nh = 0;

		float spec = pow (nh, _Shininess * 128.0);

		half3 specColor = spec_tex * spec;

	    main_color.rgb = (main_color.rgb +specColor) * light_dir; // babe fake light model

		//main_color.rgb = normal_world; // babe fake light model


		//main_color.rgb = half3(spec_tex * spec,spec_tex * spec,spec_tex * spec);
		//main_color.w = 1;
#endif
        
		UNITY_APPLY_FOG(i.fogCoord, main_color);

		return main_color;
      }
#else	  
            #include "UnityCG.cginc" 
         uniform float4 _LightColor0; 
            // color of light source (from "Lighting.cginc")
 
         // User-specified properties
         sampler2D _MainTex,_BumpMap;

         float4 _MainTex_ST; // Define this since its expected by TRANSFORM_TEX; it is also pre-populated by Unity.
	  
	     half _Shininess, _SpecStr;
 
         struct vertexInput {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
			float2 texcoord : TEXCOORD0;
         };
         struct vertexOutput {
            float4 pos : SV_POSITION;
			float4 uv0 : TEXCOORD0;
            float4 posWorld : TEXCOORD1;
            float3 normalDir : TEXCOORD2;
            float3 vertexLighting : TEXCOORD3;
			UNITY_FOG_COORDS(4)
         };
 
         vertexOutput vert(vertexInput input)
         {          
            vertexOutput output;
			UNITY_INITIALIZE_OUTPUT(vertexOutput, output);

            float4x4 modelMatrix = unity_ObjectToWorld;
            float4x4 modelMatrixInverse = unity_WorldToObject; 
			output.uv0.xy = input.texcoord;
            output.posWorld = mul(modelMatrix, input.vertex);
            output.normalDir = normalize(
               mul(float4(input.normal, 0.0), modelMatrixInverse).xyz);
            output.pos = UnityObjectToClipPos(input.vertex);
 
            // Diffuse reflection by four "vertex lights"            
            output.vertexLighting = float3(0.0, 0.0, 0.0);
            #ifdef VERTEXLIGHT_ON
            for (int index = 0; index < 4; index++)
            {    
               float4 lightPosition = float4(unity_4LightPosX0[index], 
                  unity_4LightPosY0[index], 
                  unity_4LightPosZ0[index], 1.0);
 
               float3 vertexToLightSource = 
                  lightPosition.xyz - output.posWorld.xyz;        
               float3 lightDirection = normalize(vertexToLightSource);
               float squaredDistance = 
                  dot(vertexToLightSource, vertexToLightSource);
               float attenuation = 1.0 / (1.0 + 
                  unity_4LightAtten0[index] * squaredDistance);
               float3 diffuseReflection = attenuation 
                  * unity_LightColor[index].rgb * max(0.0, dot(output.normalDir, lightDirection));         
 
               output.vertexLighting = 
                  output.vertexLighting + diffuseReflection;
            }
            #endif
			UNITY_TRANSFER_FOG(output,output.pos);
            return output;
         }
 
         float4 frag(vertexOutput input) : COLOR
         {
		    half4 main_color = tex2D(_MainTex, input.uv0.xy);

            float3 normalDirection = normalize(input.normalDir); 
            float3 viewDirection = normalize(
               _WorldSpaceCameraPos - input.posWorld.xyz);
            float3 lightDirection;
            float attenuation;
 
            if (0.0 == _WorldSpaceLightPos0.w) // directional light?
            {
               attenuation = 1.0; // no attenuation
               lightDirection = 
                  normalize(_WorldSpaceLightPos0.xyz);
			   
            } 
            else // point or spot light
            {
               float3 vertexToLightSource = 
                  _WorldSpaceLightPos0.xyz - input.posWorld.xyz;
               float distance = length(vertexToLightSource);
               attenuation = 1.0 / distance; // linear attenuation 
               lightDirection = normalize(vertexToLightSource);

            }
 
            float3 ambientLighting = 
                UNITY_LIGHTMODEL_AMBIENT.rgb;
 
            float3 diffuseReflection = 
               attenuation * _LightColor0.rgb * max(0.0, dot(normalDirection, lightDirection));
 
            float3 specularReflection;
            if (dot(normalDirection, lightDirection) < 0.0) 
               // light source on the wrong side?
            {
               specularReflection = float3(0.0, 0.0, 0.0); 
                  // no specular reflection
            }
            else // light source on the right side
            {
               specularReflection = attenuation*_SpecStr * _LightColor0.rgb 
                  * pow(max(0.0, dot(
                  reflect(-lightDirection, normalDirection), 
                  viewDirection)), _Shininess* 128.0);
            }
			float3 light = input.vertexLighting + ambientLighting + diffuseReflection + specularReflection;
			main_color.rgb = main_color.rgb*light;
			UNITY_APPLY_FOG(input.fogCoord, main_color);
			return main_color;
         }
#endif

      ENDCG
    }
  }

  SubShader{
	  LOD 200

	  Tags{ "RenderType" = "Opaque" }

	  Pass{
	  Name "FORWARD"
	  Tags{ "LightMode" = "ForwardBase" }

	  // Disable lighting, we're only using the lightmap
	  Lighting Off

	  CGPROGRAM
	  // Must be a vert/frag shader, not a surface shader: the necessary variables
	  // won't be defined yet for surface shaders.

#pragma vertex vert
#pragma fragment frag
	  // make fog work
#pragma multi_compile_fwdbase
#pragma multi_compile_fog

#pragma debug

#ifdef LIGHTMAP_ON	  

#include "UnityCG.cginc"

	  struct v2f {
	  float4 pos : SV_POSITION;
	  float3 viewDir : COLOR;
	  float4 uv0 : TEXCOORD0;
	  UNITY_FOG_COORDS(1)
  };

  struct appdata_lightmap {
	  float4 vertex : POSITION;
	  float3 normal : NORMAL;
	  float4 tangent : TANGENT;
	  float2 texcoord : TEXCOORD0;
	  float2 texcoord1 : TEXCOORD1;
  };

  sampler2D _MainTex,_BumpMap;

  float4 _MainTex_ST; // Define this since its expected by TRANSFORM_TEX; it is also pre-populated by Unity.

  half _Shininess, _SpecStr;

  v2f vert(appdata_lightmap v) {
	  v2f o;
	  UNITY_INITIALIZE_OUTPUT(v2f, o);
	  o.pos = UnityObjectToClipPos(v.vertex);

	  // UnityCG.cginc - Transforms 2D UV by scale/bias property
	  // #define TRANSFORM_TEX(tex,name) (tex.xy * name##_ST.xy + name##_ST.zw)
	  o.uv0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);

	  // Use `unity_LightmapST` NOT `unity_Lightmap_ST`
	  o.uv0.zw = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;

	  UNITY_TRANSFER_FOG(o,o.pos);

	  return o;
  }

  half4 frag(v2f i) : COLOR{
	  half4 main_color = tex2D(_MainTex, i.uv0.xy);

	  // Decodes lightmaps:
	  half3 light_map_val = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv0.zw));

	  main_color.rgb = main_color.rgb*light_map_val;

	  UNITY_APPLY_FOG(i.fogCoord, main_color);

	  return main_color;
  }
#else	  
#include "UnityCG.cginc" 
	  uniform float4 _LightColor0;
  // color of light source (from "Lighting.cginc")

  // User-specified properties
  sampler2D _MainTex, _BumpMap, _SpecTex;

  float4 _MainTex_ST; // Define this since its expected by TRANSFORM_TEX; it is also pre-populated by Unity.

  half _Shininess, _SpecStr;

  struct vertexInput {
	  float4 vertex : POSITION;
	  float3 normal : NORMAL;
	  float2 texcoord : TEXCOORD0;
  };
  struct vertexOutput {
	  float4 pos : SV_POSITION;
	  float4 uv0 : TEXCOORD0;
	  float4 posWorld : TEXCOORD1;
	  float3 normalDir : TEXCOORD2;
	  float3 vertexLighting : TEXCOORD3;
	  UNITY_FOG_COORDS(4)
  };

  vertexOutput vert(vertexInput input)
  {
	  vertexOutput output;
	  UNITY_INITIALIZE_OUTPUT(vertexOutput, output);

	  float4x4 modelMatrix = unity_ObjectToWorld;
	  float4x4 modelMatrixInverse = unity_WorldToObject;
	  output.uv0.xy = input.texcoord;
	  output.posWorld = mul(modelMatrix, input.vertex);
	  output.normalDir = normalize(
		  mul(float4(input.normal, 0.0), modelMatrixInverse).xyz);
	  output.pos = UnityObjectToClipPos(input.vertex);

	  // Diffuse reflection by four "vertex lights"            
	  output.vertexLighting = float3(0.0, 0.0, 0.0);
#ifdef VERTEXLIGHT_ON
	  for (int index = 0; index < 4; index++)
	  {
		  float4 lightPosition = float4(unity_4LightPosX0[index],
			  unity_4LightPosY0[index],
			  unity_4LightPosZ0[index], 1.0);

		  float3 vertexToLightSource =
			  lightPosition.xyz - output.posWorld.xyz;
		  float3 lightDirection = normalize(vertexToLightSource);
		  float squaredDistance =
			  dot(vertexToLightSource, vertexToLightSource);
		  float attenuation = 1.0 / (1.0 +
			  unity_4LightAtten0[index] * squaredDistance);
		  float3 diffuseReflection = attenuation
			  * unity_LightColor[index].rgb * max(0.0, dot(output.normalDir, lightDirection));

		  output.vertexLighting =
			  output.vertexLighting + diffuseReflection;
	  }
#endif
	  UNITY_TRANSFER_FOG(output, output.pos);
	  return output;
  }

  float4 frag(vertexOutput input) : COLOR
  {
	  half4 main_color = tex2D(_MainTex, input.uv0.xy);

	  float3 normalDirection = normalize(input.normalDir);
	  float3 viewDirection = normalize(
		  _WorldSpaceCameraPos - input.posWorld.xyz);
	  float3 lightDirection;
	  float attenuation;

	  if (0.0 == _WorldSpaceLightPos0.w) // directional light?
	  {
		  attenuation = 1.0; // no attenuation
		  lightDirection =
			  normalize(_WorldSpaceLightPos0.xyz);

	  }
	  else // point or spot light
	  {
		  float3 vertexToLightSource =
			  _WorldSpaceLightPos0.xyz - input.posWorld.xyz;
		  float distance = length(vertexToLightSource);
		  attenuation = 1.0 / distance; // linear attenuation 
		  lightDirection = normalize(vertexToLightSource);

	  }

	  float3 ambientLighting =
		  UNITY_LIGHTMODEL_AMBIENT.rgb;

	  float3 diffuseReflection =
		  attenuation * _LightColor0.rgb * max(0.0, dot(normalDirection, lightDirection));

	  float3 light = input.vertexLighting + ambientLighting + diffuseReflection;
	  main_color.rgb = main_color.rgb*light;
	  UNITY_APPLY_FOG(input.fogCoord, main_color);
	  return main_color;
  }
#endif

	  ENDCG
  }
  }

  fallback "Diffuse"
}