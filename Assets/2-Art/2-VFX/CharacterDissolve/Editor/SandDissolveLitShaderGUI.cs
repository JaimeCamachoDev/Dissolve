using System;
using UnityEditor;
using UnityEngine;

// Inspector de "VZ/Character Sand Dissolve": el del Lit de URP (interno, por reflexion) + las propiedades del disolve.
public class SandDissolveLitShaderGUI : ShaderGUI
{
    static readonly string[] DissolveProps =
    {
        "_DissolveHeight", "_DissolveDirection", "_DissolveNoiseScale", "_DissolveNoiseAmount",
        "_DissolveGrainScale", "_DissolveGrainAmount", "_DissolveSandWidth", "_DissolveEdgeWidth",
        "_DissolveSandColor", "_DissolveEdgeColor", "_DissolveColorOffset",
    };

    ShaderGUI m_Lit;
    static bool s_Foldout = true;

    ShaderGUI Lit
    {
        get
        {
            if (m_Lit == null)
            {
                var type = Type.GetType("UnityEditor.Rendering.Universal.ShaderGUI.LitShader, Unity.RenderPipelines.Universal.Editor");
                if (type != null)
                    m_Lit = (ShaderGUI)Activator.CreateInstance(type);
            }
            return m_Lit;
        }
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        s_Foldout = EditorGUILayout.BeginFoldoutHeaderGroup(s_Foldout, "Sand Dissolve");
        if (s_Foldout)
        {
            EditorGUILayout.HelpBox("Lo anima CharacterSandDissolve en el personaje (altura del borde en mundo).", MessageType.None);
            foreach (var name in DissolveProps)
            {
                var p = FindProperty(name, properties, false);
                if (p != null)
                    materialEditor.ShaderProperty(p, p.displayName);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();

        if (Lit != null)
            Lit.OnGUI(materialEditor, properties);
        else
            base.OnGUI(materialEditor, properties);
    }

    public override void ValidateMaterial(Material material)
    {
        if (Lit != null)
            Lit.ValidateMaterial(material);
    }
}
