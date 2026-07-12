# AI Scene Builder

Unity için AI destekli sahne tasarım aracı. Metin komutlarını (prompt) seçtiğiniz yapay zeka API'siyle (Gemini, Claude vb.) analiz eder, projedeki prefab'leri bulur ve Scene View'a mantıklı şekilde yerleştirir.

Tamamen bir **Editor aracıdır** — Play Mode'da çalışmaz.

## Özellikler

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
- [x] **Adım 3:** JSON veri modelleri ve ayrıştırma
- [x] **Adım 4:** AI API istemcisi (async/await, sağlayıcı soyutlaması: Gemini + Claude)
- [x] **Adım 5:** Sahne yerleştirici (InstantiatePrefab + Undo)
- [x] **Adım 6:** Entegrasyon, hata yönetimi ve cila
- [x] **Adım 7:** Paketleme ve dağıtım (UPM git URL desteği)

## Kurulum

**Package Manager ile (önerilen):** Unity'de **Window → Package Manager → + → Add package from git URL** deyin ve şunu yapıştırın:

```
https://github.com/fratyaldiz/unity-ai-scene-builder.git?path=Assets/AISceneBuilder
```

Belirli bir sürüme sabitlemek için sonuna `#v1.0.0` ekleyebilirsiniz.

**Elle:** Bu depoyu indirip `Assets/AISceneBuilder` klasörünü kendi projenizin `Assets` klasörüne kopyalayın.

Gereksinim: Unity 2021.3 veya üzeri.

## Kullanım

1. Projeyi Unity ile açın (dosyalar `Assets/AISceneBuilder/` altındadır).
2. Menüden **Tools → AI Scene Builder** penceresini açın.
3. Pencereden sağlayıcınızı seçin: **Gemini** (ücretsiz katman — [aistudio.google.com/apikey](https://aistudio.google.com/apikey)) veya **Claude** ([platform.claude.com](https://platform.claude.com)).
4. API anahtarınızı girin ve komutunuzu yazın.
