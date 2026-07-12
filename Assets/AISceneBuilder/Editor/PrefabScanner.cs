using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AISceneBuilder
{
    /// <summary>
    /// Projedeki tüm prefab'leri AssetDatabase üzerinden tarar.
    /// AI'ya "yalnızca bu prefab'leri kullanabilirsin" bağlamını vermek
    /// ve dönen isimlerden asset yüklemek için kullanılır.
    /// </summary>
    public static class PrefabScanner
    {
        public readonly struct PrefabInfo
        {
            public readonly string Name;
            public readonly string Path;

            public PrefabInfo(string name, string path)
            {
                Name = name;
                Path = path;
            }
        }

        /// <summary>
        /// Assets altındaki tüm prefab'leri isim ve yol bilgisiyle, ada göre sıralı döndürür.
        /// AI isimle seçim yapacağı için aynı isimden yalnızca ilki listeye alınır.
        /// </summary>
        public static List<PrefabInfo> ScanProject()
        {
            var results = new List<PrefabInfo>();
            var seenNames = new HashSet<string>();

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);

                if (seenNames.Add(name))
                {
                    results.Add(new PrefabInfo(name, path));
                }
                else
                {
                    Debug.LogWarning(
                        $"[AI Scene Builder] Aynı isimde birden fazla prefab var: '{name}' ({path}) listeye alınmadı.");
                }
            }

            results.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return results;
        }

        /// <summary>AI prompt'una eklenecek isim listesini üretir (örn. "House, Lantern, Tree").</summary>
        public static string BuildNameList(IReadOnlyList<PrefabInfo> prefabs)
        {
            var names = new string[prefabs.Count];
            for (int i = 0; i < prefabs.Count; i++)
                names[i] = prefabs[i].Name;
            return string.Join(", ", names);
        }

        /// <summary>İsimden prefab asset'ini yükler; bulunamazsa null döner.</summary>
        public static GameObject LoadPrefabByName(IReadOnlyList<PrefabInfo> prefabs, string prefabName)
        {
            foreach (var info in prefabs)
            {
                if (info.Name == prefabName)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(info.Path);
            }
            return null;
        }
    }
}
