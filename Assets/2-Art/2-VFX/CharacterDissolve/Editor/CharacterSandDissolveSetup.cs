using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

// Monta el disolve de arena en un personaje (o enemigo) de la escena:
//  - Read/Write en las mallas skinned (lo necesitan la LUT y el muestreo del VFX)
//  - LUT de triangulos ponderada por area por cada SkinnedMeshRenderer
//  - materiales <original>_SandDissolve con el shader "VZ/Character Sand Dissolve"
//  - un VFX_Sand_CharacterDissolve por SkinnedMeshRenderer + CharacterSandDissolve en la raiz
public static class CharacterSandDissolveSetup
{
    const string ShaderName = "VZ/Character Sand Dissolve";
    const string VfxPath = "Assets/2-Art/2-VFX/CharacterDissolve/VFX/VFX_Sand_CharacterDissolve.vfx";
    const string LutFolder = "Assets/2-Art/2-VFX/CharacterDissolve/Textures";
    const string FallbackMatFolder = "Assets/2-Art/2-VFX/CharacterDissolve/Materials";
    // Mallas mas pequenas que esto (pendientes, collares...) solo se recortan con el shader, sin VFX propio
    const float MinVfxArea = 0.03f;

    [MenuItem("Tools/VFX/Setup Character Sand Dissolve (seleccion)")]
    static void SetupSelected()
    {
        foreach (var go in Selection.gameObjects)
            Debug.Log(Setup(go), go);
    }

