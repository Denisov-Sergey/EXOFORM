Shader "Custom/HeightTextures"
{
    Properties
    {
        // === СЕКЦИЯ ТЕКСТУР ДЛЯ РАЗНЫХ ВЫСОТНЫХ ЗОН ===
        
        [Header(Valley Low Areas)]
        _ValleyTexture ("Valley Texture", 2D) = "white" {}
        _ValleyHeight ("Valley Height Threshold", Range(0,1)) = 0.25
        
        [Header(Plain Medium Areas)]
        _PlainTexture ("Plain Texture", 2D) = "white" {}
        _PlainHeight ("Plain Height Threshold", Range(0,1)) = 0.5
        
        [Header(Hill High Areas)]
        _HillTexture ("Hill Texture", 2D) = "white" {}
        _HillHeight ("Hill Height Threshold", Range(0,1)) = 0.75
        
        [Header(Peak Highest Areas)]
        _PeakTexture ("Peak Texture", 2D) = "white" {}
        
        // === НАСТРОЙКИ РЕНДЕРИНГА ===
        
        [Header(Settings)]
        _TextureScale ("Texture Scale", Float) = 1
        _BlendSmoothness ("Blend Smoothness", Range(0,0.3)) = 0.1
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma target 4.5  // ДОБАВЛЕНО: обязательно для DOTS instancing
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing  // ДОБАВЛЕНО: поддержка standard instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON  // ДОБАВЛЕНО: поддержка DOTS instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // === СТРУКТУРЫ ДАННЫХ ===
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;  // Высота террейна в красном канале
                #if UNITY_ANY_INSTANCING_ENABLED  // ДОБАВЛЕНО: для instancing поддержки
                uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float height : TEXCOORD3;
                #if UNITY_ANY_INSTANCING_ENABLED  // ДОБАВЛЕНО: для instancing поддержки
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
            };
            
            // === ОБЪЯВЛЕНИЕ ТЕКСТУР И СЭМПЛЕРОВ ===
            
            TEXTURE2D(_ValleyTexture); SAMPLER(sampler_ValleyTexture);
            TEXTURE2D(_PlainTexture); SAMPLER(sampler_PlainTexture);
            TEXTURE2D(_HillTexture); SAMPLER(sampler_HillTexture);
            TEXTURE2D(_PeakTexture); SAMPLER(sampler_PeakTexture);
            
            // ИСПРАВЛЕНО: SRP Batcher совместимый CBUFFER
            CBUFFER_START(UnityPerMaterial)
                float _ValleyHeight;
                float _PlainHeight;
                float _HillHeight;
                float _TextureScale;
                float _BlendSmoothness;
            CBUFFER_END
            
            // ДОБАВЛЕНО: DOTS Instanced Properties блок
            #if defined(UNITY_DOTS_INSTANCING_ENABLED)
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float, _ValleyHeight)
                    UNITY_DOTS_INSTANCED_PROP(float, _PlainHeight)
                    UNITY_DOTS_INSTANCED_PROP(float, _HillHeight)
                    UNITY_DOTS_INSTANCED_PROP(float, _TextureScale)
                    UNITY_DOTS_INSTANCED_PROP(float, _BlendSmoothness)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
                
                // ДОБАВЛЕНО: макросы для доступа к DOTS instanced свойствам
                #define _ValleyHeight UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _ValleyHeight)
                #define _PlainHeight UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _PlainHeight)
                #define _HillHeight UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _HillHeight)
                #define _TextureScale UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _TextureScale)
                #define _BlendSmoothness UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _BlendSmoothness)
            #endif
            
            // === TRIPLANAR MAPPING ФУНКЦИЯ ===
            float4 SampleTriplanar(TEXTURE2D_PARAM(tex, samp), float3 worldPos, float3 normal)
            {
                float3 blendWeights = abs(normal);
                blendWeights = pow(blendWeights, 4);
                blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);
                
                float2 uvX = worldPos.zy * _TextureScale;
                float2 uvY = worldPos.xz * _TextureScale;
                float2 uvZ = worldPos.xy * _TextureScale;
                
                float4 texX = SAMPLE_TEXTURE2D(tex, samp, uvX);
                float4 texY = SAMPLE_TEXTURE2D(tex, samp, uvY);
                float4 texZ = SAMPLE_TEXTURE2D(tex, samp, uvZ);
                
                return texX * blendWeights.x + texY * blendWeights.y + texZ * blendWeights.z;
            }
            
            // === ВЕРШИННЫЙ ШЕЙДЕР ===
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;  // ИСПРАВЛЕНО: инициализация
                
                // ДОБАВЛЕНО: instancing setup
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = input.uv;
                output.height = input.color.r;
                
                // ДОБАВЛЕНО: сохранение instance ID
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif
                
                return output;
            }
            
            // === ФРАГМЕНТНЫЙ ШЕЙДЕР ===
            half4 frag(Varyings input) : SV_Target
            {
                // ДОБАВЛЕНО: instancing setup в fragment
                UNITY_SETUP_INSTANCE_ID(input);
                
                // === РАСЧЕТ ВЕСОВ ДЛЯ СМЕШИВАНИЯ ТЕКСТУР ===
                
                float valleyWeight = 1.0 - smoothstep(_ValleyHeight - _BlendSmoothness, 
                                                     _ValleyHeight + _BlendSmoothness, 
                                                     input.height);
                
                float plainWeight = (1.0 - smoothstep(_PlainHeight - _BlendSmoothness, 
                                                     _PlainHeight + _BlendSmoothness, 
                                                     input.height)) * 
                                   smoothstep(_ValleyHeight - _BlendSmoothness, 
                                            _ValleyHeight + _BlendSmoothness, 
                                            input.height);
                
                float hillWeight = (1.0 - smoothstep(_HillHeight - _BlendSmoothness, 
                                                    _HillHeight + _BlendSmoothness, 
                                                    input.height)) * 
                                  smoothstep(_PlainHeight - _BlendSmoothness, 
                                           _PlainHeight + _BlendSmoothness, 
                                           input.height);
                                  
                float peakWeight = smoothstep(_HillHeight - _BlendSmoothness, 
                                            _HillHeight + _BlendSmoothness, 
                                            input.height);
                
                // === НОРМАЛИЗАЦИЯ ВЕСОВ ===
                float totalWeight = valleyWeight + plainWeight + hillWeight + peakWeight;
                if (totalWeight > 0)
                {
                    valleyWeight /= totalWeight;
                    plainWeight /= totalWeight;
                    hillWeight /= totalWeight;
                    peakWeight /= totalWeight;
                }
                
                // === СЭМПЛИРОВАНИЕ ТЕКСТУР С TRIPLANAR MAPPING ===
                half4 valleyColor = SampleTriplanar(TEXTURE2D_ARGS(_ValleyTexture, sampler_ValleyTexture), 
                                                   input.positionWS, input.normalWS);
                half4 plainColor = SampleTriplanar(TEXTURE2D_ARGS(_PlainTexture, sampler_PlainTexture), 
                                                  input.positionWS, input.normalWS);
                half4 hillColor = SampleTriplanar(TEXTURE2D_ARGS(_HillTexture, sampler_HillTexture), 
                                                 input.positionWS, input.normalWS);
                half4 peakColor = SampleTriplanar(TEXTURE2D_ARGS(_PeakTexture, sampler_PeakTexture), 
                                                 input.positionWS, input.normalWS);
                
                // === ФИНАЛЬНОЕ СМЕШИВАНИЕ ЦВЕТОВ ===
                half4 finalColor = valleyColor * valleyWeight + 
                                  plainColor * plainWeight + 
                                  hillColor * hillWeight + 
                                  peakColor * peakWeight;
                
                // === ПРОСТОЕ ОСВЕЩЕНИЕ ===
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 lighting = mainLight.color * NdotL + unity_AmbientSky.rgb;
                
                return half4(finalColor.rgb * lighting, 1);
            }
            ENDHLSL
        }
    }
}
