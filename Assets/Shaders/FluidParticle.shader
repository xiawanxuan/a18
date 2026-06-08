Shader "Fluid/FluidParticle"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (0.2, 0.5, 1.0, 0.6)
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 0.5
        _SoftFade ("Soft Fade", Range(0, 1)) = 0.3
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
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha, One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _GlowIntensity;
            float _SoftFade;

            #ifdef UNITY_INSTANCING_ENABLED
                UNITY_INSTANCING_BUFFER_START(Props)
                    UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
                UNITY_INSTANCING_BUFFER_END(Props)
            #endif

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                #ifdef UNITY_INSTANCING_ENABLED
                    float4 instColor = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                    o.color = instColor * _Color;
                #else
                    o.color = _Color;
                #endif

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                float2 center = i.texcoord - float2(0.5, 0.5);
                float dist = length(center) * 2.0;
                float edgeFade = 1.0 - smoothstep(0.7, 1.0, dist);

                float alpha = col.a * edgeFade;

                float3 glow = col.rgb * _GlowIntensity * col.a;

                return fixed4(col.rgb + glow, alpha);
            }
            ENDCG
        }
    }

    Fallback "Particles/Alpha Blended Premultiply"
}
