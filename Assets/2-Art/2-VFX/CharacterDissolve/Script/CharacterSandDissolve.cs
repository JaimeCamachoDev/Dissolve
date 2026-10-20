using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

// Aparicion / desaparicion de un personaje en arena (concept "Personajes · Arena y Recuerdo").
//   visibility 1 = presencia completa, 0 = el cuerpo es solo arena. Lo visible es lo que queda por encima
//   del borde: al aparecer el borde baja (la cabeza se define primero), al disolverse sube (los pies primero).
//   dust     = silueta de polvo sobre la parte que aun no es visible ("polvo con forma").
//   gather   = 1 la arena del borde converge hacia el cuerpo (solve), 0 se la lleva el viento (dissolve).
// Los tres valores se pueden animar desde Timeline; PlaySolve/PlayDissolve los animan solos.
// Mientras esta completo y quieto usa los materiales originales (sin clip) y apaga el VFX.
[ExecuteAlways]
[DisallowMultipleComponent]
public class CharacterSandDissolve : MonoBehaviour
{
    [Serializable]
    public class RendererSlot
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public Material[] dissolveMaterials;
        [Tooltip("VFX que muestrea este SkinnedMeshRenderer (opcional)")]
        public VisualEffect vfx;
        [Tooltip("LUT de triangulos ponderada por area (Tools/VFX/Bake Triangle LUT)")]
        public Texture2D triangleLut;
        [Tooltip("Fraccion de superficie del personaje que ocupa esta malla (reparte las particulas)")]
        public float areaWeight = 1f;
        [NonSerialized] public bool live;
    }

    [Header("Estado (animable)")]
    [Range(0f, 1f)] public float visibility = 1f;
    [Range(0f, 1f)] public float dust = 0f;
    [Range(0f, 1f)] public float gather = 0f;
    [Tooltip("Solve: el color real vuelve por detras del borde (entre ambos el cuerpo es estatua de arena)")]
    [Range(0f, 1f)] public float colorVisibility = 1f;
    [Tooltip("Solve: emision del remolino de arena que sube del suelo y se deposita en el cuerpo")]
    [Range(0f, 1f)] public float swirl = 0f;

    [Header("Solve (segundos; un clip mas largo/corto los escala)")]
    [Tooltip("Duracion nominal del solve")]
    public float solveLength = 3.2f;
    [Tooltip("01 el remolino emite durante este tiempo")]
    public float swirlEmitTime = 0.7f;
    [Tooltip("Tiempo de vuelo del remolino hasta los pies")]
    public float swirlTravel = 0.9f;
    [Tooltip("Tiempo de vuelo extra para llegar a la cabeza (los pies llegan antes)")]
    public float swirlTravelHeight = 1.1f;
    [Tooltip("02 el cuerpo se condensa de abajo arriba siguiendo a la arena que aterriza: " +
             "momento en que el frente sale de los pies (luego sube a la velocidad del remolino)")]
    public float formDelay = 1.25f;
    [Tooltip("Retraso del color respecto a la estatua de arena")]
    public float colorLag = 0.3f;

    [Header("Solve · remolino")]
    public float swirlParticles = 28000f;
    public float swirlRadius = 0.55f;
    public float swirlTurns = 1.2f;
    [Range(0f, 1f)] public float swirlFreeChance = 0.3f;
    public float dustSize = 0.012f;

    [Header("Dissolve")]
    public float dissolveDuration = 4f;
    public float dustTail = 1.6f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public UnityEvent onSolved;
    public UnityEvent onDissolved;

    [Header("Forma del borde (igual en material y VFX)")]
    [Tooltip("Margen por encima/debajo del cuerpo para que 0 y 1 queden totalmente fuera")]
    public float heightPadding = 0.12f;
    public float noiseScale = 9f;
    public float noiseAmount = 0.06f;

    [Header("Particulas (por personaje y segundo)")]
    public float edgeGrains = 2200f;
    public float edgePuffs = 160f;
    public float glitter = 500f;
    [Tooltip("Granos por segundo de la silueta de polvo cuando todo el cuerpo es arena")]
    public float shellGrains = 16000f;
    [Tooltip("Grosor (m) de la franja del borde donde nace la arena")]
    public float edgeBand = 0.03f;
    public Vector3 wind = new Vector3(0.35f, 0.12f, 0.12f);
    public float turbulence = 0.35f;
    public float travel = 0.45f;
    public Color sandColor = new Color(0.98f, 0.74f, 0.38f, 1f);
    public Color sandColorDark = new Color(0.62f, 0.43f, 0.20f, 1f);
    [ColorUsage(true, true)] public Color glowColor = new Color(2.4f, 1.6f, 0.55f, 1f);
    public float grainSize = 0.012f;

    [Header("Referencias")]
    public List<RendererSlot> slots = new List<RendererSlot>();
    [Tooltip("Usa los materiales originales cuando esta completo (sin coste de clip en Quest)")]
    public bool swapMaterials = true;
    [Tooltip("Desactiva los renderers cuando es todo arena y ya no hay polvo")]
    public bool hideRenderersWhenGone = true;
    [Tooltip("Otros VFX del personaje (p.ej. el aura de Mneme): su float 'companionFloat' se escala con lo visible que esta el cuerpo (solo en Play)")]
    public VisualEffect[] companionEffects = new VisualEffect[0];
    public string companionFloat = "Intensity";
    float[] m_CompanionBase;

    enum Play { None, Solve, Dissolve }
    Play m_Play;
    float m_PlayTime;
    MaterialPropertyBlock m_Block;
    bool m_UsingDissolve;
    bool m_HasBlock;

    static readonly int HeightID = Shader.PropertyToID("_DissolveHeight");
    static readonly int NoiseScaleID = Shader.PropertyToID("_DissolveNoiseScale");
    static readonly int NoiseAmountID = Shader.PropertyToID("_DissolveNoiseAmount");
    static readonly int ColorOffsetID = Shader.PropertyToID("_DissolveColorOffset");
    static readonly int DirectionID = Shader.PropertyToID("_DissolveDirection");
    static readonly int VfxEdgeDirection = Shader.PropertyToID("Edge Direction");

    static readonly int VfxEdgeHeight = Shader.PropertyToID("Edge Height");
    static readonly int VfxNoiseScale = Shader.PropertyToID("Noise Scale");
    static readonly int VfxNoiseAmount = Shader.PropertyToID("Noise Amount");
    static readonly int VfxEdgeBand = Shader.PropertyToID("Edge Band");
    static readonly int VfxGather = Shader.PropertyToID("Gather");
    static readonly int VfxWind = Shader.PropertyToID("Wind");
    static readonly int VfxTurbulence = Shader.PropertyToID("Turbulence");
    static readonly int VfxTravel = Shader.PropertyToID("Travel");
    static readonly int VfxGrainRate = Shader.PropertyToID("Grain Rate");
    static readonly int VfxPuffRate = Shader.PropertyToID("Puff Rate");
    static readonly int VfxGlitterRate = Shader.PropertyToID("Glitter Rate");
    static readonly int VfxShellRate = Shader.PropertyToID("Shell Rate");
    static readonly int VfxSandColor = Shader.PropertyToID("Sand Color");
    static readonly int VfxSandColorDark = Shader.PropertyToID("Sand Color Dark");
    static readonly int VfxGlowColor = Shader.PropertyToID("Glow Color");
    static readonly int VfxGrainSize = Shader.PropertyToID("Grain Size");
    static readonly int VfxSwirlRate = Shader.PropertyToID("Swirl Rate");
    static readonly int VfxSwirlCenter = Shader.PropertyToID("Swirl Center");
    static readonly int VfxBodyHeight = Shader.PropertyToID("Body Height");
    static readonly int VfxSwirlRadius = Shader.PropertyToID("Swirl Radius");
    static readonly int VfxSwirlTravel = Shader.PropertyToID("Swirl Travel");
    static readonly int VfxSwirlTravelH = Shader.PropertyToID("Swirl Travel Height");
    static readonly int VfxSwirlTurns = Shader.PropertyToID("Swirl Turns");
    static readonly int VfxSwirlFree = Shader.PropertyToID("Swirl Free Chance");
    static readonly int VfxDustSize = Shader.PropertyToID("Dust Size");

    public bool IsPlaying => m_Play != Play.None;

    // ------------------------------------------------------------------ API
    [ContextMenu("Play Solve (aparecer)")]
    public void PlaySolve()
    {
        visibility = 0f;
        colorVisibility = 0f;
        swirl = 0f;
        dust = 0f;
        gather = 1f;
        m_Play = Play.Solve;
        m_PlayTime = 0f;
        Apply();
    }

    [ContextMenu("Play Dissolve (desaparecer)")]
    public void PlayDissolve()
    {
        bottomUp = false;
        visibility = 1f;
        colorVisibility = 1f;
        swirl = 0f;
        dust = 1f;
        gather = 0f;
        m_Play = Play.Dissolve;
        m_PlayTime = 0f;
        Apply();
    }

    [ContextMenu("Show (completo)")]
    public void Show() { Stop(); bottomUp = false; visibility = 1f; colorVisibility = 1f; dust = 0f; swirl = 0f; Apply(); }

    [ContextMenu("Hide (oculto)")]
    public void Hide() { Stop(); bottomUp = false; visibility = 0f; colorVisibility = 0f; dust = 0f; swirl = 0f; gather = 0f; Apply(); }

    public void Stop() { m_Play = Play.None; }

    // ------------------------------------------------------------------ Loop
    static readonly int VfxSkinnedMesh = Shader.PropertyToID("Skinned Mesh");
    static readonly int VfxTriangleLut = Shader.PropertyToID("Triangle LUT");

    void OnEnable()
    {
        foreach (var s in slots)
            BindVfx(s);
        Apply();
    }

    static void BindVfx(RendererSlot s)
    {
        if (s.vfx == null) return;
        // Solo si cambia: en edit mode cada Set deja un override en la escena
        if (s.renderer is SkinnedMeshRenderer smr && s.vfx.HasSkinnedMeshRenderer(VfxSkinnedMesh)
            && s.vfx.GetSkinnedMeshRenderer(VfxSkinnedMesh) != smr)
            s.vfx.SetSkinnedMeshRenderer(VfxSkinnedMesh, smr);
        if (s.triangleLut != null && s.vfx.HasTexture(VfxTriangleLut)
            && s.vfx.GetTexture(VfxTriangleLut) != s.triangleLut)
            s.vfx.SetTexture(VfxTriangleLut, s.triangleLut);
    }

    void OnDisable()
    {
        // Deja el personaje como estaba en la escena
        UpdateCompanions(1f);
        SetDissolveMaterials(false);
        ClearBlocks();
        foreach (var s in slots)
            if (s.renderer != null) s.renderer.enabled = true;
    }

    void Update()
    {
        if (m_Play != Play.None)
            Tick(Application.isPlaying ? Time.deltaTime : 1f / 60f);
        Apply();
    }

    void Tick(float dt)
    {
        m_PlayTime += dt;
        if (m_Play == Play.Solve)
        {
            float duration = SolveLength;
            EvaluateSolve(m_PlayTime, duration);
            if (m_PlayTime >= duration)
            {
                m_Play = Play.None;
                onSolved?.Invoke();
            }
        }
        else if (m_Play == Play.Dissolve)
        {
            float duration = DissolveLength;
            EvaluateDissolve(m_PlayTime, duration);
            if (m_PlayTime >= duration)
            {
                m_Play = Play.None;
                onDissolved?.Invoke();
            }
        }
    }

    public float SolveLength => solveLength;
    public float DissolveLength => dissolveDuration + dustTail;

    float m_SolveScale = 1f;
    [NonSerialized] public bool bottomUp;

    // 01 la arena sube del suelo en remolino -> 02 el propio remolino forma el cuerpo: el frente de condensacion
    // sube de los pies a la cabeza al mismo ritmo que aterriza la arena (estatua de arena y el color justo detras)
    // -> 03 presencia completa. 'duration' = duracion del clip (escala todos los tiempos).
    public void EvaluateSolve(float t, float duration)
    {
        m_SolveScale = Mathf.Max(duration, 0.5f) / Mathf.Max(solveLength, 0.1f);
        float ts = t / m_SolveScale;
        swirl = ts < swirlEmitTime ? 1f : 0f;
        bottomUp = true;
        dust = 0f;
        gather = 1f;
        if (ts >= solveLength)
        {
            visibility = 1f;
            colorVisibility = 1f;
            return;
        }
        GetHeightRange(out float bottom, out float top);
        float bodyH = Mathf.Max(top - bottom, 0.1f);
        float pad = heightPadding + noiseAmount;
        float lo = bottom - pad, hi = top + pad;
        // Altura (normalizada) donde la arena ya ha aterrizado: misma ley que el tiempo de vuelo del VFX
        float hForm = (ts - formDelay) / Mathf.Max(swirlTravelHeight, 0.05f);
        float hColor = (ts - formDelay - colorLag) / Mathf.Max(swirlTravelHeight, 0.05f);
        visibility = Mathf.Clamp01((bottom + hForm * bodyH - lo) / (hi - lo));
        colorVisibility = Mathf.Clamp01((bottom + hColor * bodyH - lo) / (hi - lo));
    }

    // 04 empieza a desvanecerse -> 05 el cuerpo se vuelve polvo -> 06 solo queda el recuerdo
    public void EvaluateDissolve(float t, float duration)
    {
        float tailLen = duration * dustTail / Mathf.Max(DissolveLength, 1e-3f);
        float body = Mathf.Max(duration - tailLen, 1e-3f);
        float v = Mathf.Clamp01(t / body);
        visibility = v >= 1f ? 0f : 1f - curve.Evaluate(v);
        colorVisibility = visibility;
        dust = 1f - Mathf.Clamp01((t - body) / Mathf.Max(tailLen, 1e-3f));
        gather = 0f;
        swirl = 0f;
        bottomUp = false;
    }

    // ------------------------------------------------------------------ Timeline (SandDissolveTrack)
    bool m_TimelineDriven;

    public void SetTimelineState(float newVisibility, float newDust, float newGather)
    {
        m_Play = Play.None;
        m_TimelineDriven = true;
        visibility = newVisibility;
        colorVisibility = newVisibility;
        bottomUp = false;
        dust = newDust;
        gather = newGather;
        swirl = 0f;
        Apply();
    }

    // Tras EvaluateSolve/EvaluateDissolve desde el mixer del Timeline
    public void ApplyFromTimeline()
    {
        m_Play = Play.None;
        m_TimelineDriven = true;
        Apply();
    }

    public void ReleaseTimeline()
    {
        if (!m_TimelineDriven) return;
        m_TimelineDriven = false;
        // Fuera de Play (preview del Timeline en el editor) se deja el personaje como en la escena
        if (!Application.isPlaying)
            Show();
    }

    // Rango vertical del cuerpo en mundo (bounds de los renderers, siguen la animacion)
    bool GetHeightRange(out float bottom, out float top)
    {
        bottom = float.MaxValue;
        top = float.MinValue;
        foreach (var s in slots)
        {
            // Solo el cuerpo (skinned): los props (p.ej. el tazon de Amitis) pueden estar en otro sitio
            if (!(s.renderer is SkinnedMeshRenderer)) continue;
            var b = s.renderer.bounds;
            bottom = Mathf.Min(bottom, b.min.y);
            top = Mathf.Max(top, b.max.y);
        }
        if (bottom > top) { bottom = transform.position.y; top = bottom + 1.8f; return false; }
        return true;
    }

    // Centro del remolino: centro horizontal del cuerpo a la altura de los pies
    Vector3 GetCenter(float bottom)
    {
        bool any = false;
        Bounds b = default;
        foreach (var s in slots)
        {
            if (!(s.renderer is SkinnedMeshRenderer)) continue;
            if (!any) { b = s.renderer.bounds; any = true; }
            else b.Encapsulate(s.renderer.bounds);
        }
        var c = any ? b.center : transform.position;
        return new Vector3(c.x, bottom, c.z);
    }

    public void Apply()
    {
        GetHeightRange(out float bottom, out float top);
        float pad = heightPadding + noiseAmount;
        float lo = bottom - pad;
        float hi = top + pad;
        // Normal: visible por encima del borde (el dissolve sube y se lleva los pies primero).
        // bottomUp (solve): visible por debajo; el borde sube de los pies a la cabeza.
        float dir = bottomUp ? -1f : 1f;
        float edge = bottomUp ? Mathf.Lerp(lo, hi, visibility) : Mathf.Lerp(hi, lo, visibility);
        float bodyHeight = Mathf.Max(top - bottom, 0.1f);
        Vector3 swirlCenter = GetCenter(bottom);

        // Solve: el color vuelve por detras del borde; entre ambos el cuerpo es estatua de arena
        float colorVis = Mathf.Min(colorVisibility, visibility);
        float colorEdge = bottomUp ? Mathf.Lerp(lo, hi, colorVis) : Mathf.Lerp(hi, lo, colorVis);
        float colorOffset = Mathf.Max(0f, (colorEdge - edge) * dir);

        bool complete = visibility >= 0.9999f && colorVisibility >= 0.9999f && dust <= 0.0001f && swirl <= 0.0001f;
        // Durante el solve (gather) los renderers siguen activos aunque esten recortados del todo: el VFX
        // muestrea su skinned mesh y un SkinnedMeshRenderer desactivado deja de actualizar la pose.
        bool gone = visibility <= 0.0001f && dust <= 0.0001f && gather < 0.5f;
        bool edgeInBody = edge > bottom - noiseAmount && edge < top + noiseAmount;

        // Materiales
        SetDissolveMaterials(!complete || !swapMaterials);
        if (complete && swapMaterials)
            ClearBlocks();
        else
            SetBlocks(edge, colorOffset, dir);

        foreach (var s in slots)
            if (s.renderer != null)
                s.renderer.enabled = !(hideRenderersWhenGone && gone);

        UpdateCompanions(complete ? 1f : Mathf.Min(visibility, colorVisibility));

        // VFX. En la preview del Timeline en el editor no se toca (no simula y ensuciaria la escena)
        if (m_TimelineDriven && !Application.isPlaying)
            return;
        float bandScale = Mathf.Clamp(bodyHeight / Mathf.Max(edgeBand * 1.3f, 0.005f), 1f, 80f);
        float edgeOn = edgeInBody ? 1f : 0f;
        float turb = turbulence * (1f - gather);
        foreach (var s in slots)
        {
            var vfx = s.vfx;
            if (vfx == null || vfx.visualEffectAsset == null) continue;
            float w = s.areaWeight;
            bool active = edgeOn > 0f || dust > 0.0001f || swirl > 0.0001f;
            if (!active)
            {
                // Quieto: no se escribe nada en el VFX (en edit mode cada Set ensucia la escena).
                // Solo se cortan las tasas una vez al terminar y se apaga cuando no queda nada vivo.
                if (s.live)
                {
                    SetF(vfx, VfxGrainRate, 0f);
                    SetF(vfx, VfxPuffRate, 0f);
                    SetF(vfx, VfxGlitterRate, 0f);
                    SetF(vfx, VfxShellRate, 0f);
                    SetF(vfx, VfxSwirlRate, 0f);
                    s.live = false;
                }
                if (Application.isPlaying && vfx.enabled && vfx.aliveParticleCount == 0)
                    vfx.enabled = false;
                continue;
            }
            s.live = true;
            if (!vfx.enabled)
            {
                vfx.enabled = true;
                BindVfx(s);
            }

            // El VFX muestrea el skinned mesh ya en mundo (ApplyWorldRootTransform) y sus sistemas son World:
            // su transform tiene que ser la identidad o la arena sale desplazada lo que se mueva el personaje.
            var vt = vfx.transform;
            if (vt.position != Vector3.zero || vt.rotation != Quaternion.identity)
                vt.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            SetF(vfx, VfxEdgeHeight, edge);
            SetF(vfx, VfxEdgeDirection, dir);
            SetF(vfx, VfxNoiseScale, noiseScale);
            SetF(vfx, VfxNoiseAmount, noiseAmount);
            SetF(vfx, VfxEdgeBand, edgeBand);
            SetF(vfx, VfxGather, gather);
            SetF(vfx, VfxTurbulence, turb);
            SetF(vfx, VfxTravel, travel);
            SetF(vfx, VfxGrainSize, grainSize);
            if (vfx.HasVector3(VfxWind)) vfx.SetVector3(VfxWind, wind);
            if (vfx.HasVector4(VfxSandColor)) vfx.SetVector4(VfxSandColor, sandColor);
            if (vfx.HasVector4(VfxSandColorDark)) vfx.SetVector4(VfxSandColorDark, sandColorDark);
            if (vfx.HasVector4(VfxGlowColor)) vfx.SetVector4(VfxGlowColor, glowColor);

            // Tasas de candidatos: el VFX descarta en Init los que no caen en la franja/zona,
            // asi que se escalan por la fraccion del cuerpo que sobrevive.
            // En el solve no hay granos convergiendo al borde (ruido): la arena llega con el remolino
            SetF(vfx, VfxGrainRate, edgeGrains * w * bandScale * edgeOn * (1f - gather));
            SetF(vfx, VfxPuffRate, edgePuffs * w * bandScale * edgeOn * (1f - gather));
            SetF(vfx, VfxGlitterRate, glitter * w * bandScale * edgeOn);
            SetF(vfx, VfxShellRate, shellGrains * w * dust);

            // Remolino del solve: todas las particulas se emiten en la primera fraccion del clip
            float emitTime = Mathf.Max(swirlEmitTime * m_SolveScale, 0.1f);
            SetF(vfx, VfxSwirlRate, swirlParticles * w * swirl / emitTime);
            SetF(vfx, VfxSwirlTravel, swirlTravel * m_SolveScale);
            SetF(vfx, VfxSwirlTravelH, swirlTravelHeight * m_SolveScale);
            SetF(vfx, VfxBodyHeight, bodyHeight);
            SetF(vfx, VfxSwirlRadius, swirlRadius);
            SetF(vfx, VfxSwirlTurns, swirlTurns);
            SetF(vfx, VfxSwirlFree, swirlFreeChance);
            SetF(vfx, VfxDustSize, dustSize);
            if (vfx.HasVector3(VfxSwirlCenter)) vfx.SetVector3(VfxSwirlCenter, swirlCenter);
        }
    }

    // Escala el float de los VFX acompanantes (se guarda su valor original la primera vez)
    void UpdateCompanions(float k)
    {
        if (!Application.isPlaying || companionEffects == null || companionEffects.Length == 0) return;
        if (m_CompanionBase == null || m_CompanionBase.Length != companionEffects.Length)
        {
            m_CompanionBase = new float[companionEffects.Length];
            for (int i = 0; i < companionEffects.Length; i++)
                m_CompanionBase[i] = companionEffects[i] != null && companionEffects[i].HasFloat(companionFloat) ? companionEffects[i].GetFloat(companionFloat) : 1f;
        }
        for (int i = 0; i < companionEffects.Length; i++)
        {
            var c = companionEffects[i];
            if (c == null || !c.HasFloat(companionFloat)) continue;
            float v = m_CompanionBase[i] * k;
            if (!Mathf.Approximately(c.GetFloat(companionFloat), v)) c.SetFloat(companionFloat, v);
        }
    }

    static void SetF(VisualEffect vfx, int id, float v)
    {
        if (vfx.HasFloat(id)) vfx.SetFloat(id, v);
    }

    void SetDissolveMaterials(bool on)
    {
        if (on == m_UsingDissolve && Application.isPlaying) return;
        m_UsingDissolve = on;
        foreach (var s in slots)
        {
            if (s.renderer == null) continue;
            var mats = on ? s.dissolveMaterials : s.originalMaterials;
            if (mats == null || mats.Length == 0) continue;
            if (!SameMaterials(s.renderer.sharedMaterials, mats))
                s.renderer.sharedMaterials = mats;
        }
    }

    static bool SameMaterials(Material[] a, Material[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    void SetBlocks(float edge, float colorOffset, float dir)
    {
        m_Block ??= new MaterialPropertyBlock();
        foreach (var s in slots)
        {
            if (s.renderer == null) continue;
            s.renderer.GetPropertyBlock(m_Block);
            m_Block.SetFloat(HeightID, edge);
            m_Block.SetFloat(NoiseScaleID, noiseScale);
            m_Block.SetFloat(NoiseAmountID, noiseAmount);
            m_Block.SetFloat(ColorOffsetID, colorOffset);
            m_Block.SetFloat(DirectionID, dir);
            s.renderer.SetPropertyBlock(m_Block);
        }
        m_HasBlock = true;
    }

    void ClearBlocks()
    {
        if (!m_HasBlock && Application.isPlaying) return;
        foreach (var s in slots)
            if (s.renderer != null) s.renderer.SetPropertyBlock(null);
        m_HasBlock = false;
    }
}
