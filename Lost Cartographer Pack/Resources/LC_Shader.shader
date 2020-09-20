Shader "Custom/LC_Shader"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		// Height shaders attributes
		int LC_RenderType = 0;	// 0 = Default, 1 = Height discrete, 2 = Height continuous
		const static int maxColorCount = 8;
		float minHeight;
		float maxHeight;
		int numColors;
		float3 colors[maxColorCount];		

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		float inverseLerp(float a, float b, float value) {
			return saturate((value - a) / (b - a));
		}

		int roundToInt(float x) {
			float decimals = x - (int)x;
			return (decimals >= 0.5) ? x + 1 : x;
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// Default
			if (LC_RenderType == 0) {
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
				o.Albedo = c.rgb;
				o.Alpha = c.a;
			}
			// Height discrete or continuous
			else {
				float percentage = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
				float colorFloatIndex = percentage * (numColors - 1);

				// Discrete
				if (LC_RenderType == 1) {
					o.Albedo = colors[roundToInt(colorFloatIndex)];
				}
				// Continuous
				else if (LC_RenderType == 2) {
					int colorIndex = (int)colorFloatIndex;
					float indexDecimals = colorFloatIndex - (float)colorIndex;
					o.Albedo = (1 - indexDecimals) * colors[colorIndex] + indexDecimals * colors[colorIndex + 1];
				}
			}

			// Apply mettalic and glossiness
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
