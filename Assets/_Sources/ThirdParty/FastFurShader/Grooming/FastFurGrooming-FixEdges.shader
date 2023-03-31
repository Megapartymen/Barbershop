Shader "Warren's Fast Fur/Internal Utilities/Fix Edges"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Cull Off
        ZTest Off

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct meshData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                centroid float4 pos : SV_POSITION;
                centroid float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            v2f vert(meshData v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                float4 col = tex2Dlod(_MainTex, float4(i.uv, 0, 0));
                if (length(col) == 0) {
                    float4 texR = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, 0));
                    float4 texL = tex2D(_MainTex, i.uv - float2(_MainTex_TexelSize.x, 0));
                    float4 texU = tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y));
                    float4 texD = tex2D(_MainTex, i.uv - float2(0, _MainTex_TexelSize.y));
                    texR *= (texR.a > 0 ? 1 : 0);
                    texL *= (texL.a > 0 ? 1 : 0);
                    texU *= (texU.a > 0 ? 1 : 0);
                    texD *= (texD.a > 0 ? 1 : 0);
                    float valid = (texR.a > 0 ? 1 : 0) + (texL.a > 0 ? 1 : 0) + (texU.a > 0 ? 1 : 0) + (texD.a > 0 ? 1 : 0);
                    if (valid > 0) {
                        col = (texR + texL + texU + texD) / valid;
                    }
                }
                return col;
            }
            ENDCG
        }
    }
}
