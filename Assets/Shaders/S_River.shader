﻿Shader "HCS/S_River"
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
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent+1" }
		LOD 200
		
		CGPROGRAM
		
		#include "Water.cginc"
		#include "HexCellData.cginc"
		
		#pragma surface surf Standard alpha  vertex:vert
		#pragma target 3.0
		
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
			data.visibility = GetVisibilityBy2(v);
		}
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			float river = River(IN.uv_MainTex, _MainTex);
			
			fixed4 c = saturate(_Color + river);
			o.Albedo = c.rgb * IN.visibility;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
		
	}
}
