using System;
using System.Collections.Generic;
using UnityEngine;

namespace AISceneBuilder
{
    /// <summary>
    /// AI'nın döndürdüğü tek bir yerleştirme kaydı.
    /// Alan adları API'den dönen JSON anahtarlarıyla birebir aynı olmalıdır
    /// (JsonUtility isimle eşleştirme yapar, yeniden adlandırma desteklemez).
    /// </summary>
    [Serializable]
    public class PlacementData
    {
        public string PrefabAdi;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public float RotationY;

        public Vector3 GetPosition() => new Vector3(PositionX, PositionY, PositionZ);
        public Quaternion GetRotation() => Quaternion.Euler(0f, RotationY, 0f);
    }

    /// <summary>
    /// AI'dan beklenen kök JSON nesnesi.
    /// JsonUtility kök seviyede dizi ayrıştıramadığı için liste bir nesneyle sarılır:
    /// { "Yerlesimler": [ { "PrefabAdi": "...", ... } ] }
    /// </summary>
    [Serializable]
    public class ScenePlan
    {
        public PlacementData[] Yerlesimler;
    }

    /// <summary>
    /// AI cevabını güvenli şekilde ScenePlan'a dönüştürür.
    /// Modeller bazen JSON'u markdown kod bloğu içinde veya açıklama metniyle
    /// birlikte döndürür; bu sınıf JSON gövdesini ayıklayıp doğrular.
    /// </summary>
    public static class ScenePlanParser
    {
        /// <summary>
        /// Ham API cevabını ayrıştırır. Başarısızlıkta false döner ve error doldurulur;
        /// başarıda plan yalnızca geçerli kayıtları (PrefabAdi dolu) içerir.
        /// </summary>
        public static bool TryParse(string rawResponse, out ScenePlan plan, out string error)
        {
            plan = null;
            error = null;

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                error = "API'den boş cevap döndü.";
                return false;
            }

            string json = ExtractJsonObject(rawResponse);
            if (json == null)
            {
                error = "Cevabın içinde JSON nesnesi bulunamadı:\n" + Truncate(rawResponse, 200);
                return false;
            }

            ScenePlan parsed;
            try
            {
                parsed = JsonUtility.FromJson<ScenePlan>(json);
            }
            catch (Exception e)
            {
                error = "JSON ayrıştırılamadı: " + e.Message + "\n" + Truncate(json, 200);
                return false;
            }

            if (parsed?.Yerlesimler == null || parsed.Yerlesimler.Length == 0)
            {
                error = "JSON geçerli ama 'Yerlesimler' listesi boş veya eksik.";
                return false;
            }

            var validItems = new List<PlacementData>(parsed.Yerlesimler.Length);
            foreach (var item in parsed.Yerlesimler)
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.PrefabAdi))
                    validItems.Add(item);
            }

            if (validItems.Count == 0)
            {
                error = "Listedeki hiçbir kayıtta geçerli bir PrefabAdi yok.";
                return false;
            }

            parsed.Yerlesimler = validItems.ToArray();
            plan = parsed;
            return true;
        }

        /// <summary>
        /// Metnin içindeki ilk '{' ile son '}' arasını alır; böylece markdown
        /// kod blokları (```json ... ```) ve JSON öncesi/sonrası açıklamalar elenir.
        /// </summary>
        private static string ExtractJsonObject(string text)
        {
            int start = text.IndexOf('{');
            int end = text.LastIndexOf('}');

            if (start < 0 || end <= start)
                return null;

            return text.Substring(start, end - start + 1);
        }

        private static string Truncate(string text, int maxLength)
        {
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }
    }
}
