Shader "Hidden/Shader/TextureBlitPostProcess"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

		Pass
		{
			HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

			#pragma vertex vert
			#pragma fragment frag

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			// List of properties to control your post process effect
			float2 _InputResolution;
			sampler2D _TextureToBlit1;
			sampler2D _TextureToBlit2;
			sampler2D _TextureToBlit3;
			sampler2D _TextureToBlit4;

			struct Attributes
			{
				float4 positionOS       : POSITION;
				float2 uv               : TEXCOORD0;
			};

			struct Varyings
			{
				float2 uv        : TEXCOORD0;
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};


			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.vertex = vertexInput.positionCS;
				output.uv = input.uv;

				return output;
			}

			float4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				//Coordinates in input uv space
				float2 scaledCoord = input.uv * _ScreenSize.xy / _InputResolution;

				float3 outColor = float3(0, 0, 0);
				float2 offset = float2(0, 1);

				if (scaledCoord.x > 1 && scaledCoord.y <= 1) {
					//Bottom Right
					outColor = tex2D(_TextureToBlit4, scaledCoord - offset.yx);
				}
				else if (scaledCoord.x > 1 && scaledCoord.y > 1) {
					//Top Right
					outColor = tex2D(_TextureToBlit2, scaledCoord - offset.yy);
				}
				else if (scaledCoord.x <= 1 && scaledCoord.y > 1) {
					//Top Left
					outColor = tex2D(_TextureToBlit1, scaledCoord - offset.xy);
				}
				else {
					//Bottom Left
					outColor = tex2D(_TextureToBlit3, scaledCoord);
				}

				return float4(outColor, 1);

			}

			ENDHLSL
		}
	}

		FallBack "Diffuse"
}
