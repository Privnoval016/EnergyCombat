Shader "BladeMode/SliceableDepthCapture"
{
    // Override material used by SliceableDepthFeature to render sliceable objects
    // into a R32_SFloat colour texture storing linear eye depth (metres).
    //
    // _SceneDepthForSliceable is the camera depth set as a global by the feature
    // before DrawRendererList.  Fragments whose linear depth exceeds the scene depth
    // at that pixel are discarded, so geometry behind the player (or any non-sliceable
    // opaque surface) never contributes to _SliceableDepthTexture.
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "SliceableDepthCapture"
            // ZWrite/ZTest against the feature's private internal depth buffer
            // (bound as SetRenderAttachmentDepth in the feature) so overlapping
            // sliceable objects are Z-sorted correctly.
            ZWrite On
            ZTest  LEqual
            Cull   Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_SceneDepthForSliceable);
            SAMPLER(sampler_SceneDepthForSliceable);

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
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vp.positionCS;
                OUT.screenPos   = ComputeScreenPos(vp.positionCS);
                OUT.linearDepth = -vp.positionVS.z;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv      = IN.screenPos.xy / IN.screenPos.w;
                float  rawScene = SAMPLE_TEXTURE2D_X(_SceneDepthForSliceable,
                                                      sampler_SceneDepthForSliceable, uv).r;

                // rawScene == 0 in reverse-Z means "far plane / nothing there" — skip discard.
                // When the texture is unbound (returns 0) we also skip so all fragments render.
                if (rawScene > 0.000001)
                {
                    float sceneLinear = LinearEyeDepth(rawScene, _ZBufferParams);
                    // Discard if this sliceable surface is behind non-sliceable geometry.
                    // 0.05 m epsilon prevents self-discard due to z-fighting.
                    if (IN.linearDepth > sceneLinear + 0.05) discard;
                }

                return float4(IN.linearDepth, 0.0, 0.0, 1.0);
            }
            ENDHLSL
        }
    }
}
