using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AISceneBuilder
{
    /// <summary>
    /// Google Gemini API istemcisi (generateContent endpoint'i).
    /// responseMimeType=application/json sayesinde model, cevabı API seviyesinde
    /// JSON olarak döndürmeye zorlanır.
    /// </summary>
    public class GeminiProvider : ILlmProvider
    {
        private const string Endpoint =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        public string DisplayName => "Google Gemini";

        public async Task<string> RequestScenePlanAsync(string apiKey, string prefabNameList, string userPrompt)
        {
            string systemPrompt = LlmRequestUtility.BuildSystemPrompt(prefabNameList);

            string body =
                "{"
                + "\"system_instruction\":{\"parts\":[{\"text\":\"" + LlmRequestUtility.JsonEscape(systemPrompt) + "\"}]},"
                + "\"contents\":[{\"role\":\"user\",\"parts\":[{\"text\":\"" + LlmRequestUtility.JsonEscape(userPrompt) + "\"}]}],"
                + "\"generationConfig\":{\"responseMimeType\":\"application/json\"}"
                + "}";

            using (var request = new HttpRequestMessage(HttpMethod.Post, Endpoint))
            {
                // Anahtar URL parametresi yerine header'da taşınır; URL'ler log/proxy'lere düşebilir.
                request.Headers.Add("x-goog-api-key", apiKey);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await LlmRequestUtility.Http.SendAsync(request))
                {
                    string json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"Gemini API hatası ({(int)response.StatusCode}): {ExtractErrorMessage(json)}");

                    var parsed = JsonUtility.FromJson<GeminiResponse>(json);
                    string text = null;
                    if (parsed != null && parsed.candidates != null && parsed.candidates.Length > 0)
                    {
                        var content = parsed.candidates[0].content;
                        if (content != null && content.parts != null && content.parts.Length > 0)
                            text = content.parts[0].text;
                    }

                    if (string.IsNullOrEmpty(text))
                        throw new Exception("Gemini cevabında metin bulunamadı: " + LlmRequestUtility.Truncate(json));

                    return text;
                }
            }
        }

        private static string ExtractErrorMessage(string json)
        {
            try
            {
                var envelope = JsonUtility.FromJson<ErrorEnvelope>(json);
                if (envelope != null && envelope.error != null && !string.IsNullOrEmpty(envelope.error.message))
                    return envelope.error.message;
            }
            catch
            {
                // Hata gövdesi beklenen biçimde değilse ham metne düşülür.
            }
            return LlmRequestUtility.Truncate(json);
        }

        // --- Gemini cevap şeması (JsonUtility alan adları API ile birebir) ---

        [Serializable] private class GeminiResponse { public Candidate[] candidates; }
        [Serializable] private class Candidate { public Content content; }
        [Serializable] private class Content { public Part[] parts; }
        [Serializable] private class Part { public string text; }
        [Serializable] private class ErrorEnvelope { public ErrorBody error; }
        [Serializable] private class ErrorBody { public int code; public string message; public string status; }
    }
}
