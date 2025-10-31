Shader "Fullscreen/SobelOperator"
{
    Properties
    {
        _EdgeColor ("Edge Color", Color) = (0, 0, 0, 1)
        _DepthThreshold ("Depth Threshold", Range(0, 1)) = 0.1
        _NormalThreshold ("Normal Threshold", Range(0, 1)) = 0.1
        _DepthSensitivity ("Depth Sensitivity", Range(0, 10)) = 1.0
        _NormalSensitivity ("Normal Sensitivity", Range(0, 10)) = 1.0
        _ThicknessScale ("Thickness Scale", Range(0, 5)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "SobelDepthNormals"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _EdgeColor;
            float4 _BackgroundColor;
            float _DepthThreshold;
            float _NormalThreshold;
            float _DepthSensitivity;
            float _NormalSensitivity;
            float _ThicknessScale;

            // Sobel kernels
            static const float sobelX[9] = {
                -1, 0, 1,
                -2, 0, 2,
                -1, 0, 1
            };

            static const float sobelY[9] = {
                -1, -2, -1,
                 0,  0,  0,
                 1,  2,  1
            };

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.texcoord;
                float2 texelSize = _BlitTexture_TexelSize.xy;
                
                // Get center depth for distance-based scaling
                float centerDepth = SampleSceneDepth(uv);
                float linearDepth = LinearEyeDepth(centerDepth, _ZBufferParams);

                // Sample 3x3 neighborhood for depth
                float depthSamples[9];
                float3 normalSamples[9];
                int index = 0;
                
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(x, y) * texelSize;
                        float2 sampleUV = uv + offset;
                        
                        // Sample depth and normals
                        depthSamples[index] = SampleSceneDepth(sampleUV);
                        normalSamples[index] = SampleSceneNormals(sampleUV);
                        
                        index++;
                    }
                }

                // Apply Sobel operator to depth
                float depthGradX = 0;
                float depthGradY = 0;
                
                for (int i = 0; i < 9; i++)
                {
                    depthGradX += depthSamples[i] * sobelX[i];
                    depthGradY += depthSamples[i] * sobelY[i];
                }
                
                float depthEdge = sqrt(depthGradX * depthGradX + depthGradY * depthGradY);
                
                // Check if using orthographic projection
                // In ortho, unity_OrthoParams.w = 1, in perspective it's 0
                bool isOrtho = unity_OrthoParams.w == 1.0;
                
                if (!isOrtho)
                {
                    // Perspective: Compensate for depth gradient compression at distance
                    // Multiply by depth to boost distant edges
                    float depthCompensation = 1.0 + (linearDepth - 1.0) * _ThicknessScale;
                    depthEdge *= depthCompensation;
                }
                
                depthEdge *= _DepthSensitivity;

                // Apply Sobel operator to normals
                float3 normalGradX = float3(0, 0, 0);
                float3 normalGradY = float3(0, 0, 0);
                
                for (int j = 0; j < 9; j++)
                {
                    normalGradX += normalSamples[j] * sobelX[j];
                    normalGradY += normalSamples[j] * sobelY[j];
                }
                
                float3 normalGrad = sqrt(normalGradX * normalGradX + normalGradY * normalGradY);
                float normalEdge = length(normalGrad) * _NormalSensitivity;

                // Combine depth and normal edges
                float depthStrength = saturate((depthEdge - _DepthThreshold) / (1.0 - _DepthThreshold + 0.001));
                float normalStrength = saturate((normalEdge - _NormalThreshold) / (1.0 - _NormalThreshold + 0.001));
                
                float edgeStrength = max(depthStrength, normalStrength);
                
                // Sample original color for blending
                float4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                
                // Blend between scene color and edge color
                float4 finalColor = lerp(sceneColor, _EdgeColor, edgeStrength);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}