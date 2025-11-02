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
        _DepthNormalThreshold ("Depth Normal Threshold", Range(0, 1)) = 0.5
        _DepthNormalThresholdScale ("Depth Normal Threshold Scale", Range(1, 10)) = 7.0
        _OutlineThickness ("Outline Thickness", Range(0.1, 5)) = 1.0
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
            #pragma vertex Vertex
            #pragma fragment Fragment
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
            float _DepthNormalThreshold;
            float _DepthNormalThresholdScale;
            float _OutlineThickness;

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
            
            struct VertexToFragment
            {
                float4 PositionCS : SV_POSITION;
                float2 Texcoord : TEXCOORD0;
                float3 ViewSpaceDir : TEXCOORD1;
            };
            
            VertexToFragment Vertex(Attributes input)
            {
                VertexToFragment output;
                output.PositionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.Texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                
                // Calculate view space direction for glancing angle detection
                float3 viewPos = ComputeViewSpacePosition(output.Texcoord, 1.0, UNITY_MATRIX_I_P);
                output.ViewSpaceDir = viewPos;
                
                return output;
            }
            
            float4 Fragment(VertexToFragment input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.Texcoord;
                float2 texelSize = _BlitTexture_TexelSize.xy;
                
                // Get center depth and normal
                float centerDepth = SampleSceneDepth(uv);
                
                // Early out for skybox - depth value of 0 or 1 depending on platform
                #if UNITY_REVERSED_Z
                    if (centerDepth < 0.0001) // Skybox in reversed Z
                #else
                    if (centerDepth > 0.9999) // Skybox in normal Z
                #endif
                {
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                }
                
                float linearDepth = LinearEyeDepth(centerDepth, _ZBufferParams);
                float3 centerNormal = SampleSceneNormals(uv);
                
                // Transform normal from 0...1 range to -1...1 range
                float3 viewSpaceNormal = centerNormal * 2.0 - 1.0;
                
                // Validate normal - if it's invalid or zero, use default threshold
                float normalLength = length(viewSpaceNormal);
                float normalThreshold = 1.0;
                
                if (normalLength > 0.01)
                {
                    // Normalize the normal
                    viewSpaceNormal = viewSpaceNormal / normalLength;
                    
                    // Calculate NdotV for glancing angle detection
                    // Normalize view direction and get the angle between normal and view
                    float3 viewDir = normalize(input.ViewSpaceDir);
                    float NdotV = 1.0 - dot(viewSpaceNormal, -viewDir);
                    
                    // Clamp NdotV to avoid extreme values
                    NdotV = saturate(NdotV);
                    
                    // Modulate depth threshold based on viewing angle
                    // At glancing angles (high NdotV), increase threshold to reduce artifacts
                    float normalThreshold01 = saturate((NdotV - _DepthNormalThreshold) / (1.0 - _DepthNormalThreshold + 0.0001));
                    normalThreshold = normalThreshold01 * _DepthNormalThresholdScale + 1.0;
                }
                
                // Apply angle-based threshold modulation to depth threshold
                float depthThreshold = _DepthThreshold * normalThreshold;

                // Scale texel size by outline thickness to make lines thicker
                float2 scaledTexelSize = texelSize * _OutlineThickness;

                // Sample 3x3 neighborhood for depth
                float depthSamples[9];
                float3 normalSamples[9];
                int index = 0;
                
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(x, y) * scaledTexelSize;
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
                bool isOrtho = unity_OrthoParams.w == 1.0;
                
                if (!isOrtho)
                {
                    // Perspective: Compensate for depth gradient compression at distance
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

                // Combine depth and normal edges with angle-modulated threshold
                float depthStrength = saturate((depthEdge - depthThreshold) / (1.0 - depthThreshold + 0.001));
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