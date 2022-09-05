// Set by the asset automatically
#define FORCE_GLES_COMPATIBILITY 0
#define LIQUID_VOLUME_SCATTERING
#define LIQUID_VOLUME_SMOKE
#define LIQUID_VOLUME_BUBBLES
#define LIQUID_VOLUME_FP_RENDER_TEXTURES
//#define LIQUID_VOLUME_MONOSCOPIC
//#define LIQUID_VOLUME_ORTHO

		struct Input {
			float3 realPos;
			float4 vertex;
			#if LIQUID_VOLUME_DEPTH_AWARE || LIQUID_VOLUME_DEPTH_AWARE_PASS || LIQUID_VOLUME_IRREGULAR
			float4 screenPos;
			#endif
			float3 camPos;
		};

		sampler2D _NoiseTex2D;
		#if LIQUID_VOLUME_DEPTH_AWARE
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			float _DepthAwareOffset;
		#endif

		#if LIQUID_VOLUME_DEPTH_AWARE_PASS
			sampler2D _VLFrontBufferTexture;
		#endif
		
		#if LIQUID_VOLUME_IRREGULAR
			sampler2D _VLBackBufferTexture;
		#endif

        float3 _VertexOffset;
		fixed4 _Color1;
		fixed4 _Color2;
		fixed4 _FoamColor;
		float _FoamMaxPos;
		int _FoamRaySteps;
		float _FoamDensity;
		float _FoamBottom;
		float _FoamTurbulence;
		float _LevelPos;
		float3 _FlaskThickness;
		float4 _Size;
		float3 _Center;
		float _Muddy;
		float4 _Turbulence;
		float _DeepAtten;
		fixed4 _SmokeColor;
		float _SmokeAtten;
		int _SmokeRaySteps;
		float _SmokeSpeed;
		float _SmokeHeightAtten;
		int _LiquidRaySteps;
		half3 _GlossinessInt;
		float _FoamWeight;
		float4 _Scale;
		float _UpperLimit;
		float _LowerLimit;
		float3 wsCameraPos;
		fixed3	_EmissionColor;
		float _DoubleSidedBias;
        float _BackDepthBias;

        float4x4 _Rot;
        float _TurbulenceSpeed;


#if defined(LIQUID_VOLUME_SCATTERING)
    float4 _PointLightPosition[6];
    half4 _PointLightColor[6];
    float _PointLightInsideAtten;
#endif



 // Open GL 2.0 / WebGL support
#if SHADER_API_GLES || FORCE_GLES_COMPATIBILITY
	#define WEBGL_OR_LEGACY
#endif

 #if defined(WEBGL_OR_LEGACY)
        sampler2D _Noise2Tex;

        inline float noise3D(float3 x) {
            x *= 100.0;
            float3 f = frac(x);
            float3 p = x - f;
            f = f*f*(3.0-2.0*f);
            float4 xy = float4(p.xy + float2(37.0,17.0)*p.z + f.xy, 0, 0);
            xy.xy = (xy.xy + 0.5) / 256.0;
            float2 zz = tex2Dlod(_Noise2Tex, xy).yx;
            return lerp( zz.x, zz.y, f.z );
        }
        #define SAMPLE_NOISE_3D(tex,pos) noise3D( (pos).xyz)
        #define BEGIN_LOOP(k,iterations,defaultIterations) for(int k=0;k<defaultIterations;k++) if (k<iterations) {
        #define END_LOOP }
#else
        sampler3D _NoiseTex;
        #define SAMPLE_NOISE_3D(tex,pos) tex3Dlod(tex,pos)
        #define BEGIN_LOOP(k,iterations,defaultIterations) for(int k=0;k<iterations;k++) {
        #define END_LOOP }
#endif

#if defined(LIQUID_VOLUME_MONOSCOPIC)
	#define _WorldSpaceCameraPos _CustomWorldSpaceCameraPos