    public static string Setup(GameObject root)
    {
        var shader = Shader.Find(ShaderName);
        var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(VfxPath);
        if (shader == null || vfxAsset == null)
            return "Falta el shader o el VFX";

        // Read/Write en los modelos de las mallas skinned. Reimportar recrea los objetos de las instancias
        // (y los prefabs que anidan el modelo), asi que se hace aparte y se pide volver a ejecutar.
        var modelPaths = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Select(smr => AssetDatabase.GetAssetPath(smr.sharedMesh)).Distinct().ToList();
        var notReadable = modelPaths
            .Select(p => AssetImporter.GetAtPath(p) as ModelImporter)
            .Where(imp => imp != null && !imp.isReadable).ToList();
        if (notReadable.Count > 0)
        {
            foreach (var imp in notReadable)
            {
                imp.isReadable = true;
                imp.SaveAndReimport();
            }
            return $"[SandDissolve] {root.name}: activado Read/Write en {string.Join(", ", notReadable.Select(i => i.assetPath))}. Vuelve a ejecutar el setup.";
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(r => (r is SkinnedMeshRenderer || r is MeshRenderer) && !(r is UnityEngine.VFX.VFXRenderer))
            .ToList();

        var dissolve = root.GetComponent<CharacterSandDissolve>();
        if (dissolve == null) dissolve = Undo.AddComponent<CharacterSandDissolve>(root);
        Undo.RecordObject(dissolve, "Setup Sand Dissolve");

        // Si ya estaba montado, recuperar los originales (el renderer puede tener puestos los de disolve)
        var previous = dissolve.slots.Where(s => s.renderer != null).ToDictionary(s => s.renderer, s => s);
        dissolve.slots.Clear();

        var areas = new Dictionary<Renderer, float>();
        foreach (var r in renderers)
            areas[r] = MeshArea(r);
        float total = Mathf.Max(areas.Values.Sum(), 1e-6f);

        var log = new System.Text.StringBuilder();
        foreach (var r in renderers)
        {
            var originals = previous.TryGetValue(r, out var prev) && prev.originalMaterials != null && prev.originalMaterials.Length > 0
                ? prev.originalMaterials
                : r.sharedMaterials;
            var slot = new CharacterSandDissolve.RendererSlot
            {
                renderer = r,
                originalMaterials = originals,
                dissolveMaterials = originals.Select(m => DissolveMaterial(m, shader, root.name + "_" + r.name)).ToArray(),
                areaWeight = areas[r] / total,
            };

            if (r is SkinnedMeshRenderer smr && slot.areaWeight >= MinVfxArea)
            {
                Directory.CreateDirectory(LutFolder);
                var lutPath = $"{LutFolder}/T_{root.name}_{smr.name}_TriLUT.asset";
                slot.triangleLut = SkinnedTriangleLUTBaker.Bake(smr.sharedMesh, lutPath);
                slot.vfx = EnsureVfx(root.transform, smr, vfxAsset);
                // Serializado en el prefab: asi el componente no tiene que escribirlo en runtime/edit mode
                slot.vfx.SetSkinnedMeshRenderer("Skinned Mesh", smr);
                slot.vfx.SetTexture("Triangle LUT", slot.triangleLut);
            }
            dissolve.slots.Add(slot);
            log.Append($"{r.name} (area {slot.areaWeight:P0}) ");
        }

        // Propiedades del borde: las mismas en el material y en el VFX
        var anyMat = dissolve.slots.SelectMany(s => s.dissolveMaterials).FirstOrDefault(m => m != null);
        if (anyMat != null)
        {
            dissolve.noiseScale = anyMat.GetFloat("_DissolveNoiseScale");
            dissolve.noiseAmount = anyMat.GetFloat("_DissolveNoiseAmount");
        }

        // Otros VFX del personaje (aura de Mneme...): se atenuan mientras el cuerpo no es visible
        var ownVfx = new HashSet<VisualEffect>(dissolve.slots.Where(s => s.vfx != null).Select(s => s.vfx));
        dissolve.companionEffects = root.GetComponentsInChildren<VisualEffect>(true)
            .Where(v => !ownVfx.Contains(v) && v.visualEffectAsset != vfxAsset).ToArray();
        foreach (var c in dissolve.companionEffects) log.Append($"[acompanante {c.name}] ");

        EditorUtility.SetDirty(dissolve);
        AssetDatabase.SaveAssets();
        return $"[SandDissolve] {root.name}: {log}";
    }

    static Material DissolveMaterial(Material original, Shader shader, string owner)
    {
        if (original == null) return null;
        if (original.shader == shader) return original;
        var srcPath = AssetDatabase.GetAssetPath(original);
        string folder = srcPath.EndsWith(".mat") ? Path.GetDirectoryName(srcPath).Replace('\\', '/') : FallbackMatFolder;
        string baseName = srcPath.EndsWith(".mat") ? original.name : owner;
        if (string.IsNullOrEmpty(baseName.Trim('_'))) baseName = owner + "_Prop";
        Directory.CreateDirectory(folder);
        var path = $"{folder}/{baseName}_SandDissolve.mat";

        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool create = mat == null;
        if (create) mat = new Material(original);
        else mat.CopyPropertiesFromMaterial(original);
        // Conserva texturas/valores (mismos nombres que el Lit) y las keywords del original
        mat.shader = shader;
        mat.shaderKeywords = original.shaderKeywords;
        mat.renderQueue = original.renderQueue;
        mat.name = Path.GetFileNameWithoutExtension(path);
        if (create) AssetDatabase.CreateAsset(mat, path);
        else EditorUtility.SetDirty(mat);
        return mat;
    }

    static VisualEffect EnsureVfx(Transform root, SkinnedMeshRenderer smr, VisualEffectAsset asset)
    {
        string name = "VFX_SandDissolve_" + smr.name;
        var t = root.Find(name);
        if (t == null)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Sand Dissolve VFX");
            go.transform.SetParent(root, false);
            t = go.transform;
        }
        var vfx = t.GetComponent<VisualEffect>();
        if (vfx == null) vfx = Undo.AddComponent<VisualEffect>(t.gameObject);
        vfx.visualEffectAsset = asset;
        // Sistemas en World + muestreo con ApplyWorldRootTransform: el contenedor va en el origen del mundo
        t.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        return vfx;
    }

    static float MeshArea(Renderer r)
    {
        Mesh mesh = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
        if (mesh == null || !mesh.isReadable) return 0.01f;
        var v = mesh.vertices;
        var tris = mesh.triangles;
        var scale = r.transform.lossyScale;
        double a = 0;
        for (int i = 0; i < tris.Length; i += 3)
        {
            var p0 = Vector3.Scale(v[tris[i]], scale);
            var p1 = Vector3.Scale(v[tris[i + 1]], scale);
            var p2 = Vector3.Scale(v[tris[i + 2]], scale);
            a += Vector3.Cross(p1 - p0, p2 - p0).magnitude * 0.5;
        }
        return (float)a;
    }
}
