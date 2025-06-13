Shader "Custom/HeightTextures"
{
    Properties
    {
        // === СЕКЦИЯ ТЕКСТУР ДЛЯ РАЗНЫХ ВЫСОТНЫХ ЗОН ===
        
        [Header(Valley Low Areas)]
        _ValleyTexture ("Valley Texture", 2D) = "white" {}        // Текстура для низких областей (долины, овраги)
        _ValleyHeight ("Valley Height Threshold", Range(0,1)) = 0.25  // Пороговая высота для долин (0-0.25)
        
        [Header(Plain Medium Areas)]
        _PlainTexture ("Plain Texture", 2D) = "white" {}         // Текстура для средних областей (равнины, поля)
        _PlainHeight ("Plain Height Threshold", Range(0,1)) = 0.5    // Пороговая высота для равнин (0.25-0.5)
        
        [Header(Hill High Areas)]
        _HillTexture ("Hill Texture", 2D) = "white" {}           // Текстура для возвышенностей (холмы)
        _HillHeight ("Hill Height Threshold", Range(0,1)) = 0.75     // Пороговая высота для холмов (0.5-0.75)
        
        [Header(Peak Highest Areas)]
        _PeakTexture ("Peak Texture", 2D) = "white" {}           // Текстура для самых высоких областей (пики, вершины)
        
        // === НАСТРОЙКИ РЕНДЕРИНГА ===
        
        [Header(Settings)]
        _TextureScale ("Texture Scale", Float) = 1               // Масштаб текстур (больше = меньше повторений)
        _BlendSmoothness ("Blend Smoothness", Range(0,0.3)) = 0.1 // Плавность перехода между текстурами
    }
    
    SubShader
    {
        // Настройки для Universal Render Pipeline
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert     // Указываем функцию вершинного шейдера
            #pragma fragment frag   // Указываем функцию фрагментного шейдера
            
            // Подключаем библиотеки URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // === СТРУКТУРЫ ДАННЫХ ===
            
            // Входные данные от меша (приходят из C# кода генерации террейна)
            struct Attributes
            {
                float4 positionOS : POSITION;  // Позиция вершины в object space
                float3 normalOS : NORMAL;      // Нормаль вершины для освещения
                float2 uv : TEXCOORD0;         // UV-координаты (не используются в triplanar)
                float4 color : COLOR;          // ВАЖНО: высота террейна в красном канале (0-1)
            };
            
            // Данные, передаваемые из vertex в fragment шейдер
            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // Позиция для растеризации
                float3 positionWS : TEXCOORD0;    // Мировая позиция для triplanar mapping
                float3 normalWS : TEXCOORD1;      // Мировая нормаль для освещения
                float2 uv : TEXCOORD2;            // UV (резерв)
                float height : TEXCOORD3;         // Нормализованная высота из vertex color
            };
            
            // === ОБЪЯВЛЕНИЕ ТЕКСТУР И СЭМПЛЕРОВ ===
            
            TEXTURE2D(_ValleyTexture); SAMPLER(sampler_ValleyTexture);  // Текстура долин
            TEXTURE2D(_PlainTexture); SAMPLER(sampler_PlainTexture);    // Текстура равнин
            TEXTURE2D(_HillTexture); SAMPLER(sampler_HillTexture);      // Текстура холмов
            TEXTURE2D(_PeakTexture); SAMPLER(sampler_PeakTexture);      // Текстура пиков
            
            // Константный буфер с параметрами материала
            CBUFFER_START(UnityPerMaterial)
                float _ValleyHeight;      // Пороговые высоты для каждой зоны
                float _PlainHeight;
                float _HillHeight;
                float _TextureScale;      // Масштаб текстур
                float _BlendSmoothness;   // Плавность переходов
            CBUFFER_END
            
            // === TRIPLANAR MAPPING ФУНКЦИЯ ===
            // Проецирует текстуру на объект с трех осей для избежания растягивания
            float4 SampleTriplanar(TEXTURE2D_PARAM(tex, samp), float3 worldPos, float3 normal)
            {
                // Вычисляем веса проекций по трем осям на основе нормали
                float3 blendWeights = abs(normal);           // Абсолютные значения нормали
                blendWeights = pow(blendWeights, 4);         // Усиливаем контраст
                blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z); // Нормализуем
                
                // UV-координаты для каждой проекции
                float2 uvX = worldPos.zy * _TextureScale;    // Проекция на плоскость YZ (боковая)
                float2 uvY = worldPos.xz * _TextureScale;    // Проекция на плоскость XZ (верхняя/нижняя)
                float2 uvZ = worldPos.xy * _TextureScale;    // Проекция на плоскость XY (фронтальная)
                
                // Сэмплируем текстуру для каждой проекции
                float4 texX = SAMPLE_TEXTURE2D(tex, samp, uvX);
                float4 texY = SAMPLE_TEXTURE2D(tex, samp, uvY);
                float4 texZ = SAMPLE_TEXTURE2D(tex, samp, uvZ);
                
                // Смешиваем результаты с учетом весов
                return texX * blendWeights.x + texY * blendWeights.y + texZ * blendWeights.z;
            }
            
            // === ВЕРШИННЫЙ ШЕЙДЕР ===
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Получаем позиции и нормали через URP helper функции
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                
                // Заполняем выходную структуру
                output.positionHCS = posInputs.positionCS;  // Позиция для растеризации
                output.positionWS = posInputs.positionWS;   // Мировая позиция для triplanar
                output.normalWS = normInputs.normalWS;      // Мировая нормаль для освещения
                output.uv = input.uv;                       // UV (не используется)
                output.height = input.color.r;              // КЛЮЧЕВОЕ: извлекаем высоту из красного канала
                
                return output;
            }
            
            // === ФРАГМЕНТНЫЙ ШЕЙДЕР ===
            half4 frag(Varyings input) : SV_Target
            {
                // === РАСЧЕТ ВЕСОВ ДЛЯ СМЕШИВАНИЯ ТЕКСТУР ===
                
                // Вес долин: максимален в низких областях, убывает к _ValleyHeight
                float valleyWeight = 1.0 - smoothstep(_ValleyHeight - _BlendSmoothness, 
                                                     _ValleyHeight + _BlendSmoothness, 
                                                     input.height);
                
                // Вес равнин: активен между долинами и равнинной высотой
                float plainWeight = (1.0 - smoothstep(_PlainHeight - _BlendSmoothness, 
                                                     _PlainHeight + _BlendSmoothness, 
                                                     input.height)) * 
                                   smoothstep(_ValleyHeight - _BlendSmoothness, 
                                            _ValleyHeight + _BlendSmoothness, 
                                            input.height);
                
                // Вес холмов: активен между равнинами и холмистой высотой
                float hillWeight = (1.0 - smoothstep(_HillHeight - _BlendSmoothness, 
                                                    _HillHeight + _BlendSmoothness, 
                                                    input.height)) * 
                                  smoothstep(_PlainHeight - _BlendSmoothness, 
                                           _PlainHeight + _BlendSmoothness, 
                                           input.height);
                                  
                // Вес пиков: максимален в самых высоких областях
                float peakWeight = smoothstep(_HillHeight - _BlendSmoothness, 
                                            _HillHeight + _BlendSmoothness, 
                                            input.height);
                
                // === НОРМАЛИЗАЦИЯ ВЕСОВ ===
                // Гарантируем, что сумма весов = 1.0 для корректного смешивания
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
                Light mainLight = GetMainLight();                              // Получаем основной источник света
                float NdotL = saturate(dot(input.normalWS, mainLight.direction)); // Скалярное произведение нормали и света
                half3 lighting = mainLight.color * NdotL + unity_AmbientSky.rgb;   // Диффузное + окружающее освещение
                
                // Возвращаем финальный цвет с применением освещения
                return half4(finalColor.rgb * lighting, 1);
            }
            ENDHLSL
        }
    }
}
