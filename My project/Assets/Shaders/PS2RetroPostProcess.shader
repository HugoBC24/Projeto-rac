Shader "ProjectRac/PS2RetroPostProcess"
{
    Properties
    {
        _TargetWidth ("Retro Width", Float) = 480.0
        _TargetHeight ("Retro Height", Float) = 270.0
        _ColorSteps ("Color Steps Per Channel", Range(4, 64)) = 18.0
        _DitherStrength ("Bayer Dither Strength", Range(0, 1.5)) = 0.90
        _BlackCrush ("Black Crush", Range(0, 0.15)) = 0.035
        _ScanlineIntensity ("CRT Scanline Intensity", Range(0, 0.6)) = 0.18
        _ScanlineCount ("Scanline Count", Float) = 270.0
        _ChromaOffset ("Analog RGB Bleed", Range(0, 0.005)) = 0.0012
        _NoiseIntensity ("VHS Grain Intensity", Range(0, 0.2)) = 0.065
        _EnablePillarbox ("Enable 4:3 Pillarbox (0 or 1)", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "PS2RetroPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _TargetWidth;
                float _TargetHeight;
                float _ColorSteps;
                float _DitherStrength;
                float _BlackCrush;
                float _ScanlineIntensity;
                float _ScanlineCount;
                float _ChromaOffset;
                float _NoiseIntensity;
                float _EnablePillarbox;
            CBUFFER_END

            // Standard 4x4 Bayer ordered dithering threshold matrix normalized to [-0.5, +0.5].
            float SampleBayer4x4(int2 pixelPos)
            {
                static const float bayer[16] =
                {
                     0.0,  8.0,  2.0, 10.0,
                    12.0,  4.0, 14.0,  6.0,
                     3.0, 11.0,  1.0,  9.0,
                    15.0,  7.0, 13.0,  5.0
                };

                int x = pixelPos.x & 3;
                int y = pixelPos.y & 3;
                return (bayer[y * 4 + x] / 16.0) - 0.5;
            }

            float HashNoise(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z) - 0.5;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                // Optional classic 4:3 pillarbox framing (12.5% black border on each side of 16:9).
                if (_EnablePillarbox > 0.5 && (uv.x < 0.125 || uv.x > 0.875))
                {
                    return half4(0.0, 0.0, 0.0, 1.0);
                }

                float2 grid = max(float2(_TargetWidth, _TargetHeight), float2(64.0, 64.0));
                float2 pixelCoord = floor(uv * grid);
                float2 snappedUV = (pixelCoord + 0.5) / grid;

                // Subtle analog composite RGB channel bleed (Stay Out of the House / VHS vibe).
                float2 chromaShift = float2(_ChromaOffset, 0.0);
                float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, saturate(snappedUV + chromaShift)).r;
                float g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, snappedUV).g;
                float b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, saturate(snappedUV - chromaShift)).b;
                float3 col = float3(r, g, b);

                // Convert Linear URP buffer to perceptual sRGB so PS2 quantization & Bayer dithering
                // preserve shadow detail evenly (just like real gamma-space PS1/PS2 framebuffers).
                col = pow(max(col, 0.0), 1.0 / 2.2);

                // Gentle black crush in sRGB space for moody shadows without erasing dark rooms.
                col = saturate((col - _BlackCrush) / max(1.0 - _BlackCrush, 0.001));

                // Attenuate grain and dither only in true black so dark voids stay clean while shadows keep Bayer mesh.
                float luma = dot(col, float3(0.299, 0.587, 0.114));
                float lumaMask = smoothstep(0.01, 0.08, luma);

                // Animated analog VHS grain sampled on the retro pixel grid.
                float timeSeed = floor(_Time.y * 24.0);
                float grain = HashNoise(pixelCoord + timeSeed * 17.31) * _NoiseIntensity * lumaMask;
                col = saturate(col + grain);

                // PS1/PS2 color quantization with visible Bayer 4x4 ordered dithering.
                float dither = SampleBayer4x4(int2(pixelCoord)) * lumaMask;
                float steps = max(_ColorSteps, 2.0);
                col = floor(saturate(col + (dither * _DitherStrength / steps)) * steps + 0.5) / steps;

                // Subtle horizontal CRT scanlines.
                float scanWave = 0.5 + 0.5 * sin(uv.y * _ScanlineCount * 6.2831853);
                float scanFactor = lerp(1.0 - _ScanlineIntensity, 1.0, scanWave);
                col *= scanFactor;

                // Convert back to Linear for URP final blit output.
                col = pow(max(col, 0.0), 2.2);
                return half4(saturate(col), 1.0);
            }
            ENDHLSL
        }
    }
}
