Shader "Hidden/ACT/IslandIdRenderer"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _IslandId;

            float4 vert(float4 vertex : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }
            float4 frag(float4 vertex : SV_POSITION) : SV_TARGET
            {
                return float4(round(_IslandId), 0, 0, 1);
            }
            ENDCG
        }
    }
}
