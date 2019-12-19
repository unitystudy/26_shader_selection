Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5// カットオフ閾値

        _MainTex("Texture", 2D) = "white" {}// 模様のテクスチャ
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)// 乗算色

        // 実際に設定する変数。[HideInInspector]をつけて直接は触らないようにする
        [HideInInspector] _BlendMode("__mode", Float) = 0.0// シェーダでは使わないが、C#での状態管理に使う
        [HideInInspector] _SrcBlend("src", Float) = 1.0
        [HideInInspector] _DstBlend("dst", Float) = 0.0
        [HideInInspector] _ZWrite("zwrite", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile __ _ALPHATEST_ON

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _Cutoff;
            half4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _Color * tex2D(_MainTex, i.uv).xxxx;

#ifdef _ALPHATEST_ON
                clip(col.a - _Cutoff);
#endif // _ALPHATEST_ON

                return col;
            }
            ENDCG
        }
    }
    CustomEditor "CustomInspector"
}
