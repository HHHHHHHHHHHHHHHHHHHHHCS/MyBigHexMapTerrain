﻿Shader "HCS/S_Road"
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
		Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
		LOD 200
		Offset -1,-1

		CGPROGRAM
		
		#pragma surface surf Standard alpha decal:blend
		#pragma target 3.0
		
		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
		};
		
		sampler2D _MainTex;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			float4 noise = tex2D(_MainTex,IN.worldPos.xz*0.025);
			fixed4 c = _Color*(noise.y*0.75+0.25);
			float blend = IN.uv_MainTex.x;

			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			blend = smoothstep(0.4,0.7,blend);
			blend*=noise.x+0.5;
			o.Alpha = blend;
		}
		ENDCG
		
	}
	FallBack "Diffuse"
}
