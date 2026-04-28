using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using VIOSOWarpBlend;

internal class VIOSOBlitPass : ScriptableRenderPass
{
    ProfilingSampler m_ProfilingSampler = new ProfilingSampler("VIOSOBlit");
    Material m_Material;

    class CopyPassData
    {
        public TextureHandle source;
    }

    class PassData
    {
        public Material material;
        public TextureHandle sourceTexture;
        public bool hasWarper;
        public Matrix4x4 matView;
        public Vector4 bBorder;
        public Vector4 blackBias;
        public Vector4 offsScale;
        public Vector4 mapSize;
        public Texture texWarp;
        public Texture texBlend;
        public Texture texBlack;
    }

    public VIOSOBlitPass(Material material)
    {
        m_Material = material;
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        if (cameraData.camera.cameraType != CameraType.Game) return;
        if (m_Material == null) return;

        TextureHandle activeColor = resourceData.activeColorTexture;

        // Create a temp copy of the color buffer so we can read from it while writing to activeColor
        RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        TextureHandle tempHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_VIOSOTemp", false);

        // Pass 1: copy active color into temp — must be fully closed before Pass 2 opens
        using (var copyBuilder = renderGraph.AddRasterRenderPass<CopyPassData>("VIOSOCopyColor", out var copyData))
        {
            copyData.source = activeColor;
            copyBuilder.UseTexture(activeColor, AccessFlags.Read);
            copyBuilder.SetRenderAttachment(tempHandle, 0);
            copyBuilder.SetRenderFunc(static (CopyPassData data, RasterGraphContext ctx) =>
            {
                Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
            });
        }

        // Pass 2: VIOSO warp — read from temp, write warped result back to activeColor
        using var builder = renderGraph.AddRasterRenderPass<PassData>("VIOSOBlit", out var passData, m_ProfilingSampler);

        passData.material = m_Material;
        passData.sourceTexture = tempHandle;
        passData.hasWarper = false;

        if (VIOSOURPcamera._warperDict.TryGetValue(cameraData.camera.name, out VIOSOURPcamera.WarperSet s))
        {
            passData.hasWarper = true;
            passData.matView = s._ppMatrix;

            try
            {
                Warper.VWB_Warper ini = s._warper.Get();
                s._bBorder[1] = ini.bDoNotBlend ? 0 : 1;
                s._bBorder[3] = ini.bBicubic ? 1 : 0;
                passData.bBorder = s._bBorder;
                s._blackBias[3] = ini.bDoNoBlack ? 0 : 1;
                passData.blackBias = s._blackBias;

                passData.offsScale = ini.bPartialInput
                    ? new Vector4(
                        ini.optimalRect.left / ini.optimalRes.cx,
                        ini.optimalRect.top / ini.optimalRes.cy,
                        (ini.optimalRect.right - ini.optimalRect.left) / ini.optimalRes.cx,
                        (ini.optimalRect.bottom - ini.optimalRect.top) / ini.optimalRes.cy)
                    : new Vector4(0, 0, 1, 1);
            }
            catch (Exception ex)
            {
                Debug.LogError("VIOSOWarpBlendPP.Render(" + cameraData.camera.name + ") " + ex.ToString());
            }

            passData.mapSize = s._size;
            passData.texWarp = s._texWarp;
            passData.texBlend = s._texBlend;
            passData.texBlack = s._texBlack;
        }

        builder.UseTexture(tempHandle, AccessFlags.Read);
        builder.SetRenderAttachment(activeColor, 0);
        builder.AllowPassCulling(false);
        builder.AllowGlobalStateModification(true);

        builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
        {
            if (!data.hasWarper) return;

            data.material.SetMatrix("matView", data.matView);
            data.material.SetVector("bBorder", data.bBorder);
            data.material.SetVector("blackBias", data.blackBias);
            data.material.SetVector("offsScale", data.offsScale);
            data.material.SetVector("mapSize", data.mapSize);
            data.material.SetTexture("_texWarp", data.texWarp);
            data.material.SetTexture("_texBlend", data.texBlend);
            data.material.SetTexture("_texBlack", data.texBlack);

            // _texContent is the shader's input texture (equivalent to _BlitTexture set by Blitter)
            ctx.cmd.SetGlobalTexture("_texContent", data.sourceTexture);
            Blitter.BlitTexture(ctx.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 0);
        });
    }
}
