using System;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.VisualScripting.Member;
using VIOSOWarpBlend;

internal class VIOSOBlitPass : ScriptableRenderPass
{
    ProfilingSampler m_ProfilingSampler = new ProfilingSampler("VIOSOBlit");
    Material m_Material;
    RTHandle m_viosoTarget;

    public VIOSOBlitPass(Material material)
    {
        m_Material = material;
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void SetTarget(RTHandle colorHandle)
    {
        m_viosoTarget = colorHandle;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        ConfigureTarget(m_viosoTarget);
    }


    /// <summary>
    /// Performs the Blit to update the texture with the VIOSO material 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="renderingData"></param>
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;

        if (cameraData.camera.cameraType != CameraType.Game)
        {
            Debug.Log("VIOSO: camera not game type");
            return;
        }
            

        if (m_Material == null)
            return;

        //SETUP VIOSO material
        if (VIOSOURPcamera._warperDict.TryGetValue(cameraData.camera.name, out VIOSOURPcamera.WarperSet s))
        {
            Matrix4x4 mVP = s._ppMatrix;

            m_Material.SetMatrix("matView", mVP);

            try
            { // update
                Warper.VWB_Warper ini = s._warper.Get();
                s._bBorder[1] = ini.bDoNotBlend ? 0 : 1;
                s._bBorder[3] = ini.bBicubic ? 1 : 0;
                m_Material.SetVector("bBorder", s._bBorder);
                s._blackBias[3] = ini.bDoNoBlack ? 0 : 1;
                m_Material.SetVector("blackBias", s._blackBias);

                if (ini.bPartialInput)
                {
                    m_Material.SetVector("offsScale", new Vector4(
                        ini.optimalRect.left / ini.optimalRes.cx,
                        ini.optimalRect.top / ini.optimalRes.cy,
                        (ini.optimalRect.right - ini.optimalRect.left) / ini.optimalRes.cx,
                        (ini.optimalRect.bottom - ini.optimalRect.top) / ini.optimalRes.cy));
                }
                else
                {
                    m_Material.SetVector("offsScale", new Vector4(0, 0, 1, 1));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("VIOSOWarpBlendPP.Render(" + cameraData.camera.name + ") " + ex.ToString());
            }

            m_Material.SetTexture("_texContent", m_viosoTarget);
            m_Material.SetVector("mapSize", s._size);
            m_Material.SetTexture("_texWarp", s._texWarp);
            m_Material.SetTexture("_texBlend", s._texBlend);
            m_Material.SetTexture("_texBlack", s._texBlack);

            
        }

        //APPLY
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            Blitter.BlitCameraTexture(cmd, m_viosoTarget, m_viosoTarget, m_Material, 0);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }
}