Shader "HCS/S_Road"
{
	Properties
	{
		_Color ("Color", Color) = (1, 1, 1, 1)
		_MainTex ("Albedo (RGB)", 2D) = "white" { }
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Specular ("Specular", Color) =(0.2, 0.2, 0.2)
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1" }
		LOD 200
		Offset -1, -1
		
		CGPROGRAM
		
		#include "HexCellData.cginc"
		
		#pragma surface surf StandardSpecular alpha decal:blend vertex:vert
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
			data.visibility = GetVisibilityBy2(v);
		}
		
		void surf(Input IN, inout SurfaceOutputStandardSpecular o)
		{
			float4 noise = tex2D(_MainTex, IN.worldPos.xz * 0.025);
			half4 c = _Color * (noise.y * 0.75 + 0.25);
			float blend = IN.uv_MainTex.x;
			blend = smoothstep(0.4, 0.7, blend);
			blend *= noise.x + 0.5;
			
			float explored = IN.visibility.y;
			o.Albedo = c.rgb * IN.visibility.x;
			o.Specular = _Specular * explored;
			o.Smoothness = _Glossiness;
			o.Occlusion = explored;
			o.Alpha = blend * explored;
		}
		ENDCG
		
	}
}
