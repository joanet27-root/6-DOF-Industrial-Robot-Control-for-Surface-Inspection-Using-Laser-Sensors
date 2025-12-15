using UnityEngine;
using UnityEditor;

public static class ResetRodamientos
{
    [MenuItem("Tools/Resetear Rodamientos a 0°")]
    private static void ResetearRotacionRodamientos()
    {
        string[] nombres = {
            "2_Rodamiento",
            "4_Rodamiento",
            "6_Rodamiento",
            "8_Rodamiento",
            "10_Rodamiento",
            "12_Rodamiento"
        };

        foreach (string nombre in nombres)
        {
            GameObject go = GameObject.Find(nombre);

            if (go != null)
            {
                Undo.RecordObject(go.transform, "Reset Rotación " + nombre);

                // ¡LOCAL, no global!
                go.transform.localRotation = Quaternion.identity;
                // o si prefieres euler:
                // go.transform.localEulerAngles = Vector3.zero;

                EditorUtility.SetDirty(go.transform);
            }
            else
            {
                Debug.LogWarning($"No se encontró el objeto '{nombre}' en la escena.");
            }
        }

        Debug.Log("Rotación LOCAL de los rodamientos puesta a (0,0,0).");
    }
}
