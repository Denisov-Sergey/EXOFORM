Shader "Custom/URP_VoxelVertexColor"
{
    Properties
    {
        _Smoothness("Smoothness", Range(0, 1)) = 0
        _Metallic("Metallic", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float4 color : COLOR;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float4 color : COLOR;
        };

        CBUFFER_START(UnityPerMaterial)
            half _Smoothness;
            half _Metallic;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Освещение (базовый расчет)
                half3 normal = half3(0, 1, 0); // Нормаль по умолчанию
                Light light = GetMainLight();
                half3 lighting = LightingLambert(light.color, light.direction, normal);
                
                // Итоговый цвет
                half3 albedo = IN.color.rgb * lighting;
                return half4(albedo, 1);
            }
            ENDHLSL
        }
    }
}