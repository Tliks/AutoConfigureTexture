Shader "Hidden/ACT/IslandMaskRenderer"
{
    Properties {}
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct appdata
            {
                float3 vertex : POSITION; // expects UV-space in [0,1] mapped to XY
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
            };
            v2f vert(appdata v)
            {
                v2f o;
                // map UV [0,1] to clip space [-1,1]
                float2 uv = v.vertex.xy;
                float2 clip = uv * 2.0 - 1.0;
                o.pos = float4(clip, 0, 1);
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                // write full white to R8/RGBA target; downstream uses red channel
                return 1.0.xxxx;
            }
            ENDHLSL
        }
    }
}


