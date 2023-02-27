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
        internal static readonly int FullTexelSize = Shader.PropertyToID("_Full_TexelSize");
        internal static readonly int ScaledTexelSize = Shader.PropertyToID("_Scaled_TexelSize");
        internal static readonly int TargetScale = Shader.PropertyToID("_TargetScale");
        internal static readonly int UVToView = Shader.PropertyToID("_UVToView");
        internal static readonly int WorldToCameraMatrix = Shader.PropertyToID("_WorldToCameraMatrix");
        internal static readonly int Radius = Shader.PropertyToID("_Radius");
        internal static readonly int RadiusToScreen = Shader.PropertyToID("_RadiusToScreen");
        internal static readonly int MaxRadiusPixels = Shader.PropertyToID("_MaxRadiusPixels");
        internal static readonly int InvRadius2 = Shader.PropertyToID("_InvRadius2");
        internal static readonly int AngleBias = Shader.PropertyToID("_AngleBias");
        internal static readonly int AOMultiplier = Shader.PropertyToID("_AOMultiplier");
        internal static readonly int Intensity = Shader.PropertyToID("_Intensity");
        internal static readonly int Thickness = Shader.PropertyToID("_Thickness");
        internal static readonly int MaxDistance = Shader.PropertyToID("_MaxDistance");
        internal static readonly int DistanceFalloff = Shader.PropertyToID("_DistanceFalloff");
        internal static readonly int BlurSharpness = Shader.PropertyToID("_BlurSharpness");
        internal static readonly int CameraProjMatrix = Shader.PropertyToID("_CameraProjMatrix");


        public static string GetAOTypeKeyword(ScreenSpaceOcclusionSetting.AOType type)
        {
            switch (type)
            {
                case ScreenSpaceOcclusionSetting.AOType.GroundTruthBasedAmbientOcclusion:
                    return "GROUNDTRUTH_BASED_AMBIENTOCCLUSION";
                case ScreenSpaceOcclusionSetting.AOType.ScalableAmbientObscurance:
                    return "SCALABLE_AMBIENT_OBSCURANCE";
                case ScreenSpaceOcclusionSetting.AOType.HorizonBasedAmbientOcclusion:
                default:
                    return "HORIZON_BASED_AMBIENTOCCLUSION";
            }
        }
        public static string GetQualityKeyword(ScreenSpaceOcclusionSetting.Quality quality)
        {
            switch (quality)
            {
                case ScreenSpaceOcclusionSetting.Quality.Lowest:
                    return "QUALITY_LOWEST";
                case ScreenSpaceOcclusionSetting.Quality.Low:
                    return "QUALITY_LOW";
                case ScreenSpaceOcclusionSetting.Quality.Medium:
                    return "QUALITY_MEDIUM";
                case ScreenSpaceOcclusionSetting.Quality.High:
                    return "QUALITY_HIGH";
                case ScreenSpaceOcclusionSetting.Quality.Highest:
                    return "QUALITY_HIGHEST";
                default:
                    return "QUALITY_MEDIUM";
            }
        }

        public static string GetBlurRadiusKeyword(ScreenSpaceOcclusionSetting.BlurType blurType)
        {
            switch (blurType)
            {
                case ScreenSpaceOcclusionSetting.BlurType.x2:
                    return "BLUR_RADIUS_2";
                case ScreenSpaceOcclusionSetting.BlurType.x3:
                    return "BLUR_RADIUS_3";
                case ScreenSpaceOcclusionSetting.BlurType.x4:
                    return "BLUR_RADIUS_4";
                case ScreenSpaceOcclusionSetting.BlurType.x5:
                    return "BLUR_RADIUS_5";
                case ScreenSpaceOcclusionSetting.BlurType.None:
                default:
                    return "BLUR_RADIUS_3";
            }
        }

        public static string GetReconstructNormal(ScreenSpaceOcclusionSetting.ReconstructNormal reconstructNormal)
        {
            switch (reconstructNormal)
            {
                case ScreenSpaceOcclusionSetting.ReconstructNormal.Low:
                    return "RECONSTRUCT_NORMAL_LOW";
                case ScreenSpaceOcclusionSetting.ReconstructNormal.Medium:
                    return "RECONSTRUCT_NORMAL_MEDIUM";
                case ScreenSpaceOcclusionSetting.ReconstructNormal.High:
                    return "RECONSTRUCT_NORMAL_HIGH";
                case ScreenSpaceOcclusionSetting.ReconstructNormal.Disabled:
                default:
                    return "_";
            }
        }

        public static string GetDebugKeyword(ScreenSpaceOcclusionSetting.DebugMode debugMode)
        {
            switch (debugMode)
            {
                case ScreenSpaceOcclusionSetting.DebugMode.AOOnly:
                    return "DEBUG_AO";
                case ScreenSpaceOcclusionSetting.DebugMode.ViewNormal:
                    return "DEBUG_VIEWNORMAL";
                case ScreenSpaceOcclusionSetting.DebugMode.Disabled:
                default:
                    return "_";
            }
        }
    }

    ProfilingSampler m_ProfilingSampler;
    RenderTextureDescriptor m_AmbientOcclusionDescriptor;
    public ScreenSpaceOcclusionSetting settings { get; set; }

    Material m_AmbientOcclusionMaterial;
    string[] m_ShaderKeywords = new string[5];

    RTHandle m_OcclusionFinalRT;
    RTHandle m_OcclusionDepthRT;
    RTHandle m_OcclusionTempRT;

    public RTHandle OcclusionFinalRT => m_OcclusionFinalRT;

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

        if (settings.type == ScreenSpaceOcclusionSetting.AOType.ScalableAmbientObscurance)
            m_AmbientOcclusionMaterial.SetMatrix(ShaderConstants.CameraProjMatrix, cameraData.camera.projectionMatrix);

        var targetScale = settings.resolution == ScreenSpaceOcclusionSetting.Resolution.Half ?
                            new Vector4((width + 0.5f) / width, (height + 0.5f) / height, 1f, 1f) :
                            Vector4.one;

        float maxRadInPixels = Mathf.Max(16, settings.maxRadiusPixels * Mathf.Sqrt((width * height) / (1080.0f * 1920.0f)));

        m_AmbientOcclusionMaterial.SetVector(ShaderConstants.FullTexelSize, new Vector4(1f / width, 1f / height, width, height));
        m_AmbientOcclusionMaterial.SetVector(ShaderConstants.ScaledTexelSize, new Vector4(1f / widthAO, 1f / heightAO, widthAO, heightAO));
        m_AmbientOcclusionMaterial.SetVector(ShaderConstants.TargetScale, targetScale);
        m_AmbientOcclusionMaterial.SetVector(ShaderConstants.UVToView, new Vector4(2.0f * invFocalLenX, -2.0f * invFocalLenY, -1.0f * invFocalLenX, 1.0f * invFocalLenY));
        m_AmbientOcclusionMaterial.SetMatrix(ShaderConstants.WorldToCameraMatrix, cameraData.camera.worldToCameraMatrix);

        m_AmbientOcclusionMaterial.SetFloat(ShaderConstants.Radius, settings.radius);
        m_AmbientOcclusionMaterial.SetFloat(ShaderConstants.RadiusToScreen, settings.radius * 0.5f * (height / (invFocalLenY * 2.0f)));
        m_AmbientOcclusionMaterial.SetFloat(ShaderConstants.MaxRadiusPixels, maxRadInPixels);
        m_AmbientOcclusionMaterial.SetFloat(ShaderConstants.InvRadius2, 1.0f / (settings.radius * settings.radius));
        m_AmbientOcclusionMaterial.SetFloat(ShaderConstants.AngleBias, settings.bias);
        m_AmbientOcclusionMaterial.SetFloat(ShaderConstants.AOMultiplier, 2.0f * (1.0f / (1.0f - settings.bias)));
        m_AmbientOcclusionMaterial.SetFloat(ShaderConstants.Intensity, settings.intensity);
        m_AmbientOcclusionMaterial.SetFloat(ShaderConstants.Thickness, settings.thickness);
        m_AmbientOcclusionMaterial.SetFloat(ShaderConstants.MaxDistance, settings.maxDistance);
        m_AmbientOcclusionMaterial.SetFloat(ShaderConstants.DistanceFalloff, settings.distanceFalloff);
        m_AmbientOcclusionMaterial.SetFloat(ShaderConstants.BlurSharpness, settings.sharpness);
        // -------------------------------------------------------------------------------------------------
        // local shader keywords
        m_ShaderKeywords[0] = ShaderConstants.GetAOTypeKeyword(settings.type);
        m_ShaderKeywords[1] = ShaderConstants.GetQualityKeyword(settings.quality);
        m_ShaderKeywords[2] = ShaderConstants.GetBlurRadiusKeyword(settings.blurType);
        m_ShaderKeywords[3] = ShaderConstants.GetReconstructNormal(settings.reconstructNormal);
        m_ShaderKeywords[4] = ShaderConstants.GetDebugKeyword(settings.debugMode);

        m_AmbientOcclusionMaterial.shaderKeywords = m_ShaderKeywords;
    }


    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, true);
        // ---------------------------------------------------------------------------

        m_AmbientOcclusionDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        m_AmbientOcclusionDescriptor.msaaSamples = 1;
        m_AmbientOcclusionDescriptor.depthBufferBits = 0;

        if (settings.resolution == ScreenSpaceOcclusionSetting.Resolution.Half)
        {
            m_AmbientOcclusionDescriptor.width = m_AmbientOcclusionDescriptor.width >> 1;
            m_AmbientOcclusionDescriptor.height = m_AmbientOcclusionDescriptor.height >> 1;
        }

        RenderingUtils.ReAllocateIfNeeded(ref m_OcclusionFinalRT, m_AmbientOcclusionDescriptor, FilterMode.Bilinear, name: "OcclusionFinalRT");
        RenderingUtils.ReAllocateIfNeeded(ref m_OcclusionDepthRT, m_AmbientOcclusionDescriptor, FilterMode.Bilinear, name: "OcclusionDepthRT");
        RenderingUtils.ReAllocateIfNeeded(ref m_OcclusionTempRT, m_AmbientOcclusionDescriptor, FilterMode.Bilinear, name: "OcclusionTempRT");


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
            Blit(cmd, source, m_OcclusionDepthRT, m_AmbientOcclusionMaterial, 0);

            //Blur
            if (settings.blurType != ScreenSpaceOcclusionSetting.BlurType.None)
            {
                Blit(cmd, m_OcclusionDepthRT, m_OcclusionTempRT, m_AmbientOcclusionMaterial, 1);
                Blit(cmd, m_OcclusionTempRT, m_OcclusionDepthRT, m_AmbientOcclusionMaterial, 2);
            }

            cmd.SetGlobalTexture("_ScreenSpaceOcclusionTexture", m_OcclusionFinalRT);

            //Composite
            Blit(cmd, m_OcclusionDepthRT, m_OcclusionFinalRT, m_AmbientOcclusionMaterial, 3);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {

        CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, false);
    }

    public void Dispose()
    {
        m_OcclusionFinalRT?.Release();
        m_OcclusionDepthRT?.Release();

    }

}
