using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AISceneBuilder
{
    public enum LlmProviderType
    {
        Gemini = 0,
        Claude = 1
    }

    /// <summary>
    /// Tüm AI sağlayıcılarının ortak sözleşmesi. Girdi her zaman aynıdır
    /// (anahtar + prefab listesi + kullanıcı komutu), çıktı ham model cevabıdır;
    /// JSON ayrıştırma sağlayıcıdan bağımsız olarak ScenePlanParser'da yapılır.
    /// </summary>
    public interface ILlmProvider
    {
        string DisplayName { get; }
        Task<string> RequestScenePlanAsync(string apiKey, string prefabNameList, string userPrompt);
    }

    public static class LlmProviderFactory
    {
        public static ILlmProvider Create(LlmProviderType type)
        {
            switch (type)
            {
                case LlmProviderType.Claude:
                    return new ClaudeProvider();
                default:
                    return new GeminiProvider();
            }
        }
    }

    /// <summary>Sağlayıcıların ortak kullandığı HTTP istemcisi ve prompt/JSON yardımcıları.</summary>
    internal static class LlmRequestUtility
    {
        // Tek HttpClient tüm istekler boyunca paylaşılır (her istekte yenisini
        // oluşturmak socket tükenmesine yol açar).
        internal static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(120)
        };

        /// <summary>
        /// Modele gönderilen sistem talimatı: yalnızca eldeki prefab'leri kullan,
        /// yalnızca beklenen şemada JSON döndür.
        /// </summary>
        internal static string BuildSystemPrompt(string prefabNameList)
        {
            var sb = new StringBuilder();
            sb.Append("You are a Unity scene layout planner. The user describes a scene in natural language (often Turkish). ");
            sb.Append("Plan where to place prefabs in the scene.\n\n");
            sb.Append("Available prefabs (use these names EXACTLY as written, and no others): ");
            sb.Append(prefabNameList);
            sb.Append("\n\nRespond with ONLY a raw JSON object - no markdown fences, no explanation. Schema:\n");
            sb.Append("{\"Yerlesimler\":[{\"PrefabAdi\":\"Name\",\"PositionX\":0.0,\"PositionY\":0.0,\"PositionZ\":0.0,\"RotationY\":0.0}]}\n\n");
            sb.Append("Rules:\n");
            sb.Append("- PositionY is ground height; keep it 0 unless the user asks otherwise.\n");
            sb.Append("- Spread objects in a sensible, non-overlapping layout around the origin.\n");
            sb.Append("- RotationY is in degrees (0-360).\n");
            sb.Append("- If the user asks for N copies of something, output N entries.");
            return sb.ToString();
        }

        /// <summary>Bir metni JSON string değeri içine güvenle gömülecek şekilde kaçışlar.</summary>
        internal static string JsonEscape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var sb = new StringBuilder(text.Length + 16);
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>Sık görülen HTTP hata kodları için kullanıcıya yol gösteren Türkçe ipucu.</summary>
        internal static string HttpHint(int statusCode)
        {
            switch (statusCode)
            {
                case 400:
                case 401:
                case 403:
                    return " İpucu: API anahtarınızı kontrol edin (yanlış, süresi dolmuş veya yetkisiz olabilir).";
                case 404:
                    return " İpucu: Model adı artık geçerli olmayabilir — eklentiyi güncelleyin.";
                case 429:
                    return " İpucu: İstek/kota limiti aşıldı. Biraz bekleyip tekrar deneyin.";
                case 500:
                case 502:
                case 503:
                case 529:
                    return " İpucu: Sağlayıcı tarafında geçici bir sorun var, tekrar deneyin.";
                default:
                    return "";
            }
        }

        internal static string Truncate(string text, int maxLength = 300)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }
    }
}
