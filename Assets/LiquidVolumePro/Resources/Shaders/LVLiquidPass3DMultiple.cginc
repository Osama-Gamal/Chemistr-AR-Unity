#include "LVLiquidPassBase.cginc"

        float  _DitherStrength;
        float _SizeWorld;

        float _MurkinessSpeed;
        float3 _BubblesData; // x = scale, y = vertical speed, z = brightness


#if defined(WEBGL_OR_LEGACY)
        sampler2D _LayersPropertiesTex;
        sampler2D _LayersColorsTex;
        sampler2D _LayersColors2Tex;
        sampler2D _NoiseTexUnwrapped;

        // Lower quality with a single texture fetch
        /*
        inline float4 GetNoiseUnwrapped(float4 pos) {
            float4 f = frac(pos);
            f.z = (floor(f.y * 64.0) + 0.5) / 64.0 + f.z / 64.0;
            return tex2Dlod(_NoiseTexUnwrapped, float4(f.xz,0,0));
        }
        */

        inline float4 GetNoiseUnwrapped(float4 pos) {
            float4 f = frac(pos);
            float t = f.y * 64.0;
            float by = floor(t);
            float4 n0 = tex2Dlod(_NoiseTexUnwrapped, float4(f.x, (by + f.z) / 64.0,0,0));
            float4 n1 = tex2Dlod(_NoiseTexUnwrapped, float4(f.x, (by + 1.0 + f.z) / 64.0,0,0));
            return lerp(n0, n1, t-by);
        }


        #define SET_LAYER_INDEX(y) float layerIndex = -y / _SizeWorld;
        #define UPDATE_LAYER_INDEX(y) layerIndex = -y / _SizeWorld;
        #define SAMPLE_LAYER_PROPERTIES(index) tex2Dlod(_LayersPropertiesTex, float4(0,index,0,0))
        #define SAMPLE_LAYER_COLOR(index) tex2Dlod(_LayersColorsTex, float4(0,index,0,0))
        #define SAMPLE_LAYER_COLOR2(index) tex2Dlod(_LayersColors2Tex, float4(0,index,0,0))
        #define SAMPLE_NOISE_UNWRAPPED(pos) GetNoiseUnwrapped(pos)

#else
        fixed4 _LayersColors[256];
        fixed4 _LayersColors2[256];
        float4 _LayersProperties[256];

        #define SET_LAYER_INDEX(y) int layerIndex = clamp((int)(-255.0 * y / _SizeWorld), 0, 255);
        #define UPDATE_LAYER_INDEX(y) layerIndex = clamp((int)(-255.0 * y / _SizeWorld), 0, 255);
        #define SAMPLE_LAYER_PROPERTIES(index) _LayersProperties[index]
        #define SAMPLE_LAYER_COLOR(index) _LayersColors[index]
        #define SAMPLE_LAYER_COLOR2(index) _LayersColors2[index]
        #define SAMPLE_NOISE_UNWRAPPED(pos) tex3Dlod(_NoiseTex, pos)

