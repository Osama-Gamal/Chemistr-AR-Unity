Shader "LiquidVolume/Editor/DepthTexPreview"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #pragma multi_compile _ LIQUID_VOLUME_FP_RENDER_TEXTURES

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
            sampler2D _VLBackBufferTexture;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float4 depth = tex2D(_VLBackBufferTexture, i.uv);
                #if LIQUID_VOLUME_FP_RENDER_TEXTURES
		        	depth /= _ProjectionParams.z;
			    #endif
                return depth;
			}
			ENDCG
		}
	}
}
