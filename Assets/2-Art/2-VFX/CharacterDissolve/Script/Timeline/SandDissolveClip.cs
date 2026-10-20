using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// Clip de SandDissolveTrack. La duracion del clip es la de la fase completa:
//   Solve    = remolino de arena que forma el cuerpo de abajo arriba (estatua de arena y color justo detras)
//   Dissolve = el cuerpo se deshace de los pies a la cabeza + el polvo se desvanece
// (las fases son fracciones de la duracion: ver los campos Solve/Dissolve de CharacterSandDissolve).
public class SandDissolveClip : PlayableAsset, ITimelineClipAsset
{
    public enum Mode { Solve, Dissolve }

    public Mode mode = Mode.Solve;

    public ClipCaps clipCaps => ClipCaps.None;

    public override double duration => mode == Mode.Solve ? 3.2 : 5.6;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        // La logica esta en SandDissolveMixer (evalua por tiempo del Timeline)
        return Playable.Create(graph);
    }
}
