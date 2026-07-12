using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AISceneBuilder
{
    /// <summary>
    /// AI'dan gelen ScenePlan'ı Scene View'a döker.
    /// PrefabUtility.InstantiatePrefab ile prefab bağlantıları korunur,
    /// Undo kaydı sayesinde tüm yerleştirme tek Ctrl+Z ile geri alınabilir.
    /// </summary>
    public static class ScenePlacer
    {
        private const string UndoLabel = "AI Scene Builder Yerleştirmesi";

        /// <summary>
        /// Plandaki kayıtları sahneye yerleştirir ve yerleşen obje sayısını döndürür.
        /// Bulunamayan prefab'ler atlanır ve warnings listesine yazılır.
        /// </summary>
        public static int PlacePlan(
            ScenePlan plan,
            IReadOnlyList<PrefabScanner.PrefabInfo> prefabs,
            out List<string> warnings)
        {
            warnings = new List<string>();

            // Tüm yerleştirme tek Undo grubunda toplanır: bir Ctrl+Z hepsini geri alır.
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);

            // Objeler ortak bir kök altında toplanır; hiyerarşi dağılmaz.
            var root = new GameObject("AI Scene " + System.DateTime.Now.ToString("HH.mm.ss"));
            Undo.RegisterCreatedObjectUndo(root, UndoLabel);

            int placedCount = 0;
            foreach (PlacementData item in plan.Yerlesimler)
            {
                GameObject prefab = PrefabScanner.LoadPrefabByName(prefabs, item.PrefabAdi);
                if (prefab == null)
                {
                    warnings.Add($"'{item.PrefabAdi}' adında prefab bulunamadı, kayıt atlandı.");
                    continue;
                }

                // Instantiate yerine InstantiatePrefab: sahnedeki obje prefab'e bağlı kalır
                // (mavi ikon, prefab değişiklikleri sahneye yansır).
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.scene);
                Undo.RegisterCreatedObjectUndo(instance, UndoLabel);

                instance.transform.SetParent(root.transform, worldPositionStays: false);
                instance.transform.position = item.GetPosition();
                instance.transform.rotation = item.GetRotation();
                placedCount++;
            }

            Undo.CollapseUndoOperations(undoGroup);

            if (placedCount == 0)
            {
                // Hiçbir şey yerleşmediyse boş kök obje sahnede bırakılmaz.
                Undo.DestroyObjectImmediate(root);
                return 0;
            }

            // Yerleştirilen grup seçilir ve kamera ona odaklanır.
            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();

            EditorSceneManager.MarkSceneDirty(root.scene);
            return placedCount;
        }
    }
}
