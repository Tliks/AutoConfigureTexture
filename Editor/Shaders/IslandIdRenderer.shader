Shader "Hidden/ACT/IslandIdRenderer"
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
            #include "UnityCG.cginc"
            struct appdata
            {
                float3 vertex : POSITION;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
            };
            float _IslandId;
            v2f vert(appdata v)
            {
                v2f o;
                // VP=Ortho(0..1) を CommandBuffer 側でセットしている前提。
                // ここではそのままVPを適用するだけ。
                o.pos = mul(UNITY_MATRIX_VP, float4(v.vertex.xy, 0, 1));
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                // RFloat ターゲットに ID をそのまま書き込む（ガンマ非依存）
                float id = round(_IslandId);
                return float4(id, 0.0, 0.0, 1.0);
            }
            ENDHLSL
        }
    }
}



