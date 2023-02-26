using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

public class ScreenSpaceOcclusionFeature : ScriptableRendererFeature
{
    public ScreenSpaceOcclusionSetting setting = new ScreenSpaceOcclusionSetting();
    private ScreenSpaceOcclusion m_Pass;
    private ScreenSpaceOcclusionDebug m_DebugPass;

    public override void Create()
    {
        m_Pass = new ScreenSpaceOcclusion();
        m_Pass.setting = setting;
        m_DebugPass = new ScreenSpaceOcclusionDebug();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (setting.intensity <= 0)
            return;

        renderer.EnqueuePass(m_Pass);

        if (setting.debugMode != ScreenSpaceOcclusionSetting.DebugMode.Disabled)
        {
            renderer.EnqueuePass(m_DebugPass);
        }
    }
}


[Serializable]
public class ScreenSpaceOcclusionSetting
{
    public enum AOType
    {
        HorizonBasedAmbientOcclusion,//HBAO
        GroundTruthBasedAmbientOcclusion,//GTAO
        ScalableAmbientObscurance,
    }

    public enum Quality
    {
        Lowest,
        Low,
        Medium,
        High,
        Highest
    }

    public enum Resolution
    {
        Full,
        Half
    }

    public enum BlurType
    {
        None,
        x2,
        x3,
        x4,
        x5
    }

    public enum ReconstructNormal
    {
        Disabled,
        Low,
        Medium,
        High,
    }

    public enum DebugMode
    {
        Disabled,
        AOOnly,
        ViewNormal,
    }


    [Tooltip("类型 HBAO/GTAO")]
    public AOType type = AOType.GroundTruthBasedAmbientOcclusion;

    [Tooltip("可见性估算采样数")]
    public Quality quality = Quality.Medium;

    [Tooltip("分辨率")]
    public Resolution resolution = Resolution.Full;

    [Tooltip("利用深度重建法线, 可以消除原始法线带来的一些不必要的高频信息, 有额外消耗")]
    public ReconstructNormal reconstructNormal = ReconstructNormal.Disabled;


    [Space(6)]
    [Tooltip("强度")]
    [Range(0f, 4f)]
    public float intensity = 0f;

    [Tooltip("采样半径")]
    [Range(0f, 10f)]
    public float radius = 0.8f;

    [Tooltip("像素层级采样半径限制")]
    [Range(16f, 512f)]
    public float maxRadiusPixels = 128f;

    [Tooltip("由于几何面连续性问题, 实际可见性会被多算, 偏移用以弥补")]
    [Range(0f, 0.99f)]
    public float bias = 0.5f;

    [Tooltip("GTAO中使用, 减少薄的物体AO过度的问题")]
    [Range(0, 1f)]
    public float thickness = 0f;

    [Tooltip("直接光部分AO遮蔽强度")]
    [Range(0, 1f)]
    public float directLightingStrength = 0.25f;

    [Tooltip("最大距离范围")]
    [Min(0)]
    public float maxDistance = 150f;

    [Tooltip("距离衰减")]
    public float distanceFalloff = 50f;

    [Tooltip("模糊采样次数")]
    public BlurType blurType = BlurType.x3;

    [Tooltip("锐化")]
    [Range(0f, 16f)]
    public float sharpness = 8f;

    public DebugMode debugMode = DebugMode.Disabled;
}
