Shader "HCS/S_Feature"
{
	Properties
	{
		_Color ("Color", Color) = (1, 1, 1, 1)
		_MainTex ("Albedo (RGB)", 2D) = "white" { }
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Specular ("Specular", Color) = (0.2, 0.2, 0.2)
		_BackgroundColor ("Background Color", Color) = (0.4, 0.4, 0.4)
		[NoScaleOffset] _GridCoordinates ("Grid Coordinates", 2D) = "white" { }
	}
	
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		
		LOD 200
		
		CGPROGRAM
		
		#include "HexcellData.cginc"
		
		#pragma surface surf StandardSpecular fullforwardshadows vertex:vert
		#pragma multi_compile _ HEX_MAP_EDIT_MODE
		#pragma target 3.0
		
		sampler2D _MainTex, _GridCoordinates;
		half _Glossiness;
		half3 _Specular;
		half4 _Color;
		half3 _BackgroundColor;
		
		struct Input
		{
			float2 uv_MainTex;
			float2 visibility;
		};
		
		float4 GetCellData(float2 cellDataCoordinates)
		{
			float2 uv = cellDataCoordinates + 0.5;
			uv.x *= _HexCellData_TexelSize.x;
			uv.y *= _HexCellData_TexelSize.y;
			return FilterCellData(tex2Dlod(_HexCellData, float4(uv, 0, 0)));
		}
		
		void vert(inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);
			float3 pos = mul(unity_ObjectToWorld, v.vertex);//surf用 Unity_ObjectToWorld 好像有问题
			
			float4 gridUV = float4(pos.xz, 0, 0);
			gridUV.x *= 1 / (4 * 8.66025404);//因为图片的格子是两格的所以尺寸都缩小2了
			gridUV.y *= 1 / (2 * 15.0);
			float2 cellDataCoordinates = floor(gridUV.xy) + tex2Dlod(_GridCoordinates, gridUV).rg;
			cellDataCoordinates *= 2;//乘2 变成正确的尺寸
			
			float4 cellData = GetCellData(cellDataCoordinates);
			data.visibility.x = cellData.x;
			data.visibility.x = lerp(0.25, 1, data.visibility.x);
			data.visibility.y = cellData.y;
		}
		
		void surf(Input IN, inout SurfaceOutputStandardSpecular o)
		{
			half4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			float explored = IN.visibility.y;
			o.Albedo = c.rgb * (IN.visibility.x * explored);
			o.Specular = _Specular * explored;
			o.Occlusion = explored;
			o.Emission = _BackgroundColor * (1 - explored);
			o.Alpha = c.a;
		}
		
		ENDCG
		
	}
}