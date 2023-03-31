Shader "Warren's Fast Fur/Internal Utilities/Stencil"
{
    Properties
    {
        //[HideInInspector]
        _MainTex("Fur Depth and Combing Map", 2D) = "white" {}
		_FurShapeMap("Original Fur Depth and Combing Map", 2D) = "grey" {}
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque" }

            Cull Off
            ZTest Off

            Pass
            {
                CGPROGRAM

                #include "UnityCG.cginc"
                #include "FastFur-Functions.cginc"

                #pragma vertex vert
                #pragma fragment frag

                sampler2D _MainTex;
                sampler2D _FurShapeMap;

                struct meshData
                {
                    float4 vertex : POSITION;
	                float2 uv : TEXCOORD0;
                };

                struct v2f {
                    centroid float4 pos : SV_POSITION;
                    centroid float2 uv : TEXCOORD0;
                };

                v2f vert(meshData v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float4 col = tex2D(_MainTex, i.uv);
                    if(col.a > 0) return(col);

                    return(tex2D(_FurShapeMap, i.uv));
                }

                ENDCG
            }
        }
}
