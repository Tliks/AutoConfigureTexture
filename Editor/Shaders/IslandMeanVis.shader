Shader "Hidden/ACT/IslandMeanVis"
{
    Properties {}
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _IdTex; // RFloat。sampler2D でも .r に値が来る
            sampler2D _MeanTex; // width = NumIslands, height=1, R channel = mean
            int _NumIslands;
            float _UseColor; // 1: heatmap, 0: grayscale

            float3 heat(float t)
            {
                t = saturate(t);
                float3 c1 = float3(0.0, 0.0, 1.0);
                float3 c2 = float3(0.0, 1.0, 1.0);
                float3 c3 = float3(1.0, 1.0, 0.0);
                float3 c4 = float3(1.0, 0.0, 0.0);
                if (t < 0.33) return lerp(c1, c2, t / 0.33);
                if (t < 0.66) return lerp(c2, c3, (t - 0.33) / 0.33);
                return lerp(c3, c4, (t - 0.66) / 0.34);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float idVal = tex2D(_IdTex, uv).r;
                uint id = (uint)round(idVal);
                if (id == 0u) return float4(0,0,0,1);
                int idx = (int)id - 1;
                if (idx < 0 || idx >= _NumIslands) return float4(0,0,0,1);
                float u = (idx + 0.5) / max(1,_NumIslands);
                float mean = tex2D(_MeanTex, float2(u, 0.5)).r; // 0..1
                if (_UseColor > 0.5)
                {
                    float3 col = heat(mean);
                    return float4(col, 1);
                }
                return float4(mean, mean, mean, 1);
            }
            ENDCG
        }
    }
}


