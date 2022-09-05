#include "LVLiquidPassBase.cginc"

		fixed4 raymarch(float4 vertex, float3 rd, float t0, float t1) {

			// ray-march smoke
			fixed4 sumSmoke = fixed4(0,0,0,0);
			float stepSize = (t1 - t0) / (float)_SmokeRaySteps;
			float4 dir  = float4(rd * stepSize, 0);
			float4 rpos = float4(wsCameraPos + rd * t0, 0);
			float4 disp = float4(0, _Time.x * _Turbulence.x * _Size.y * _SmokeSpeed, 0, 0);
            BEGIN_LOOP(k,_SmokeRaySteps,5)
				half n = SAMPLE_NOISE_3D(_NoiseTex, (rpos - disp) * _Scale.x).r;
				float py = (_LevelPos - rpos.y)/_Size.y;
				n = saturate(n + py * _SmokeHeightAtten);
				fixed4 lc  = fixed4(_SmokeColor.rgb, n * _SmokeColor.a);
				lc.rgb *= lc.aaa;
				fixed deep = exp(py * _SmokeAtten);
				lc *= deep;
				sumSmoke += lc * (1.0-sumSmoke.a);
                rpos += dir;
			END_LOOP
			return sumSmoke;
		}
