using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceOcclusionDebug : ScriptableRenderPass
{

    RTHandle m_SourceRT;

    public ScreenSpaceOcclusionDebug()
    {
    }

    public void Setup(RTHandle target)
    {
        m_SourceRT = target;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (m_SourceRT == null)
            return;

        var cmd = CommandBufferPool.Get(nameof(ScreenSpaceOcclusionDebug));
        cmd.Clear();

        var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
        Blit(cmd, m_SourceRT, source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }


}
