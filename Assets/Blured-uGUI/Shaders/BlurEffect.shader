Shader "uGUI/BlurEffect"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            ZTest Off
            ZWrite Off
            Cull Back

            CGPROGRAM
            #pragma fragmentoption ARB_precision_hint_fastest
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
                float4 vertex : SV_POSITION;
                half2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;

            half4 _Offsets;
            float _Intencity;

            static const int samplingCount = 10;
            half _Weights[samplingCount];
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                half4 col = 0;

                [unroll]
                for (int j = samplingCount - 1; j > 0; j--)
                {
                    col += tex2D(_MainTex, i.uv - (_Offsets.xy * j * _Intencity)) * _Weights[j];
                }

                [unroll]
                for (int j = 0; j < samplingCount; j++)
                {
                    col += tex2D(_MainTex, i.uv + (_Offsets.xy * j * _Intencity)) * _Weights[j];
                }

                return col;
            }
            ENDCG
        }
    }
}
