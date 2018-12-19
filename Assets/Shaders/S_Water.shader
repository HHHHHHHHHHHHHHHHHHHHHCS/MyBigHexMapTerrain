Shader "HCS/S_Water"
{
	Properties
	{
		_Color ("Color", Color) = (1, 1, 1, 1)
		_MainTex ("Albedo (RGB)", 2D) = "white" { }
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Metallic ("Metallic", Range(0, 1)) = 0.0
	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		LOD 200
		
		CGPROGRAM
		
		#pragma surface surf Standard alpha vertex:vert
		#pragma target 3.0
		
		#include "Water.cginc"
		#include "HexcellData.cginc"
		
		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
			float visibility;
		};
		
		sampler2D _MainTex;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		
		void vert(inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);
			data.visibility = GetVisibilityBy3(v);
		}
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			float waves = Waves(IN.worldPos.xz, _MainTex);
			
			fixed4 c = saturate(_Color + waves);
			o.Albedo = c * IN.visibility	;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
		
	}
}
