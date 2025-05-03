Shader "Custom/FogOfWarMaterial"
{
    Properties
    {
        _FogTex ("Fog Texture", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+1"
            "RenderType" = "Transparent" 
            "IgnoreProjector" = "True"
        }
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
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
            
            sampler2D _FogTex;
            fixed4 _FogColor;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float fogValue = tex2D(_FogTex, i.uv).r;

                if (fogValue >= 0.7)
                {
                    return _FogColor;
                }
                else
                {
                    float alpha = fogValue;
                    return fixed4(_FogColor.rgb, alpha);
                }
            }
            ENDCG
        }
    }
}