Shader "Universal Render Pipeline/WhiteFlashSprite"
{
    Properties
    {
        // Per-renderer sprite texture (SpriteRenderer supplies it)
        [MainTexture] [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        // Per-renderer tint (SpriteRenderer.color)
        [PerRendererData] _BaseColor ("Tint", Color) = (1,1,1,1)
        // Driven from C#
        _FlashAmount ("Flash Amount", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float  _FlashAmount;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                half3 baseRGB = tex.rgb * _BaseColor.rgb * IN.color.rgb;
                half  alpha   = tex.a  * _BaseColor.a  * IN.color.a;

                baseRGB = lerp(baseRGB, half3(1,1,1), saturate(_FlashAmount));

                return half4(baseRGB, alpha);
            }
            ENDHLSL
        }
    }
}
