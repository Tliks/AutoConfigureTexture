Shader "Hidden/ACT/IslandSelectMask"
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
            sampler2D _IdTex;
            float _IslandId;
            struct appdata { float3 vertex:POSITION; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o; o.pos=float4(v.vertex.xy*2-1,0,1); o.uv=v.vertex.xy; return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float4 c = tex2D(_IdTex, i.uv);
                uint id = (uint)round(c.r*255.0) | ((uint)round(c.g*255.0) << 8) | ((uint)round(c.b*255.0) << 16);
                return (id == (uint)_IslandId) ? 1.0.xxxx : 0.0.xxxx;
            }
            ENDHLSL
        }
    }
}

