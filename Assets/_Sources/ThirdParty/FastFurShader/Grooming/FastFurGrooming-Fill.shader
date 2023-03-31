Shader "Warren's Fast Fur/Internal Utilities/Fill"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Threshold("Threshold", float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Cull Off
        ZTest Off


        // The purpose of this shader is to do a 2D surface fill, rather than painting with a 3D 
        // cursor. However, I don't know how to deal with hard-edges, and so this shader doesn't
        // recognize them as being connected.


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
                centroid float4 vertex : SV_POSITION;
                centroid float2 uv : TEXCOORD0;
            };

            float4 _MainTex_TexelSize;
            float _Threshold;
            float _FurGroomBrushRadius;

            v2f vert(meshData v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            SamplerState my_point_repeat_sampler;
            UNITY_DECLARE_TEX2D(_MainTex);

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                float4 originalCol = _MainTex.SampleLevel(my_point_repeat_sampler, i.uv, 0);
                float dist = abs(originalCol.a);
                if(dist < _Threshold) return (originalCol);
                if(dist > _FurGroomBrushRadius) return float4(0.5,0,0,1000000);
                float2 loc = i.uv;
                for(int z = 0 ; z < 64 ; z++)// 32 is probably as much as we need, but I'm doubling it in case someone is using a high-res texture
                {
                    float closest = 900000;
                    int closestX = 0;
                    int closestY = 0;

                    for(int x = -3 ; x < 4 ; x++)
                    {
                        for(int y = -3 ; y < 4 ; y++)
                        {
                            float tex = abs(_MainTex.SampleLevel(my_point_repeat_sampler, loc + float2(_MainTex_TexelSize.x * x, _MainTex_TexelSize.y * y), 0).a);
                           
                            if(tex > 0) // Distances of 0 are actually non-rendered pixels, so we need to ignore them
                            {
                                if(tex <= _Threshold) return (originalCol);
                                if(tex < closest)
                                {
                                    closest = tex;
                                    closestX = x;
                                    closestY = y;
                                }
                            }
                        }
                    }

                    if(closestX == 0 && closestY == 0) return float4(0,1,1,1000000);

                    loc = loc + float2(_MainTex_TexelSize.x * closestX, _MainTex_TexelSize.y * closestY);
                }

                return float4(1,1,0,1000000);
            }
            ENDCG
        }
    }
}
