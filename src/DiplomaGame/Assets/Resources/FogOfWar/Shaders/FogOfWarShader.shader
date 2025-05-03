Shader "Hidden/FogOfWar"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PlayerPos ("Player Position", Vector) = (0.5, 0.5, 0, 0)
        _VisionRadius ("Vision Radius", Float) = 0.1
        _FadeSpeed ("Fade Speed", Float) = 0.05
        _CurrentVisibilityTex ("Current Visibility", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
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
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            float4 _PlayerPos;
            float _VisionRadius;
            
            fixed4 frag (v2f i) : SV_Target
            {
                float dist = distance(i.uv, _PlayerPos.xy);
                
                float visibility = smoothstep(_VisionRadius * 0.8, _VisionRadius, dist);
                
                float existingVisibility = tex2D(_MainTex, i.uv).r;
                
                return saturate(min(existingVisibility, visibility));
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
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
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            float4 _PlayerPos;
            float _VisionRadius;
            
            fixed4 frag (v2f i) : SV_Target
            {
                float dist = distance(i.uv, _PlayerPos.xy);
                
                float visibility = 1.0 - smoothstep(_VisionRadius * 0.8, _VisionRadius, dist);
                
                float existingVisibility = tex2D(_MainTex, i.uv).r;
                
                return saturate(max(existingVisibility, visibility));
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
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
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            sampler2D _CurrentVisibilityTex;
            float _FadeSpeed;

            fixed4 frag (v2f i) : SV_Target
            {
                float currentFog = tex2D(_MainTex, i.uv).r;
                float currentVisibility = tex2D(_CurrentVisibilityTex, i.uv).r;

                float targetFog = currentVisibility > 0.1 ? 0.0 : 1.0;

                float delta = _FadeSpeed * unity_DeltaTime.x;

                if (currentFog < targetFog)
                    currentFog = min(currentFog + delta, targetFog);
                else
                    currentFog = max(currentFog - delta, targetFog);

                return fixed4(currentFog, 0, 0, 1);
            }
            ENDCG
        }    
    }
}