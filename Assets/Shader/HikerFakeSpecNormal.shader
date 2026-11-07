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

Shader "Hiker/HikerFakeSpecNormal" {

  Properties {
    _MainTex ("Texture 1", 2D) = "white" {}
    _MainColor("Main Color", Color) = (1,1,1,1)
	_BumpMap ("Normalmap", 2D) 	= "bump" {}
	_SpecTex ("SpecMap", 2D) = "white" {}
	_SpecStr ("Specular Strength", Float ) = 1
	_Shininess ("Shininess", Range (0.03, 10)) = 0.078125
	_ShadowStrength ("ShadowStrength", Range (0.0, 1)) = 0.5

	 
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
		#include "Lighting.cginc"
		#include "AutoLight.cginc"

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
		SHADOW_COORDS(5) 
		float4 uv1 : TEXCOORD6;

      };

      struct appdata_lightmap {
        float4 vertex : POSITION;
		float3 normal : NORMAL;
		float4 tangent : TANGENT;
        float2 texcoord : TEXCOORD0;
        float2 texcoord1 : TEXCOORD1;
      };

      sampler2D _MainTex,_BumpMap,_SpecTex;

      float4 _MainTex_ST,_BumpMap_ST,_MainColor; // Define this since its expected by TRANSFORM_TEX; it is also pre-populated by Unity.
	  
	  half _Shininess, _SpecStr,_ShadowStrength;

      v2f vert(appdata_lightmap v) {
        v2f o;
		UNITY_INITIALIZE_OUTPUT(v2f, o);
        o.pos = UnityObjectToClipPos(v.vertex);

        // UnityCG.cginc - Transforms 2D UV by scale/bias property
        // #define TRANSFORM_TEX(tex,name) (tex.xy * name##_ST.xy + name##_ST.zw)
        o.uv0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
		o.uv1.xy = TRANSFORM_TEX(v.texcoord, _BumpMap);

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
		TRANSFER_SHADOW(o)

		UNITY_TRANSFER_FOG(o,o.pos);

        return o;
      }

      half4 frag(v2f i) : COLOR {
        half4 main_color = tex2D(_MainTex, i.uv0.xy) * _MainColor;
		fixed shadow = SHADOW_ATTENUATION(i);

        // Decodes lightmaps:
		half3 light_map_val = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv0.zw));
		if(shadow < 1)
			light_map_val -= (1 - shadow) * _ShadowStrength ;
#ifdef DIRLIGHTMAP_OFF
		main_color.rgb = main_color.rgb*light_map_val;

#else
		half spec_tex = tex2D (_SpecTex, i.uv0.xy).g;
		spec_tex*=_SpecStr;

		half3 normal = UnpackNormal(tex2D(_BumpMap, i.uv1.xy));

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

		float nh = max (0, dot ( normal_world, h));

		float diffuseLight = dot(normal_world, _WorldSpaceLightPos0);
		if (diffuseLight <= 0.0f)
			nh = 0;

		float spec = pow (nh, _Shininess * 128.0);

		float specColor = spec_tex * spec;

	    main_color.rgb = (main_color.rgb +specColor) * light_dir; // babe fake light model
		//main_color.w = 1;
		//main_color.rgb = float3(1,1,1);
#endif
		UNITY_APPLY_FOG(i.fogCoord, main_color);

		return main_color;
      }
