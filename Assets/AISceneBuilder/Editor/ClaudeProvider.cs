using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AISceneBuilder
{
    /// <summary>
    /// Anthropic Claude API istemcisi (Messages endpoint'i).
    /// JSON çıktısı sistem talimatıyla zorlanır; ScenePlanParser yine de
    /// olası markdown sarmalayıcılarını temizler.
    /// </summary>
    public class ClaudeProvider : ILlmProvider
    {
        private const string Endpoint = "https://api.anthropic.com/v1/messages";
        private const string ApiVersion = "2023-06-01";

        // Gerekirse "claude-haiku-4-5" gibi daha ekonomik bir modelle değiştirilebilir.
        private const string Model = "claude-opus-4-8";

        public string DisplayName => "Anthropic Claude";

        public async Task<string> RequestScenePlanAsync(string apiKey, string prefabNameList, string userPrompt)
        {
            string systemPrompt = LlmRequestUtility.BuildSystemPrompt(prefabNameList);

            string body =
                "{"
                + "\"model\":\"" + Model + "\","
                + "\"max_tokens\":8192,"
                + "\"system\":\"" + LlmRequestUtility.JsonEscape(systemPrompt) + "\","
                + "\"messages\":[{\"role\":\"user\",\"content\":\"" + LlmRequestUtility.JsonEscape(userPrompt) + "\"}]"
                + "}";

            using (var request = new HttpRequestMessage(HttpMethod.Post, Endpoint))
            {
                request.Headers.Add("x-api-key", apiKey);
                request.Headers.Add("anthropic-version", ApiVersion);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await LlmRequestUtility.Http.SendAsync(request))
                {
                    string json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        int status = (int)response.StatusCode;
                        throw new Exception(
                            $"Claude API hatası ({status}): {ExtractErrorMessage(json)}{LlmRequestUtility.HttpHint(status)}");
                    }

                    var parsed = JsonUtility.FromJson<ClaudeResponse>(json);
                    string text = null;
                    if (parsed != null && parsed.content != null)
                    {
                        foreach (var block in parsed.content)
                        {
                            if (block != null && block.type == "text" && !string.IsNullOrEmpty(block.text))
                            {
                                text = block.text;
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(text))
                        throw new Exception("Claude cevabında metin bulunamadı: " + LlmRequestUtility.Truncate(json));

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

        // --- Claude cevap şeması (JsonUtility alan adları API ile birebir) ---

        [Serializable] private class ClaudeResponse { public ContentBlock[] content; }
        [Serializable] private class ContentBlock { public string type; public string text; }
        [Serializable] private class ErrorEnvelope { public ErrorBody error; }
        [Serializable] private class ErrorBody { public string type; public string message; }
    }
}
