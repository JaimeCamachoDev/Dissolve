using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// Track de Timeline para CharacterSandDissolve: clips de Solve (aparecer) y Dissolve (desaparecer).
// Fuera de los clips mantiene el estado del ultimo clip (antes de un Solve: oculto; tras un Dissolve: oculto;
// entre Solve y Dissolve: completo), asi el scrub y los saltos de tiempo dan siempre el mismo resultado.
[TrackColor(0.85f, 0.65f, 0.3f)]
[TrackClipType(typeof(SandDissolveClip))]
[TrackBindingType(typeof(CharacterSandDissolve))]
public class SandDissolveTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        var playable = ScriptPlayable<SandDissolveMixer>.Create(graph, inputCount);
        var mixer = playable.GetBehaviour();
        mixer.clips.Clear();
        foreach (var clip in GetClips())
        {
            if (clip.asset is SandDissolveClip asset)
            {
                mixer.clips.Add(new SandDissolveMixer.ClipInfo { start = clip.start, end = clip.end, mode = asset.mode });
                clip.displayName = asset.mode == SandDissolveClip.Mode.Solve ? "Sand Solve" : "Sand Dissolve";
            }
        }
        mixer.clips.Sort((a, b) => a.start.CompareTo(b.start));
        return playable;
    }

    // La preview del Timeline en el editor revierte lo que toca el disolve (materiales y renderers)
    public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
    {
        var target = director.GetGenericBinding(this) as CharacterSandDissolve;
        if (target == null) return;
        driver.AddFromName<CharacterSandDissolve>(target.gameObject, "visibility");
        driver.AddFromName<CharacterSandDissolve>(target.gameObject, "dust");
        driver.AddFromName<CharacterSandDissolve>(target.gameObject, "gather");
        driver.AddFromName<CharacterSandDissolve>(target.gameObject, "colorVisibility");
        driver.AddFromName<CharacterSandDissolve>(target.gameObject, "swirl");
        foreach (var s in target.slots)
        {
            if (s.renderer == null) continue;
            driver.AddFromName(s.renderer, "m_Enabled");
            int count = s.renderer.sharedMaterials.Length;
            for (int i = 0; i < count; i++)
                driver.AddFromName(s.renderer, $"m_Materials.Array.data[{i}]");
        }
        base.GatherProperties(director, driver);
    }
}

public class SandDissolveMixer : PlayableBehaviour
{
    public struct ClipInfo
    {
        public double start;
        public double end;
        public SandDissolveClip.Mode mode;
    }

    public readonly List<ClipInfo> clips = new List<ClipInfo>();
    CharacterSandDissolve m_Target;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var target = playerData as CharacterSandDissolve;
        if (target == null || clips.Count == 0) return;
        m_Target = target;
        double t = playable.GetTime();

        // Estado por defecto: lo contrario de como empieza el primer clip
        float vis = clips[0].mode == SandDissolveClip.Mode.Solve ? 0f : 1f;
        float dust = 0f, gather = 0f;
        foreach (var c in clips)
        {
            if (t < c.start) break;
            float duration = (float)(c.end - c.start);
            if (t < c.end)
            {
                float local = (float)(t - c.start);
                if (c.mode == SandDissolveClip.Mode.Solve) target.EvaluateSolve(local, duration);
                else target.EvaluateDissolve(local, duration);
                target.ApplyFromTimeline();
                return;
            }
            vis = c.mode == SandDissolveClip.Mode.Solve ? 1f : 0f;
        }
        target.SetTimelineState(vis, dust, gather);
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (m_Target != null) m_Target.ReleaseTimeline();
    }
}
