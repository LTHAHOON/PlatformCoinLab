using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoinGame.Editor
{
    using CoinGame.Content;
    using CoinGame.UI;
    using CoinGame.Utilities;

    public static class BootstrapSceneBuilder
    {
        private static readonly Color Gold = new Color(0.896f, 0.712f, 0.292f);

        public static void Create(BootstrapContentSettings settings)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            GameObject canvasObject = new GameObject("BootstrapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform background = Rect("Background", canvasObject.transform, Vector2.zero, Vector2.zero);
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            Image backdrop = background.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.035f, 0.055f, 0.1f);
            RectTransform safe = Rect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.zero);
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            SafeArea safeArea = safe.gameObject.AddComponent<SafeArea>();
            Set(safeArea, "_root", safe);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_CoinGame/Fonts/NotoSansKR_Dynamic.asset");
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/2D Casual UI/Sprite/GUI.png").OfType<Sprite>().ToArray();
            RectTransform card = Rect("DownloadCard", safe, new Vector2(960, 620), Vector2.zero);
            Image panel = card.gameObject.AddComponent<Image>();
            panel.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            panel.type = Image.Type.Sliced;
            panel.color = new Color(0.087f, 0.121f, 0.217f);
            TMP_Text title = Text("Title", card, "COIN GAME", 62, new Vector2(840, 90), new Vector2(0, 210), font);
            title.color = Gold;
            TMP_Text subtitle = Text("Subtitle", card, "게임 리소스 준비", 26, new Vector2(840, 50), new Vector2(0, 135), font);
            TMP_Text status = Text("Status", card, "콘텐츠 확인 중", 36, new Vector2(840, 70), new Vector2(0, 40), font);
            TMP_Text detail = Text("Detail", card, "최신 리소스 목록을 확인하고 있습니다.", 25, new Vector2(840, 90), new Vector2(0, -40), font);
            RectTransform track = Rect("ProgressTrack", card, new Vector2(780, 22), new Vector2(0, -125));
            track.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.04f, 0.085f);
            RectTransform fill = Rect("ProgressFill", track, Vector2.zero, Vector2.zero);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            Image progress = fill.gameObject.AddComponent<Image>();
            progress.sprite = sprites.First(x => x.name == "GUI_12");
            progress.color = Gold;
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            progress.fillAmount = 0;
            Button download = Button("DownloadButton", card, "다운로드", font, sprites);
            Button retry = Button("RetryButton", card, "다시 시도", font, sprites);
            BootstrapDownloadView view = canvasObject.AddComponent<BootstrapDownloadView>();
            Set(view, "_status", status);
            Set(view, "_detail", detail);
            Set(view, "_progress", progress);
            Set(view, "_downloadButton", download);
            Set(view, "_retryButton", retry);
            ContentBootstrap bootstrap = canvasObject.AddComponent<ContentBootstrap>();
            Set(bootstrap, "_settings", settings);
            Set(bootstrap, "_view", view);
            RuntimeFontAtlas atlas = canvasObject.AddComponent<RuntimeFontAtlas>();
            Set(atlas, "_source", font);
            SerializedObject serialized = new SerializedObject(atlas);
            TMP_Text[] texts = canvasObject.GetComponentsInChildren<TMP_Text>(true);
            SerializedProperty array = serialized.FindProperty("_texts");
            array.arraySize = texts.Length;
            for (int i = 0; i < texts.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = texts[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EditorSceneManager.SaveScene(scene, BootstrapProjectSetup.ScenePath);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static TMP_Text Text(string name, Transform parent, string value, float size, Vector2 bounds, Vector2 position, TMP_FontAsset font)
        {
            TMP_Text text = Rect(name, parent, bounds, position).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static Button Button(string name, Transform parent, string label, TMP_FontAsset font, Sprite[] sprites)
        {
            RectTransform rect = Rect(name, parent, new Vector2(330, 76), new Vector2(0, -220));
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = Gold;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TMP_Text text = Text("Label", rect, label, 30, new Vector2(310, 65), Vector2.zero, font);
            text.color = new Color(0.06f, 0.08f, 0.12f);
            rect.gameObject.SetActive(false);
            return button;
        }

        private static void Set(Object target, string field, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
