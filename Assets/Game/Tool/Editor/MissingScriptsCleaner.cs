// Minimalista: remueve componentes faltantes de los GameObjects seleccionados o de toda la escena.
// Unity 6.x compatible. Editor only.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class MissingScriptsCleaner
{
    [MenuItem("Tools/Missing Scripts/Remove From Selection")]
    private static void RemoveFromSelection()
    {
        var selection = Selection.gameObjects;
        if (selection == null || selection.Length == 0)
        {
            Debug.LogWarning("No hay selección. Se limpiará toda la escena.");
            selection = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        }

        int totalRemoved = 0;
        foreach (var go in selection)
        {
            // Recorre jerarquía completa del objeto
            var transforms = go.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                // Elimina todos los componentes Missing en este GameObject
                totalRemoved += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
        }
        Debug.Log($"MissingScriptsCleaner: Componentes faltantes eliminados: {totalRemoved}");
    }
}
#endif
