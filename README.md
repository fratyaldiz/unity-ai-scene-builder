# AI Scene Builder

Unity için AI destekli sahne tasarım aracı. Metin komutlarını (prompt) seçtiğiniz yapay zeka API'siyle (Gemini, Claude vb.) analiz eder, projedeki prefab'leri bulur ve Scene View'a mantıklı şekilde yerleştirir.

Tamamen bir **Editor aracıdır** — Play Mode'da çalışmaz.

## Özellikler (planlanan)

- `EditorWindow` tabanlı özel arayüz (API anahtarı + komut girişi)
- API anahtarı `EditorPrefs`'te saklanır, koda gömülmez
- Projedeki prefab'lerin otomatik taranması
- Seçilebilir AI sağlayıcısı (Gemini, Claude, ...) ve asenkron (async/await) HTTP isteği
- JSON formatında yerleştirme planı (`PrefabAdi`, `PositionX/Y/Z`, `RotationY`)
- `PrefabUtility.InstantiatePrefab` ile prefab bağlantısı korunarak üretim
- `Undo.RegisterCreatedObjectUndo` ile tam Undo/Redo desteği

## Yol Haritası

- [x] **Adım 1:** EditorWindow arayüzü (görsel UI, API isteği yok)
- [x] **Adım 2:** Prefab tarayıcı (`AssetDatabase` ile proje prefab listesi)
- [ ] **Adım 3:** JSON veri modelleri ve ayrıştırma
- [ ] **Adım 4:** AI API istemcisi (async/await, sağlayıcı soyutlaması: Gemini, Claude, ...)
- [ ] **Adım 5:** Sahne yerleştirici (InstantiatePrefab + Undo)
- [ ] **Adım 6:** Entegrasyon, hata yönetimi ve cila

## Kullanım

1. Projeyi Unity ile açın (dosyalar `Assets/AISceneBuilder/` altındadır).
2. Menüden **Tools → AI Scene Builder** penceresini açın.
3. AI sağlayıcınızın API anahtarını girin ve komutunuzu yazın.
