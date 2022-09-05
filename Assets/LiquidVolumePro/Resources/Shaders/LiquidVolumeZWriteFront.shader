Shader "LiquidVolume/ZWriteFront" {
	Properties {
		_Color ("Color", Color) = (0,0,1,1)
	}
	SubShader {
	
	Pass {	
        Cull Front
        ZWrite Off
		//ZTest Always
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
        #pragma fragmentoption ARB_precision_hint_fastest
        #pragma multi_compile _ LIQUID_VOLUME_FP_RENDER_TEXTURES
		#include "UnityCG.cginc"

		struct v2f {
			float4 pos: SV_POSITION;
            float3 worldPos: TEXCOORD0;
		};
		
		v2f vert(appdata_base v) {
    		v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    		return o;
		}

		inline float3 ProjectOnPlane(float3 v, float3 planeNormal) {
			float sqrMag = dot(planeNormal, planeNormal);
			float dt = dot(v, planeNormal);
			return v - planeNormal * dt / sqrMag;
		}

		float4 frag (v2f i) : SV_Target {
            float distPersp = distance(_WorldSpaceCameraPos, i.worldPos);

			float3 cameraForward = UNITY_MATRIX_V[2].xyz;
			float3 orthoCameraPos = ProjectOnPlane(i.worldPos - _WorldSpaceCameraPos, cameraForward) + _WorldSpaceCameraPos;
			float distOrtho = distance(orthoCameraPos, i.worldPos);

			float dist = lerp(distPersp, distOrtho, unity_OrthoParams.w);

            #if LIQUID_VOLUME_FP_RENDER_TEXTURES
			return dist;
			#else
			return EncodeFloatRGBA( 1.0 / (1.0 + dist) );
			#endif
		}
		ENDCG
	} 	
}
}

    