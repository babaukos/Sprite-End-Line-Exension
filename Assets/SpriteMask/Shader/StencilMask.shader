Shader "Hidden/Sprite/StencilMask"
{
    Properties
    {
        _MainTex ("Alpha Mask (Texture)", 2D) = "white" {}
        _StencilRef ("Stencil ID", Float) = 1
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Geometry-1" "RenderType"="Transparent" }
        ColorMask 0
        ZWrite Off

        Pass
        {
            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Cutoff;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a - _Cutoff); // Прорізаємо дірку по формі спрайту
                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}