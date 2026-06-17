using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BladeMode.Visualization
{
    /// <summary>
    /// URP ScriptableRendererFeature (RenderGraph path, URP 17 / Unity 6).
    ///
    /// Renders a R32_SFloat colour texture containing the linear eye depth (metres) of
    /// every visible sliceable object, exposed globally as _SliceableDepthTexture.
    /// CutPlane.shader compares this against its own fragment depth to draw the
    /// intersection outline only where the cut plane meets sliceable geometry.
    ///
    /// Occlusion by non-sliceable geometry (e.g. the player) is handled per-fragment
    /// inside SliceableDepthCapture.shader — fragments behind the scene are discarded,
    /// so the player never leaks into _SliceableDepthTexture.
    ///
    /// Colour texture (not depth attachment) is used for SetGlobalTextureAfterPass because
    /// URP 17 RenderGraph has restrictions on exporting depth attachments as global textures.
    ///
    /// SETUP: In the Inspector open Settings/PC_Renderer, click "Add Renderer Feature" →
    /// SliceableDepthFeature. Assign SliceableLayers to match BladeModeSettings.SliceableLayers.
    /// </summary>
    public sealed class SliceableDepthFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public sealed class Settings
        {
            [Tooltip("Layers whose depth is captured. Must match BladeModeSettings.SliceableLayers.")]
            public LayerMask sliceableLayers;

            [Tooltip("AfterRenderingOpaques ensures all opaque sliceable geometry is captured " +
                     "and the camera depth buffer is fully populated for occlusion testing.")]
            public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public Settings settings = new Settings();

        Material         _depthMat;
        SliceableDepthPass _pass;

        public override void Create()
        {
            var shader = Shader.Find("BladeMode/SliceableDepthCapture");
            if (shader == null)
            {
                UnityEngine.Debug.LogError("[SliceableDepthFeature] Shader 'BladeMode/SliceableDepthCapture' " +
                                           "not found. Ensure the shader file exists in the project.");
                return;
            }
            _depthMat = CoreUtils.CreateEngineMaterial(shader);
            _pass = new SliceableDepthPass(settings, _depthMat);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null) return;
            var camType = renderingData.cameraData.camera.cameraType;
            if (camType == CameraType.SceneView || camType == CameraType.Reflection) return;
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_depthMat);
        }

        // ─────────────────────────────────────────────────────────────────────────

        sealed class SliceableDepthPass : ScriptableRenderPass
        {
            static readonly int         s_TexID   = Shader.PropertyToID("_SliceableDepthTexture");
            static readonly ShaderTagId s_Fwd     = new ShaderTagId("UniversalForward");
            static readonly ShaderTagId s_Unlit   = new ShaderTagId("SRPDefaultUnlit");

            readonly Settings _settings;
            readonly Material _depthMat;

            class PassData
            {
                // Static so the render func (which must be static) can reference it.
                public static readonly int SceneDepthID =
                    Shader.PropertyToID("_SceneDepthForSliceable");

                public RendererListHandle rendererList;
                public TextureHandle      cameraDepthTex;
            }

            public SliceableDepthPass(Settings settings, Material depthMat)
            {
                _settings        = settings;
                _depthMat        = depthMat;
                renderPassEvent  = settings.passEvent;
                profilingSampler = new ProfilingSampler("Sliceable Depth Capture");
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_depthMat == null) return;

                var cameraData    = frameData.Get<UniversalCameraData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var resourceData  = frameData.Get<UniversalResourceData>();

                var src = cameraData.cameraTargetDescriptor;

                // R32_SFloat colour RT: stores linear eye depth of visible sliceable surfaces.
                // Cleared to 1e6 (sentinel) so pixels with no geometry produce depthDiff >> LineWidth
                // in CutPlane.shader and show no intersection line.
                var colorDesc = new TextureDesc(src.width, src.height)
                {
                    colorFormat = GraphicsFormat.R32_SFloat,
                    clearBuffer = true,
                    clearColor  = new Color(1000000f, 0f, 0f, 0f),
                    msaaSamples = MSAASamples.None,
                    name        = "_SliceableDepthTexture"
                };
                TextureHandle colorHandle = renderGraph.CreateTexture(colorDesc);

                // Private depth buffer for hardware Z-ordering between multiple overlapping
                // sliceable objects.  Separate from the camera depth so we don't conflict with
                // URP's own depth writes.
                var depthDesc = new TextureDesc(src.width, src.height)
                {
                    colorFormat     = GraphicsFormat.None,
                    depthBufferBits = DepthBits.Depth16,
                    msaaSamples     = MSAASamples.None,
                    clearBuffer     = true,
                    name            = "SliceableInternalDepth"
                };
                TextureHandle internalDepth = renderGraph.CreateTexture(depthDesc);

                // Renderer list: all opaque sliceable objects, rendered with our override material.
                // The ShaderTagIds here select WHICH objects to draw; the override material
                // determines HOW they are drawn (outputting linear depth as colour).
                var sorting      = new SortingSettings(cameraData.camera) { criteria = SortingCriteria.CommonOpaque };
                var drawSettings = new DrawingSettings(s_Fwd, sorting);
                drawSettings.SetShaderPassName(1, s_Unlit);
                drawSettings.overrideMaterial          = _depthMat;
                drawSettings.overrideMaterialPassIndex = 0;

                var filterSettings = new FilteringSettings(
                    RenderQueueRange.opaque, _settings.sliceableLayers);

                var rlp = new RendererListParams(
                    renderingData.cullResults, drawSettings, filterSettings);

                using var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "Sliceable Depth Capture", out var passData, profilingSampler);

                passData.rendererList   = renderGraph.CreateRendererList(in rlp);
                passData.cameraDepthTex = resourceData.cameraDepth;

                // Colour output + private depth for Z-ordering.
                builder.SetRenderAttachment(colorHandle, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(internalDepth, AccessFlags.Write);
                // Camera depth is sampled in the shader to discard occluded fragments.
                builder.UseTexture(passData.cameraDepthTex, AccessFlags.Read);
                builder.UseRendererList(passData.rendererList);
                // Export colour texture globally so CutPlane.shader can sample it.
                builder.SetGlobalTextureAfterPass(colorHandle, s_TexID);
                // Required: allows SetGlobalTexture calls inside SetRenderFunc.
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
                {
                    // Make camera depth available to SliceableDepthCapture.shader as a global.
                    // Must be set before DrawRendererList.
                    ctx.cmd.SetGlobalTexture(PassData.SceneDepthID, data.cameraDepthTex);
                    ctx.cmd.DrawRendererList(data.rendererList);
                });
            }
        }
    }
}
