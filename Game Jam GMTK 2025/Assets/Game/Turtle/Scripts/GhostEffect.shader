Shader "Custom/GhostEffect"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GhostColor ("Ghost Color", Color) = (0.5, 0.8, 1, 0.5)
        _FlickerSpeed ("Flicker Speed", Float) = 5.0
        _FlickerIntensity ("Flicker Intensity", Range(0, 1)) = 0.3
        _WaveAmplitude ("Wave Amplitude", Float) = 0.02
        _WaveFrequency ("Wave Frequency", Float) = 10.0
        _WaveSpeed ("Wave Speed", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _GhostColor;
            float _FlickerSpeed;
            float _FlickerIntensity;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;

            v2f vert(appdata_t IN)
            {
                v2f OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // Apply wave distortion
                float wave = sin(IN.vertex.y * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveAmplitude;
                IN.vertex.x += wave;

                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _GhostColor;
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            sampler2D _MainTex;
            sampler2D _AlphaTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;

                // Apply flicker effect
                float flicker = sin(_Time.y * _FlickerSpeed) * _FlickerIntensity + (1.0 - _FlickerIntensity);
                c.a *= flicker;

                // Apply ghost color tinting
                c.rgb = lerp(c.rgb, _GhostColor.rgb, 0.3);

                // Ensure alpha blending
                c.rgb *= c.a;

                return c;
            }
            ENDCG
        }
    }
}