#else	  
            #include "UnityCG.cginc" 
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

        // uniform float4 _LightColor0; 
            // color of light source (from "Lighting.cginc")

         // User-specified properties
         sampler2D _MainTex,_BumpMap,_SpecTex;

         float4 _MainTex_ST,_BumpMap_ST,_MainColor; // Define this since its expected by TRANSFORM_TEX; it is also pre-populated by Unity.
	  
	     half _Shininess, _SpecStr, _ShadowStrength;
 
         struct vertexInput {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
			float2 texcoord : TEXCOORD0;	
			float4 tangent: TANGENT;
         };
         struct vertexOutput {
            float4 pos : SV_POSITION;
			float4 uv0 : TEXCOORD0;
            float4 posWorld : TEXCOORD1;
            float3 normalDir : TEXCOORD2;
            float3 vertexLighting : TEXCOORD3;
			UNITY_FOG_COORDS(4)
			SHADOW_COORDS(5) 
			float3 normalWorld: TEXCOORD6;
			float3 tangentWorld: TEXCOORD7;
			float3 binormalWorld: TEXCOORD8;
			float4 uv1 : TEXCOORD9;

         };
 
         vertexOutput vert(vertexInput input)
         {          
            vertexOutput output;
			UNITY_INITIALIZE_OUTPUT(vertexOutput, output);
 
            float4x4 modelMatrix = unity_ObjectToWorld;
            float4x4 modelMatrixInverse = unity_WorldToObject; 
			output.uv0.xy = TRANSFORM_TEX(input.texcoord, _MainTex);
			output.uv1.xy = TRANSFORM_TEX(input.texcoord, _BumpMap);

			//output.uv0.xy = input.texcoord;
            output.posWorld = mul(modelMatrix, input.vertex);
            output.normalDir = normalize(
               mul(float4(input.normal, 0.0), modelMatrixInverse).xyz);
            output.pos = UnityObjectToClipPos(input.vertex);
 				output.normalWorld = normalize( mul( float4( input.normal, 0.0 ), unity_WorldToObject ).xyz );		
				output.tangentWorld = normalize( mul( unity_ObjectToWorld, input.tangent ).xyz );
				output.binormalWorld = normalize( cross( output.normalWorld, output.tangentWorld ) );
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
			UNITY_TRANSFER_SHADOW(output, output.uv0)

			UNITY_TRANSFER_FOG(output,output.pos);
            return output;
         }
 
         float4 frag(vertexOutput input) : COLOR
         {
		    half4 main_color = tex2D(_MainTex, input.uv0.xy) * _MainColor;

            float3 normalDirection = normalize(input.normalDir); 
			float4 texN = tex2D( _BumpMap, input.uv1.xy );
				
				// unpackNormal Function
				float3 localCoords = float3(2.0 * texN.ag - float2(1.0,1.0), 0.0);
				//localCoords.z = 1.0 - 0.5 * dot( localCoords, localCoords );
				localCoords.z = 1.0;
				//localCoords.z = _BumpDepth;
				
				// Normal Transpose Matrix
				float3x3 local2WorldTranspose = float3x3(
					input.tangentWorld,
					input.binormalWorld,
					input.normalWorld
				);
			normalDirection = normalize( mul( localCoords, local2WorldTranspose ) );

            float3 viewDirection = normalize(
               _WorldSpaceCameraPos - input.posWorld.xyz);
            float3 lightDirection;
            float attenuation;
 			fixed shadow = SHADOW_ATTENUATION(input);

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

			float3 light = input.vertexLighting  + ambientLighting + diffuseReflection * shadow + specularReflection;
			main_color.rgb = main_color.rgb*light ;
			//main_color.rgb = float3(1,1,1);
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

  sampler2D _MainTex,_BumpMap,_SpecTex;

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
  sampler2D _MainTex,_BumpMap,_SpecTex;

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

	  //float3 normalDirection = normalize(input.normalDir);

	  /*float3 viewDirection = normalize(
		  _WorldSpaceCameraPos - input.posWorld.xyz);*/

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

	  float3 diffuseReflection = attenuation * _LightColor0.rgb * max(0.0, dot(input.normalDir, lightDirection));

	  float3 light = input.vertexLighting + UNITY_LIGHTMODEL_AMBIENT.rgb + diffuseReflection;
	  main_color.rgb = main_color.rgb*light;
	 // main_color.rgb = float3(1,1,1)
	  UNITY_APPLY_FOG(input.fogCoord, main_color);
	  return main_color;
  }
#endif

	  ENDCG
  }
  }

  fallback "Diffuse"
}