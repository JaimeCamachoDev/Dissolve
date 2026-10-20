using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentDissolveController : MonoBehaviour
{
    [Header("Transition")]
    [SerializeField] private float hiddenValue = 1f;
    [SerializeField] private float visibleValue = 0f;
    [SerializeField] private float transitionDuration = 1f;

    [Header("Timing")]
    [SerializeField] private float delayBetweenObjects = 0.15f;

    [Header("Behaviour")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool includeInactiveObjects = true;

    private static readonly int TransitionID =
        Shader.PropertyToID("_Transition");

    private readonly List<Renderer> orderedRenderers =
        new List<Renderer>();

    private Coroutine revealCoroutine;

    private void Awake()
    {
        CollectRenderersInHierarchyOrder();
        SetAllTransitions(hiddenValue);
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayReveal();
        }
    }

    private void CollectRenderersInHierarchyOrder()
    {
        orderedRenderers.Clear();

        CollectRecursive(transform);

        Debug.Log(
            $"[{name}] Found {orderedRenderers.Count} renderers.",
            this
        );
    }

    private void CollectRecursive(Transform parent)
    {
        // Primero comprobamos el propio objeto.
        Renderer renderer = parent.GetComponent<Renderer>();

        if (renderer != null)
        {
            if (includeInactiveObjects || parent.gameObject.activeInHierarchy)
            {
                orderedRenderers.Add(renderer);
            }
        }

        // Después recorremos los hijos respetando
        // exactamente el orden del Hierarchy.
        for (int i = 0; i < parent.childCount; i++)
        {
            CollectRecursive(parent.GetChild(i));
        }
    }

    private void SetAllTransitions(float value)
    {
        foreach (Renderer renderer in orderedRenderers)
        {
            if (renderer == null)
                continue;

            Material[] materials = renderer.materials;

            foreach (Material material in materials)
            {
                if (material != null &&
                    material.HasProperty(TransitionID))
                {
                    material.SetFloat(TransitionID, value);
                }
            }
        }
    }

    public void PlayReveal()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
        }

        revealCoroutine = StartCoroutine(RevealRoutine());
    }

    public void ResetEnvironment()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        SetAllTransitions(hiddenValue);
    }

    private IEnumerator RevealRoutine()
    {
        SetAllTransitions(hiddenValue);

        foreach (Renderer renderer in orderedRenderers)
        {
            if (renderer == null)
                continue;

            List<Material> validMaterials =
                GetTransitionMaterials(renderer);

            if (validMaterials.Count == 0)
                continue;

            // Todos los materiales del mismo Renderer
            // aparecen simultáneamente.
            foreach (Material material in validMaterials)
            {
                StartCoroutine(
                    AnimateTransition(
                        material,
                        hiddenValue,
                        visibleValue,
                        transitionDuration
                    )
                );
            }

            yield return new WaitForSeconds(delayBetweenObjects);
        }

        revealCoroutine = null;
    }

    private List<Material> GetTransitionMaterials(Renderer renderer)
    {
        List<Material> result = new List<Material>();

        Material[] materials = renderer.materials;

        foreach (Material material in materials)
        {
            if (material != null &&
                material.HasProperty(TransitionID))
            {
                result.Add(material);
            }
        }

        return result;
    }

    private IEnumerator AnimateTransition(
        Material material,
        float from,
        float to,
        float duration)
    {
        if (duration <= 0f)
        {
            material.SetFloat(TransitionID, to);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            float value = Mathf.Lerp(
                from,
                to,
                t
            );

            material.SetFloat(
                TransitionID,
                value
            );

            yield return null;
        }

        material.SetFloat(
            TransitionID,
            to
        );
    }
}