#endif

	float3 _CustomWorldSpaceCameraPos;

		half4 LightingWrappedSpecular (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
    	    half3 h = normalize (lightDir + viewDir);
			half NdotL = dot (s.Normal, lightDir);
        	half diff = saturate (NdotL * 0.5 + 0.5);
	        float nh = saturate (dot (s.Normal, h));
    	    float spec = pow (nh, _GlossinessInt.x);
	        half4 c;

       	    // apply light scattering
       	    #if defined(LIQUID_VOLUME_SCATTERING)
       	    diff += pow( max( dot( viewDir, -lightDir), 0.0 ), _GlossinessInt.y) * _GlossinessInt.z;
       	    #endif

    	    c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * atten;
        	c.a = s.Alpha;
        	return c;
    	}

		half4 LightingSimple (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
	        half4 c;
    	    c.rgb = s.Albedo * _LightColor0.rgb * atten;
        	c.a = s.Alpha;
        	return c;
    	}
		
		void intSphere(float3 rd, out float t0, out float t1) {
			float3  d = wsCameraPos - _Center;
		    float   b = dot(rd, d);
    		float   c = dot(d,d) - _Size.w * _Size.w;
    		float   t = sqrt(b*b - c);
	        t0 = -b-t;
			t1 = -b+t;
	    }
	    
	    void intCylinder(float3 rd, out float t0, out float t1) {
			#if LIQUID_VOLUME_NON_AABB
   			rd = mul((float3x3)_Rot, rd);
   			#endif
			float3  d = wsCameraPos - _Center;
	    	#if LIQUID_VOLUME_NON_AABB
			d = mul((float3x3)_Rot, d);
   			#endif
			float   a = dot(rd.xz, rd.xz);
			float   b = dot(rd.xz, d.xz);
			float   c = dot(d.xz,d.xz) - _Size.w * _Size.w;
			float   t = sqrt(max(b*b-a*c,0)); // max prevents artifacts with MSAA
	        t0 = (-b-t)/a;
			t1 = (-b+t)/a;
			
			// cylinder cap
			float sy = _Size.y * 0.5 * _FlaskThickness.y;
			float h = abs(d.y) - sy;
			if (h>0) {
				float rdl = dot(rd.xz / rd.y, rd.xz / rd.y);
				float tc = h * sqrt (1.0 + rdl);
				t0 = max(t0, tc);
			}
			
			h = sign(rd.y) * -d.y + sy;
			if (h>0) {
				float rdl = dot(rd.xz / rd.y, rd.xz / rd.y);
				float tc = h * sqrt (1.0 + rdl);
				t1 = min(t1, tc);
			}
		}
		
		void intBox(float3 rd, out float t0, out float t1) {
	    	#if LIQUID_VOLUME_NON_AABB
			rd = mul((float3x3)_Rot, rd);
			#endif
			float3 ro = wsCameraPos - _Center;
	    	#if LIQUID_VOLUME_NON_AABB
			ro = mul((float3x3)_Rot, ro);
			#endif

		    float3 invR   = 1.0 / rd;
		    float3 boxmin = - _Size.w;
		    float3 boxmax = + _Size.w;
    		float3 tbot   = invR * (boxmin - ro);
    		float3 ttop   = invR * (boxmax - ro);
			float3 tmin   = min (ttop, tbot);
			float3 tmax   = max (ttop, tbot);
			float2 tt0    = max (tmin.xx, tmin.yz);
			t0  = max(tt0.x, tt0.y);
			tt0 = min (tmax.xx, tmax.yz);
			t1  = min(tt0.x, tt0.y);	
		}
  	    
  	    
		void vert(inout appdata_base v, out Input o) {
		 	UNITY_INITIALIZE_OUTPUT(Input,o);
			o.vertex = v.vertex;
			o.vertex.w = dot(o.vertex.xz, _Turbulence.zw) + _TurbulenceSpeed;
			o.vertex.xz *= 0.1.xx * _Turbulence.xx;	// extracted from frag
			o.vertex.xz += _Time.xx;
			v.vertex.xyz *= _FlaskThickness;
			o.realPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
	        #if LIQUID_VOLUME_IGNORE_GRAVITY
   				o.realPos = mul((float3x3)_Rot, o.realPos - _Center) + _Center;
				o.camPos = mul((float3x3)_Rot, _WorldSpaceCameraPos - _Center) + _Center;
			#else
				o.camPos = _WorldSpaceCameraPos;
   			#endif
		}
		
		half4 raymarch(float4 vertex, float3 rd, float t0, float t1); // forward declaration


		#if defined(LIQUID_VOLUME_FP_RENDER_TEXTURES)
			#define texDistance(tex, uv) (tex2D(tex, uv)).r
		#else
			inline float DecodeDistance(sampler2D tex, float2 uv) {
				float distRGBA = tex2D(tex, uv);
				return (1.0 / DecodeFloatRGBA(distRGBA)) - 1.0;
			}
			#define texDistance(tex, uv) DecodeDistance(tex, uv)
		#endif


	 	float minimum_distance_sqr(float rayLengthSqr, float3 w, float3 p) {
     		// Return minimum distance between line segment vw and point p
     		float t = saturate(dot(p, w) / rayLengthSqr);
     		float3 projection = t * w;
     		return dot(p - projection, p - projection);
 		}

		inline float3 ProjectOnPlane(float3 v, float3 planeNormal) {
			float sqrMag = dot(planeNormal, planeNormal);
			float dt = dot(v, planeNormal);
			return v - planeNormal * dt / sqrMag;
		}
		
		void surf (Input IN, inout SurfaceOutput o) {

	        if (IN.vertex.y > _UpperLimit || IN.vertex.y < _LowerLimit) return;

			wsCameraPos = IN.camPos; // _WorldSpaceCameraPos is wrong in XR in fragment shader of a surface shader due to a Unity bug (we pass it from the vertex shader where the stereo eye index is set correctly)

			#if defined(LIQUID_VOLUME_ORTHO)
				float3 cameraForward = UNITY_MATRIX_V[2].xyz;
				float3 orthoCameraPos = ProjectOnPlane(IN.realPos - wsCameraPos, cameraForward) + wsCameraPos;
				wsCameraPos = lerp(wsCameraPos, orthoCameraPos, unity_OrthoParams.w);
			#endif

			float t0, t1;
			float3 rd = IN.realPos - wsCameraPos;
			float dist = length(rd);
			rd /= dist;
			#if LIQUID_VOLUME_SPHERE
				intSphere(rd, t0, t1);
			#elif LIQUID_VOLUME_CUBE
				intBox(rd, t0, t1);			
			#elif LIQUID_VOLUME_CYLINDER
				intCylinder(rd, t0, t1);
			#else
				t0 = dist;
				t1 = dist + _Size.x + _BackDepthBias;
			#endif
			
			t0 = max(0,t0);	// needed if camera is inside container

			#if LIQUID_VOLUME_DEPTH_AWARE || LIQUID_VOLUME_DEPTH_AWARE_PASS || LIQUID_VOLUME_IRREGULAR
				float2 uv = IN.screenPos.xy / IN.screenPos.w;
			#endif

			#if LIQUID_VOLUME_DEPTH_AWARE
				#if defined(LIQUID_VOLUME_ORTHO)
					float  vz = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
					#if UNITY_REVERSED_Z
						vz = 1.0 - vz;
					#endif
					float z = vz * _ProjectionParams.z;
				#else
					float  vz = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
					float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
					float2 suv = uv;
					#if UNITY_SINGLE_PASS_STEREO
						// If Single-Pass Stereo mode is active, transform the
						// coordinates to get the correct output UV for the current eye.
					float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
						suv = (suv - scaleOffset.zw) / scaleOffset.xy;
					#endif
		    		float3 vpos = float3((suv * 2 - 1) / p11_22, -1) * vz;
			    	float4 wpos = mul(unity_CameraToWorld, float4(vpos, 1));
					float z = distance(wsCameraPos, wpos.xyz) + _DepthAwareOffset;
				#endif
				t1 = min(t1, z);
			#endif

			#if LIQUID_VOLUME_DEPTH_AWARE_PASS
			float frontDist = texDistance(_VLFrontBufferTexture, uv);
				t1 = min(t1, frontDist);
			#endif
			
			#if LIQUID_VOLUME_IRREGULAR
			float backDist = texDistance(_VLBackBufferTexture, uv);
				t1 = min(t1, backDist);
				clip(t1-t0-_DoubleSidedBias);
			#endif

			half4 co = raymarch(IN.vertex,rd,t0,t1);

			#if defined(LIQUID_VOLUME_SCATTERING)
				float3 rayStart = wsCameraPos + rd * t0;
				float rayLength = t1 - t0;
         		rayStart += rd * _PointLightInsideAtten;
         		rayLength -= _PointLightInsideAtten;
         		rd *= rayLength;
         		float rayLengthSqr = rayLength * rayLength;
         		for (int k=0;k<6;k++) {
             		half pointLightInfluence = minimum_distance_sqr(rayLengthSqr, rd, _PointLightPosition[k] - rayStart) / _PointLightColor[k].w;
             		co.rgb += _PointLightColor[k].rgb * (co.a / (1.0 + pointLightInfluence));
         		}
         	#endif


			o.Albedo = co.rgb;
			o.Emission = co.rgb * _EmissionColor;
			o.Alpha = co.a;
		}
