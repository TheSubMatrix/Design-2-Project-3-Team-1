Shader "Fullscreen/SobelOperator"
{
    Properties
    {
        _EdgeColor ("Edge Color", Color) = (0, 0, 0, 1)
        _DepthThreshold ("Depth Threshold", Float) = 0.1
        _NormalThreshold ("Normal Threshold", Float) = 0.1
        _DepthSensitivity ("Depth Sensitivity", Float) = 1.0
        _NormalSensitivity ("Normal Sensitivity", Float) = 1.0
        _ThicknessScale ("Thickness Scale", Float) = 1.0
        _DepthNormalThreshold ("Depth Normal Threshold", Float) = 0.5
        _DepthNormalThresholdScale ("Depth Normal Threshold Scale", Float) = 7.0
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
                float centerDepth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    if (centerDepth < 0.0001)
                #else
                    if (centerDepth > 0.9999) 
                #endif
                {
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                }
                
                float linearDepth = LinearEyeDepth(centerDepth, _ZBufferParams);
                float3 centerNormal = SampleSceneNormals(uv);
                float3 viewSpaceNormal = centerNormal * 2.0 - 1.0;
                float normalLength = length(viewSpaceNormal);
                float normalThreshold = 1.0;
                
                if (normalLength > 0.01)
                {
                    viewSpaceNormal = viewSpaceNormal / normalLength;
                    float3 viewDir = normalize(input.ViewSpaceDir);
                    float ndotV = 1.0 - dot(viewSpaceNormal, -viewDir);
                    ndotV = saturate(ndotV);
                    float normalThreshold01 = saturate((ndotV - _DepthNormalThreshold) / (1.0 - _DepthNormalThreshold + 0.0001));
                    normalThreshold = normalThreshold01 * _DepthNormalThresholdScale + 1.0;
                }
                

                float depthThreshold = _DepthThreshold * normalThreshold;

                float2 scaledTexelSize = texelSize * _OutlineThickness;

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