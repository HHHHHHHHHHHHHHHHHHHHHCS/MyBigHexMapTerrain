Shader "HCS/S_Water"
{
	Properties
	{
		_Color ("Color", Color) = (1, 1, 1, 1)
		_MainTex ("Albedo (RGB)", 2D) = "white" { }
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Specular ("Specular", Color) = (0.2, 0.2, 0.2)
	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		LOD 200
		
		CGPROGRAM
		
		#include "Water.cginc"
		#include "HexcellData.cginc"
		
		#pragma surface surf StandardSpecular alpha vertex:vert
		#pragma multi_compile _ HEX_MAP_EDIT_MODE
		#pragma target 3.0
		
		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
			float2 visibility;
		};
		
		sampler2D _MainTex;
		half _Glossiness;
		half4 _Color;
		half3 _Specular;
		
		void vert(inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);
			data.visibility = GetVisibilityBy3(v);
		}
		
		void surf(Input IN, inout SurfaceOutputStandardSpecular o)
		{
			float waves = Waves(IN.worldPos.xz, _MainTex);
			half4 c = saturate(_Color + waves);
			float explored = IN.visibility.y;
			o.Albedo = c.rgb * IN.visibility.x;
			o.Specular = _Specular * explored;
			o.Smoothness = _Glossiness;
			o.Occlusion = explored;
			o.Alpha = c.a * explored;
		}
		ENDCG
		
	}
}
