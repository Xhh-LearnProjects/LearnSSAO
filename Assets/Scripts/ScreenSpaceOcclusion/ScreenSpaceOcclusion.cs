using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceOcclusion : ScriptableRenderPass
{

    static class ShaderConstants
    {
        internal static readonly int OcclusionDepthTex = Shader.PropertyToID("_OcclusionDepthTex");
        internal static readonly int OcclusionTempTex = Shader.PropertyToID("_OcclusionTempTex");
        internal static readonly int OcclusionFinalTex = Shader.PropertyToID("_OcclusionFinalTex");
    }

    ProfilingSampler m_ProfilingSampler;
    RenderTextureDescriptor m_AmbientOcclusionDescriptor;
    public ScreenSpaceOcclusionSetting setting { get; set; }

    Material m_AmbientOcclusionMaterial;

    RTHandle m_OcclusionFinalRT;
    RTHandle m_OcclusionDepthRT;
    RTHandle m_OcclusionTempRT;

    public ScreenSpaceOcclusion()
    {
        m_ProfilingSampler = new ProfilingSampler("ScreenSpaceOcclusion");
        m_AmbientOcclusionMaterial = CoreUtils.CreateEngineMaterial("Hidden/PostProcessing/ScreenSpaceOcclusion");
    }

    private void SetupMaterials(ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;

        var width = cameraData.cameraTargetDescriptor.width;
        var height = cameraData.cameraTargetDescriptor.height;
        var widthAO = m_AmbientOcclusionDescriptor.width;
        var heightAO = m_AmbientOcclusionDescriptor.height;

        float invFocalLenX = 1.0f / cameraData.camera.projectionMatrix.m00;
        float invFocalLenY = 1.0f / cameraData.camera.projectionMatrix.m11;
    }


    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        m_AmbientOcclusionDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        m_AmbientOcclusionDescriptor.msaaSamples = 1;
        m_AmbientOcclusionDescriptor.depthBufferBits = 0;

        if (setting.resolution == ScreenSpaceOcclusionSetting.Resolution.Half)
        {
            m_AmbientOcclusionDescriptor.width = m_AmbientOcclusionDescriptor.width << 1;
            m_AmbientOcclusionDescriptor.height = m_AmbientOcclusionDescriptor.height << 1;
        }

        RenderingUtils.ReAllocateIfNeeded(ref m_OcclusionFinalRT, m_AmbientOcclusionDescriptor, FilterMode.Bilinear);
        ConfigureTarget(m_OcclusionFinalRT);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get();

        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            SetupMaterials(ref renderingData);

            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            //AO
            m_OcclusionDepthRT = RTHandles.Alloc(m_AmbientOcclusionDescriptor, FilterMode.Bilinear);
            Blit(cmd, source, m_OcclusionDepthRT, m_AmbientOcclusionMaterial, 0);

            //Blur
            if (setting.blurType != ScreenSpaceOcclusionSetting.BlurType.None)
            {
                m_OcclusionTempRT = RTHandles.Alloc(m_AmbientOcclusionDescriptor, FilterMode.Bilinear);
                Blit(cmd, m_OcclusionDepthRT, m_OcclusionTempRT, m_AmbientOcclusionMaterial, 1);
                Blit(cmd, m_OcclusionTempRT, m_OcclusionDepthRT, m_AmbientOcclusionMaterial, 2);
                m_OcclusionTempRT.Release();
            }

            //Composite
            // Blit(cmd, m_OcclusionDepthRT, source, m_AmbientOcclusionMaterial, 3);

            m_OcclusionDepthRT.Release();
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        m_OcclusionFinalRT.Release();
        CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, false);
    }
}
