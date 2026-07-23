Shader "RagePlatformer/GeneratedSprite"
{
    Properties
    {
        [MainTexture] _ArtTex("Generated Art", 2D) = "white" {}
        [MainColor] _Color("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "GeneratedSpriteUnlit"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_ArtTex);
            SAMPLER(sampler_ArtTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _ArtTex_ST;
                half4 _Color;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS.xyz = UnityFlipSprite(
                    input.positionOS.xyz,
                    unity_SpriteProps.xy
                );

                Varyings output;
                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);
                output.uv =
                    input.uv * _ArtTex_ST.xy + _ArtTex_ST.zw;
                output.color =
                    input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(
                    _ArtTex,
                    sampler_ArtTex,
                    input.uv
                ) * input.color;

                clip(color.a - 0.001h);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
