Shader "HCS/S_VertexColors"
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
		Tags { "RenderType" = "Opaque" }
		LOD 200
		CGPROGRAM
		
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0
		
		struct Input
		{
			float2 uv_MainTex;
			float4 color: COLOR;
		};
		
		sampler2D _MainTex;
		half _Glossiness;
		half _Metallic;
		half4 _Color;
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			half4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb * IN.color;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		
		ENDCG
		
	}
}
