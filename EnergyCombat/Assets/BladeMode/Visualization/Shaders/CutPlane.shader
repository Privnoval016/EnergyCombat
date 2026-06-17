Shader "BladeMode/CutPlane"
{
    Properties
    {
        _PlaneColor         ("Plane Fill Color",         Color) = (0, 0.8, 1, 0.15)
        [HDR] _IntersectionColor ("Intersection Color", Color) = (0, 1, 1, 1)
        _LineWidth          ("Intersection Line Width",  Float) = 0.04
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Linear eye depth of visible sliceable surfaces, set by SliceableDepthFeature.
            // Cleared to 1e6 where no sliceable geometry exists.
            // Occlusion by non-sliceable geometry (e.g. player) is handled by the feature
            // via per-fragment discard, so no _CameraDepthTexture comparison is needed here.
            TEXTURE2D(_SliceableDepthTexture);
            SAMPLER(sampler_SliceableDepthTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _PlaneColor;
                float4 _IntersectionColor;
                float  _LineWidth;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
                float  linearDepth : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.screenPos   = ComputeScreenPos(posInputs.positionCS);
                OUT.linearDepth = -posInputs.positionVS.z;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.screenPos.xy / IN.screenPos.w;

                // _SliceableDepthTexture.r is already linear eye depth (metres).
                // Pixels with no sliceable geometry are cleared to 1e6 by the feature,
                // making depthDiff >> _LineWidth so no intersection line appears there.
                float sliceableLinear = SAMPLE_TEXTURE2D(_SliceableDepthTexture,
                                                          sampler_SliceableDepthTexture, uv).r;

                float depthDiff = abs(sliceableLinear - IN.linearDepth);
                float onLine    = step(depthDiff, _LineWidth);

                return lerp(_PlaneColor, _IntersectionColor, onLine);
            }
            ENDHLSL
        }
    }
}
