using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways]
public class DissolveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private VisualEffect targetVFX;

    [Header("Shared dissolve value")]
    [SerializeField] private float dissolve;

    private static readonly int ShaderDissolveID =
        Shader.PropertyToID("_Dissolve");

    private static readonly int VFXDissolveID =
        Shader.PropertyToID("Dissolve");

    private MaterialPropertyBlock propertyBlock;
    private float lastAppliedValue = float.NaN;

    public float Dissolve
    {
        get => dissolve;
        set
        {
            dissolve = Mathf.Clamp(value, -0.25f, 1f);
            ApplyDissolve();
        }
    }

    private void OnEnable()
    {
        ApplyDissolve();
    }

    private void OnValidate()
    {
        ApplyDissolve();
    }

    private void Update()
    {
        // Permite animarlo mediante Animator o Timeline.
        if (!Mathf.Approximately(lastAppliedValue, dissolve))
            ApplyDissolve();
    }

    public void SetDissolve(float value)
    {
        Dissolve = value;
    }

    private void ApplyDissolve()
    {
        if (targetRenderer != null)
        {
            propertyBlock ??= new MaterialPropertyBlock();

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(ShaderDissolveID, dissolve);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        if (targetVFX != null)
        {
            targetVFX.SetFloat(VFXDissolveID, dissolve);
        }

        lastAppliedValue = dissolve;
    }
}