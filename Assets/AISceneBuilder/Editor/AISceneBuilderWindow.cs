using UnityEditor;
using UnityEngine;

namespace AISceneBuilder
{
    /// <summary>
    /// AI Destekli Sahne Tasarım Aracı - Ana Editor Penceresi.
    /// Adım 1: Sadece görsel arayüz. API isteği henüz bağlı değil.
    /// </summary>
    public class AISceneBuilderWindow : EditorWindow
    {
        // API anahtarı EditorPrefs'te saklanır; koda gömülmez, versiyon kontrolüne girmez.
        private const string ApiKeyPrefKey = "AISceneBuilder_ApiKey";

        private string _apiKey = "";
        private string _userPrompt = "";
        private bool _showApiKey;      // Anahtarı göster/gizle
        private bool _isProcessing;    // Adım 4'te async istek sırasında UI'ı kilitlemek için
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.Info;
        private Vector2 _promptScroll;

        [MenuItem("Tools/AI Scene Builder")]
        public static void ShowWindow()
        {
            var window = GetWindow<AISceneBuilderWindow>("AI Scene Builder");
            window.minSize = new Vector2(400f, 320f);
        }

        private void OnEnable()
        {
            _apiKey = EditorPrefs.GetString(ApiKeyPrefKey, "");
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(8f);
            DrawApiKeySection();
            EditorGUILayout.Space(8f);
            DrawPromptSection();
            EditorGUILayout.Space(8f);
            DrawGenerateButton();
            DrawStatusBar();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(4f);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("AI Scene Builder", titleStyle);
            EditorGUILayout.LabelField(
                "Metin komutuyla sahneye prefab yerleştirin.",
                EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawApiKeySection()
        {
            EditorGUILayout.LabelField("API Ayarları", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();

                string newKey = _showApiKey
                    ? EditorGUILayout.TextField("Claude API Anahtarı", _apiKey)
                    : EditorGUILayout.PasswordField("Claude API Anahtarı", _apiKey);

                if (EditorGUI.EndChangeCheck())
                {
                    _apiKey = newKey.Trim();
                    EditorPrefs.SetString(ApiKeyPrefKey, _apiKey);
                }

                _showApiKey = GUILayout.Toggle(_showApiKey, "Göster",
                    EditorStyles.miniButton, GUILayout.Width(55f));
            }

            if (string.IsNullOrEmpty(_apiKey))
            {
                EditorGUILayout.HelpBox(
                    "API anahtarı girilmedi. Anahtar EditorPrefs'te yerel olarak saklanır, projeyle paylaşılmaz.",
                    MessageType.Warning);
            }
        }

        private void DrawPromptSection()
        {
            EditorGUILayout.LabelField("Sahne Komutu", EditorStyles.boldLabel);

            _promptScroll = EditorGUILayout.BeginScrollView(_promptScroll, GUILayout.Height(100f));
            _userPrompt = EditorGUILayout.TextArea(_userPrompt, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField(
                "Örnek: \"Merkeze bir kulübe, etrafına 5 ağaç ve girişe 2 fener yerleştir.\"",
                EditorStyles.miniLabel);
        }

        private void DrawGenerateButton()
        {
            bool canGenerate = !_isProcessing
                               && !string.IsNullOrEmpty(_apiKey)
                               && !string.IsNullOrWhiteSpace(_userPrompt);

            using (new EditorGUI.DisabledScope(!canGenerate))
            {
                if (GUILayout.Button(_isProcessing ? "Oluşturuluyor..." : "Sahneyi Oluştur",
                        GUILayout.Height(32f)))
                {
                    OnGenerateClicked();
                }
            }
        }

        private void OnGenerateClicked()
        {
            // Adım 4'te burası async API çağrısına bağlanacak.
            _statusMessage = "Arayüz hazır. API entegrasyonu bir sonraki adımda eklenecek.";
            _statusType = MessageType.Info;
            Debug.Log($"[AI Scene Builder] Komut alındı: {_userPrompt}");
        }

        private void DrawStatusBar()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }
    }
}
