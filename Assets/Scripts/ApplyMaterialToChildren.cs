// Assets/Editor/ApplyMaterialToChildren.cs
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ApplyMaterialToChildren
{
    [MenuItem("Tools/Materials/Apply To Selected Parents")]
    public static void ApplyToSelected()
    {
        var mat = Selection.objects.OfType<Material>().FirstOrDefault();
        if (mat == null)
        {
            EditorUtility.DisplayDialog("Material no encontrado",
                "Selecciona también un Material en el Project (Ctrl/Cmd-clic) junto con el/los GCParent.", "OK");
            return;
        }

        var parents = Selection.gameObjects;
        if (parents.Length == 0)
        {
            EditorUtility.DisplayDialog("Nada seleccionado",
                "Selecciona al menos un objeto padre (p. ej. GCParent).", "OK");
            return;
        }

        int count = 0;
        foreach (var parent in parents)
        {
            var mfs = parent.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in mfs)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (!mr || !mf.sharedMesh) continue;

                int subMeshCount = Mathf.Max(1, mf.sharedMesh.subMeshCount);
                var mats = Enumerable.Repeat(mat, subMeshCount).ToArray();
                mr.sharedMaterials = mats;
                count++;
            }
        }

        Debug.Log($"Material aplicado a {count} MeshRenderer(s).");
    }
}
