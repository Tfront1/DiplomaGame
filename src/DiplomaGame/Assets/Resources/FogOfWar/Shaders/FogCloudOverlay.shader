Shader "Custom/FogCloudOverlay"
{
    Properties
    {
        _FogTex ("Fog Texture", 2D) = "white" {}
        _MainTex ("Noise Texture", 2D) = "white" {}
        _TimeScale ("Time Scale", Float) = 0.5
        _LayerSpeed ("Layer Speed", Vector) = (0.1, 0.1, 0, 0)
        _LayerScale ("Layer Scale", Float) = 1
        _Alpha ("Alpha", Float) = 0.5
        _FogColor ("Fog Color", Color) = (0.5, 0.5, 0.5, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _FogTex;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _TimeScale;
            float4 _LayerSpeed;
            float _LayerScale;
            float _Alpha;
            float4 _FogColor;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float time = _Time.y * _TimeScale;
                float2 uv = i.uv * _LayerScale + time * _LayerSpeed.xy;
                
                float noise = tex2D(_MainTex, uv).r;
                float fog = tex2D(_FogTex, i.uv).r;

                if( fog < 0.1)
                {
                    return float4(0, 0, 0, 0);
                }

                if(noise < 0.5)
                {
                    return float4(0, 0, 0, 0);
                }

                float combined = fog * noise;

                float inverted = smoothstep(0.2, 0.4, combined);
                return float4(_FogColor.rgb, inverted * _FogColor.a * _Alpha);
            }
            ENDCG
        }
    }
}
