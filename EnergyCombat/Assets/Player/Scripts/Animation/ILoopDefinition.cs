using System;
using Animancer;

/// <summary>
/// Abstracts the two loop variants — single clip and 2D blend tree — behind a single interface.
/// Assign a concrete type in the Inspector via right-click → Managed Reference.
/// </summary>
public interface ILoopDefinition
{
    bool IsValid { get; }
    AnimancerState Play(AnimancerLayer layer, float fade);
}

[Serializable]
public class ClipLoopDef : ILoopDefinition
{
    public ClipTransition Clip;
    public bool IsValid => Clip?.IsValid == true;
    public AnimancerState Play(AnimancerLayer layer, float fade) => layer.Play(Clip, fade);
}

[Serializable]
public class MixerLoopDef : ILoopDefinition
{
    public MixerTransition2D Mixer;
    public bool IsValid => Mixer?.IsValid == true;
    public AnimancerState Play(AnimancerLayer layer, float fade) => layer.Play(Mixer, fade);
}
