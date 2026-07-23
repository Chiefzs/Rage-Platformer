Shader "RagePlatformer/SpriteFragment"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _FragmentLocalRect(
            "Fragment Local Rect",
            Vector
        ) = (-1000, -1000, 1000, 1000)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteFragmentUnlit"
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
                float2 localPosition : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _FragmentLocalRect;
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
                output.uv = input.uv;
                output.color =
                    input.color * _Color * unity_SpriteColor;
                output.localPosition = input.positionOS.xy;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                clip(
                    input.localPosition.x -
                    _FragmentLocalRect.x
                );
                clip(
                    input.localPosition.y -
                    _FragmentLocalRect.y
                );
                clip(
                    _FragmentLocalRect.z -
                    input.localPosition.x
                );
                clip(
                    _FragmentLocalRect.w -
                    input.localPosition.y
                );

                half4 color = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
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
