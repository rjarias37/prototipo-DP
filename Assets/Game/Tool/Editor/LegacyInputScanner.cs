#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class LegacyInputScanner
{
    private static readonly string[] Patterns = {
        "Input.GetKeyDown(", "Input.GetKey(", "Input.GetKeyUp(",
        "Input.GetAxis(", "Input.GetAxisRaw(", "Input.anyKey"
    };

    [MenuItem("Tools/Input/Scan Project for Old Input API")]
    public static void Scan()
    {
        string[] files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        int hits = 0;

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (var p in Patterns)
            {
                if (text.Contains(p))
                {
                    hits++;
                    Debug.LogWarning($"[LegacyInput] {p} encontrado en: {file}");
                }
            }
        }

        if (hits == 0) Debug.Log("[LegacyInput] No se encontraron usos del Input antiguo.");
        else Debug.LogWarning($"[LegacyInput] Total coincidencias: {hits}. Reemplaza por el nuevo Input System.");
    }
}
#endif
