Shader "Hidden/CopyRedChannel"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float r = tex2D(_MainTex, i.uv).r;
                return float4(r, 0, 0, 1);
            }
            ENDCG
        }
    }
}
