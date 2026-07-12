using System;
using System.Collections.Generic;
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
        // API anahtarları EditorPrefs'te saklanır; koda gömülmez, versiyon kontrolüne girmez.
        // Her sağlayıcının anahtarı ayrı tutulur, sağlayıcı değişince ilgili anahtar yüklenir.
        private const string ProviderPrefKey = "AISceneBuilder_Provider";
        private string ApiKeyPrefKey => "AISceneBuilder_ApiKey_" + _providerType;

        private LlmProviderType _providerType = LlmProviderType.Gemini;
        private string _apiKey = "";
        private string _userPrompt = "";
        private bool _showApiKey;      // Anahtarı göster/gizle
        private bool _isProcessing;    // Adım 4'te async istek sırasında UI'ı kilitlemek için
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.Info;
        private Vector2 _promptScroll;

        private List<PrefabScanner.PrefabInfo> _prefabs = new List<PrefabScanner.PrefabInfo>();
        private bool _prefabFoldout;
        private Vector2 _prefabScroll;

        [MenuItem("Tools/AI Scene Builder")]
        public static void ShowWindow()
        {
            var window = GetWindow<AISceneBuilderWindow>("AI Scene Builder");
            window.minSize = new Vector2(400f, 320f);
        }

        private void OnEnable()
        {
            _providerType = (LlmProviderType)EditorPrefs.GetInt(ProviderPrefKey, (int)LlmProviderType.Gemini);
            _apiKey = EditorPrefs.GetString(ApiKeyPrefKey, "");
            _prefabs = PrefabScanner.ScanProject();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(8f);
            DrawApiKeySection();
            EditorGUILayout.Space(8f);
            DrawPrefabSection();
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

            EditorGUI.BeginChangeCheck();
            var newProvider = (LlmProviderType)EditorGUILayout.EnumPopup("AI Sağlayıcısı", _providerType);
            if (EditorGUI.EndChangeCheck())
            {
                _providerType = newProvider;
                EditorPrefs.SetInt(ProviderPrefKey, (int)_providerType);
                _apiKey = EditorPrefs.GetString(ApiKeyPrefKey, "");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();

                string newKey = _showApiKey
                    ? EditorGUILayout.TextField("API Anahtarı", _apiKey)
                    : EditorGUILayout.PasswordField("API Anahtarı", _apiKey);

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
                string hint = _providerType == LlmProviderType.Gemini
                    ? "Ücretsiz Gemini anahtarı: aistudio.google.com/apikey"
                    : "Claude anahtarı: platform.claude.com (ön ödemeli kredi gerekir)";
                EditorGUILayout.HelpBox(
                    $"API anahtarı girilmedi. {hint}\nAnahtar EditorPrefs'te yerel olarak saklanır, projeyle paylaşılmaz.",
                    MessageType.Warning);
            }
        }

        private void DrawPrefabSection()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _prefabFoldout = EditorGUILayout.Foldout(
                    _prefabFoldout,
                    $"Projedeki Prefab'ler ({_prefabs.Count})",
                    toggleOnLabelClick: true);

                if (GUILayout.Button("Yenile", EditorStyles.miniButton, GUILayout.Width(55f)))
                    _prefabs = PrefabScanner.ScanProject();
            }

            if (_prefabs.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Projede hiç prefab bulunamadı. AI'nın yerleştirebileceği objeler için Assets altına prefab ekleyin.",
                    MessageType.Warning);
                return;
            }

            if (_prefabFoldout)
            {
                _prefabScroll = EditorGUILayout.BeginScrollView(_prefabScroll, GUILayout.MaxHeight(120f));
                foreach (var prefab in _prefabs)
                    EditorGUILayout.LabelField(prefab.Name, EditorStyles.miniLabel);
                EditorGUILayout.EndScrollView();
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

        private async void OnGenerateClicked()
        {
            // Prefab listesi istek anında tazelenir; AI yalnızca gerçekten var olan
            // prefab isimlerini görmelidir.
            _prefabs = PrefabScanner.ScanProject();
            if (_prefabs.Count == 0)
            {
                SetStatus("Projede hiç prefab yok — önce Assets altına prefab ekleyin.", MessageType.Error);
                return;
            }

            ILlmProvider provider = LlmProviderFactory.Create(_providerType);

            _isProcessing = true;
            SetStatus($"{provider.DisplayName} yanıtı bekleniyor...", MessageType.Info);
            Repaint();

            try
            {
                string response = await provider.RequestScenePlanAsync(
                    _apiKey,
                    PrefabScanner.BuildNameList(_prefabs),
                    _userPrompt);

                if (ScenePlanParser.TryParse(response, out ScenePlan plan, out string error))
                {
                    SetStatus(
                        $"{plan.Yerlesimler.Length} yerleştirme planı alındı. " +
                        "(Sahneye yerleştirme Adım 5'te bağlanacak — plan Console'a yazıldı.)",
                        MessageType.Info);

                    foreach (var item in plan.Yerlesimler)
                        Debug.Log($"[AI Scene Builder] {item.PrefabAdi} → Pozisyon {item.GetPosition()}, Rotasyon Y={item.RotationY}");
                }
                else
                {
                    SetStatus("Ayrıştırma hatası: " + error, MessageType.Error);
                }
            }
            catch (Exception e)
            {
                SetStatus("İstek başarısız: " + e.Message, MessageType.Error);
                Debug.LogException(e);
            }
            finally
            {
                _isProcessing = false;
                Repaint();
            }
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
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
