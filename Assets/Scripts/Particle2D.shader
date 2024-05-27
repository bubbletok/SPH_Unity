Shader "Custom/Particle2D"
{
    Properties {
        _EmissionColor ("Emission Color", Color) = (1,1,1,0.5)
        _EmissionStrength ("Emission Strength", Range(0, 10)) = 1.0
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"
            StructuredBuffer<float2> Positions2D;
            StructuredBuffer<float2> Velocities;
            StructuredBuffer<float2> Densities;
            StructuredBuffer<float2> PressureForces;
            float scale;
            float4 colA;
            Texture2D ColourMap;
            SamplerState linear_clamp_sampler;
            float velocityMax;

            float4 _EmissionColor;
            float _EmissionStrength;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 colour : TEXCOORD1;
            };

            float random (float2 uv)
            {
                return frac(sin(dot(uv,float2(12.9898,78.233)))*43758.5453123);
            }

            // Set Color based on Velocity
            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
                float speed = length(Velocities[instanceID]);
                float speedT = saturate(speed / velocityMax);
                float colT = speedT;

                float3 centreWorld = float3(Positions2D[instanceID], 0);
                float3 worldVertPos = centreWorld + mul(unity_ObjectToWorld, v.vertex * scale);
                float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));

                v2f o;
                o.uv = v.texcoord;
                o.pos = UnityObjectToClipPos(objectVertPos);
                o.colour = ColourMap.SampleLevel(linear_clamp_sampler, float2(colT, 0.5), 0);

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 centreOffset = (i.uv.xy - 0.5) * 2;
                float sqrDst = dot(centreOffset, centreOffset);
                float delta = fwidth(sqrt(sqrDst));
                float alpha = 1 - smoothstep(1 - delta, 1 + delta, sqrDst);

                float3 colour = i.colour;

                /*// Calculate outline emission
                float outline = smoothstep(1 - 0.1, 1, sqrt(sqrDst));
                float3 emission = _EmissionColor.rgb * _EmissionStrength * outline;

                return float4(colour + emission, alpha);*/
                return float4(colour, alpha);
            }

            ENDCG
        }
    }
}