#endif
      
		inline float getRandomFast(float2 uv) {
			float2 p = uv; // + _Time.ww;
			p -= floor(p * 0.01408450704) * 71.0;    
			p += float2( 26.0, 161.0 );                                
			p *= p;                                          
			return frac(p.x * p.y * 0.001051374728);
		}
        


		fixed4 raymarch(float4 vertex, float3 rd, float t0, float t1) {

	        float3 wpos = wsCameraPos + rd * t0;
	        
			float turbulence = (tex2D(_NoiseTex2D, vertex.xz).g - 0.5) * _Turbulence.x;
			turbulence += sin(vertex.w) * _Turbulence.y;
			turbulence *= 0.05 * _Size.y * _FoamTurbulence;
			_LevelPos += turbulence;
			_FoamMaxPos += turbulence;

			// compute level of surface liquid (t2)
			float2 rdy   = rd.xz / rd.y;
			float delta = sqrt(1.0 + dot (rdy, rdy));
			float h = abs(wpos.y - _LevelPos);
			float t2 = t0 + h * delta; // length(delta * h.xx);;
						
			// compute foam level (t3)
			float hf = abs(wpos.y - _FoamMaxPos);
			float t3 = t0 + hf * delta;

			// ray-march smoke
			float tmin, tmax;
			#if defined(LIQUID_VOLUME_SMOKE)
			fixed4 sumSmoke = fixed4(0,0,0,0);
			if (wpos.y > _LevelPos) {
				tmin = t0;
				tmax = rd.y<0 ? min(t2,t1) : t1;
				float stepSize = (tmax - tmin) / (float)_SmokeRaySteps;
				float4 dir  = float4(rd * stepSize, 0);
				float4 rpos = float4(wsCameraPos + rd * tmin, 0);
				float4 disp = float4(0, _Time.x * _Turbulence.x * _Size.y * _SmokeSpeed, 0, 0);
                BEGIN_LOOP(k,_SmokeRaySteps,5)
                    fixed n = SAMPLE_NOISE_3D(_NoiseTex, (rpos - disp) * _Scale.x).r;
					float py = (_LevelPos - rpos.y)/_Size.y;
					n = saturate(n + py * _SmokeHeightAtten);
					fixed4 lc  = fixed4(_SmokeColor.rgb, n * _SmokeColor.a);
					lc.rgb *= lc.aaa;
					fixed deep = exp(py * _SmokeAtten);
					lc *= deep;
					sumSmoke += lc * (1.0-sumSmoke.a);
                    rpos += dir;
				END_LOOP
			}
			#endif

			// ray-march foam
			tmax = min(t3,t1), tmin = t0;
			float sy = sign(rd.y);
			if (wpos.y > _FoamMaxPos) {
				tmin = tmax;
				tmax = min(t2, t1) * -sy;
			} else if (wpos.y < _LevelPos) {
				tmin  = min(t2,t1);
				tmax *= _FoamBottom * sy;
			} else if (rd.y<0) {
				tmax = min(t2, t1);
			}
			fixed4 sumFoam  = fixed4(0,0,0,0);
			if (tmax>tmin) {
				float stepSize = (tmax - tmin) / (float)_FoamRaySteps;
				float4 dir  = float4(rd * stepSize, 0);
				float4 rpos = float4(wsCameraPos + rd * tmin, 0);
				rpos.y -= _LevelPos;
				float foamThickness = _FoamMaxPos - _LevelPos;
				float4 disp = float4(_Time.x, 0, _Time.x, 0) * _Turbulence.x * _Size.w * _FoamTurbulence;
                BEGIN_LOOP(k,_FoamRaySteps,7)

					float h = saturate( rpos.y / foamThickness );
					float n = saturate(SAMPLE_NOISE_3D(_NoiseTex, (rpos - disp) * _Scale.y ).r + _FoamDensity);
					if (n>h) {
						fixed4 lc  = fixed4(_FoamColor.rgb, n-h);
						lc.a   *= _FoamColor.a;
						lc.rgb *= lc.aaa;
						fixed deep = saturate(rpos.y * _FoamWeight / foamThickness);
						lc *= deep;
						sumFoam += lc * (1.0 - sumFoam.a);
					}
                    
                    rpos += dir;

				END_LOOP
				sumFoam *= 1.0 + _FoamDensity;
			}	
            
			// ray-march liquid
			if (wpos.y > _LevelPos) {
				tmin = t2;
				tmax = t1 * -sy; // address case where ray does not cross liquid; this makes tmax < tmin in this case skipping the whole if block below
			} else {
				tmin = t0;
				tmax = min(t2,t1);
			}
			fixed4 sum = fixed4(0,0,0,0);
			if (tmax>tmin) {
                float4 rpos  = float4(wsCameraPos + rd * tmin, 0); // tmin or t0 ? does not matter to move to level pos; tmin will make bubbles position correct, so we stick with tmin (t0 may reduce banding on certain cases)
				float stepSize = (t1-t0) / (float)_LiquidRaySteps;
				float4 dir   = float4(rd * stepSize, 0);
            
                rpos += dir * (getRandomFast(rpos.xy * 100.0) * _DitherStrength);
                float4 disp  = float4(_Turbulence.y, 1.5, _Turbulence.y, 0) * (_MurkinessSpeed * _Size.y);
                disp.y -= _LevelPos - turbulence;
                rpos.y -= _LevelPos - turbulence;

                BEGIN_LOOP(k,_LiquidRaySteps,10)
                    
                    SET_LAYER_INDEX(rpos.y);

                    // turbulence
                    float layerTurbulence = SAMPLE_LAYER_PROPERTIES(layerIndex).z;
                    float4 xpos = float4(rpos.x, rpos.y + layerTurbulence * turbulence, rpos.z, 0);
                    UPDATE_LAYER_INDEX(xpos.y);

                    xpos.xyz -= _Center;

                    // murkiness
                    float4 properties = SAMPLE_LAYER_PROPERTIES(layerIndex);
                    float layerScale = properties.y;
					fixed n = SAMPLE_NOISE_3D(_NoiseTex, (xpos - disp * layerTurbulence) * layerScale).r;
                    
					// accumulate color
                    fixed muddy = properties.x;
                    fixed4 layerColor = SAMPLE_LAYER_COLOR(layerIndex);
                    fixed4 layerColor2 = SAMPLE_LAYER_COLOR2(layerIndex);
                    fixed4 lc  = lerp(layerColor, layerColor2, smoothstep(0, 1.0, n) * muddy);
					lc.a = layerColor.a;
					lc.rgb *= lc.aaa;
					sum += lc * (1.0-sum.a);

                    // bubbles; uses tex3Dlod
                    #if defined(LIQUID_VOLUME_BUBBLES)
                        fixed4 ba = SAMPLE_NOISE_UNWRAPPED ( (xpos - float4(turbulence, 0, turbulence, 0)) * _BubblesData.x - float4(0, _BubblesData.y, 0,0) );
                        fixed4 bb = SAMPLE_NOISE_UNWRAPPED ( (xpos + float4(turbulence, 0, turbulence, 0)) * _BubblesData.x - float4(0.5, _BubblesData.y * 1.5 + 0.5, 0.5, 0) );
                        fixed4 b = ba+bb;
                        fixed3 bnorm = b.yzw - 1.0;
                        fixed fresnel = abs(dot(rd, bnorm));
                        lc.rgb += fresnel * _BubblesData.z;
                        lc.a = fresnel;
                        lc.rgb *= lc.aaa; // 0.1
                        lc *= properties.w;
                        sum += lc * (1.0-sum.a);
                    #endif

                    rpos += dir;

				END_LOOP
			}
            
			// Final blend
			if (wpos.y>_LevelPos) {
				#if defined(LIQUID_VOLUME_SMOKE)
				    fixed4 lfoam = sumFoam * (1.0 - sumSmoke.a);
				    fixed4 liquid = sum * (1.0 - lfoam.a) * (1.0 - sumSmoke.a);
				    sum = sumSmoke + lfoam + liquid;
				#else
				    fixed4 liquid = sum * (1.0 - sumFoam.a);
				    sum = sumFoam + liquid;
				#endif
			} else {
				fixed4 lfoam = sumFoam * (1.0 - sum.a);
				sum = sum + lfoam;
			}
            
			return sum;
		}
