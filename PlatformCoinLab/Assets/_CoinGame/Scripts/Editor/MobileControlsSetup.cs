using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CoinGame.Editor
{
    using CoinGame.Input;
    using CoinGame.UI;

    public static class MobileControlsSetup
    {
        private static TMP_FontAsset _font;
        private static Sprite _circle;
        private static readonly Color Ink = new Color(0.07f, 0.11f, 0.19f, 0.92f);
        private static readonly Color Gold = new Color(1f, 0.78f, 0.36f, 1f);

        [MenuItem("CoinGame/UI/Configure Mobile Controls")]
        public static void Configure()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene("Assets/_CoinGame/Scenes/InGame_CoinGameScene.unity");
            GameObject mobile = GameObject.Find("MobileControls");
            if (mobile.GetComponent<MobileControlsView>() != null)
            {
                Debug.Log("Mobile controls are already configured; edit their Inspector references and layout.");
                return;
            }

            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_CoinGame/Fonts/NotoSansKR_Dynamic.asset");
            RectTransform root = (RectTransform)mobile.transform;
            Stretch(root);
            Stretch((RectTransform)root.parent);
            RectTransform visual = (RectTransform)root.Find("JoystickBase");
            RectTransform handle = (RectTransform)visual.Find("JoystickHandle");
            _circle = handle.GetComponent<Image>().sprite;
            Object.DestroyImmediate(handle.GetComponent<UnityEngine.InputSystem.OnScreen.OnScreenStick>());

            string path = "Assets/_CoinGame/ScriptableObject/MobileControlsSettings.asset";
            MobileControlsSettings settings = AssetDatabase.LoadAssetAtPath<MobileControlsSettings>(path);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<MobileControlsSettings>();
                AssetDatabase.CreateAsset(settings, path);
            }

            MobileControlInput input = mobile.AddComponent<MobileControlInput>();
            MobileControlLayout layout = mobile.AddComponent<MobileControlLayout>();
            MobileControlsView view = mobile.AddComponent<MobileControlsView>();
            RectTransform touch = Rect("MoveTouchArea", root, Vector2.zero, Vector2.zero);
            Stretch(touch);
            touch.anchorMax = new Vector2(0.48f, 0.66f);
            Image hit = touch.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            visual.SetParent(touch, false);
            visual.anchorMin = visual.anchorMax = new Vector2(0.5f, 0.5f);
            visual.sizeDelta = new Vector2(250f, 250f);
            visual.GetComponent<Image>().color = new Color(0.1f, 0.16f, 0.26f, 0.7f);
            handle.SetParent(visual, false);
            handle.sizeDelta = new Vector2(105f, 105f);
            handle.GetComponent<Image>().color = Gold;
            visual.GetComponent<Image>().raycastTarget = false;
            handle.GetComponent<Image>().raycastTarget = false;
            CanvasGroup joyGroup = visual.gameObject.AddComponent<CanvasGroup>();
            joyGroup.alpha = 0f;
            joyGroup.blocksRaycasts = false;
            FloatingJoystick joystick = touch.gameObject.AddComponent<FloatingJoystick>();
            Set(joystick, "_touchArea", touch, "_visual", visual, "_handle", handle, "_group", joyGroup, "_settings", settings, "_input", input);

            List<MobileActionButton> actions = new List<MobileActionButton>();
            actions.Add(Action(root, input, layout, MobileAction.Attack, "공격", new Vector2(-140f, 235f), new Vector2(185f, 185f)));
            actions.Add(Action(root, input, layout, MobileAction.Reload, "장전", new Vector2(-140f, 430f), new Vector2(120f, 120f)));
            actions.Add(Action(root, input, layout, MobileAction.Push, "밀치기", new Vector2(-345f, 220f), new Vector2(135f, 135f)));
            actions.Add(Action(root, input, layout, MobileAction.Skill, "코인 봉쇄", new Vector2(-335f, 405f), new Vector2(135f, 135f)));
            actions.Add(Action(root, input, layout, MobileAction.Boost, "부스트", new Vector2(-535f, 205f), new Vector2(120f, 120f)));
            actions.Add(Action(root, input, layout, MobileAction.Revive, "부활", new Vector2(-535f, 375f), new Vector2(120f, 120f)));
            actions.Add(Action(root, input, layout, MobileAction.Fist, "주먹", new Vector2(-430f, 65f), new Vector2(110f, 90f)));
            actions.Add(Action(root, input, layout, MobileAction.Gun, "총", new Vector2(-290f, 65f), new Vector2(110f, 90f)));
            actions.Add(Action(root, input, layout, MobileAction.Sword, "검", new Vector2(-150f, 65f), new Vector2(110f, 90f)));

            Button edit = SimpleButton("EditControls", root, "배치", new Vector2(-65f, 580f), new Vector2(100f, 60f));
            UnityEventTools.AddPersistentListener(edit.onClick, layout.ToggleEditing);
            RectTransform panel = Rect("LayoutSettings", root, new Vector2(-390f, 600f), new Vector2(520f, 140f));
            panel.gameObject.AddComponent<Image>().color = Ink;
            Text(panel, "Hint", "버튼을 드래그해 이동 · 크기", new Vector2(-50f, 40f), new Vector2(390f, 40f), 23f);
            Button reset = SimpleButton("Reset", panel, "초기화", new Vector2(195f, 0f), new Vector2(110f, 55f));
            reset.GetComponent<RectTransform>().anchorMin = reset.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            UnityEventTools.AddPersistentListener(reset.onClick, layout.ResetLayout);
            RectTransform sliderRect = Rect("ButtonScale", panel, new Vector2(-65f, -20f), new Vector2(330f, 32f));
            sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.3f, 0.38f);
            Slider slider = sliderRect.gameObject.AddComponent<Slider>();
            RectTransform knob = Rect("Handle", sliderRect, Vector2.zero, new Vector2(32f, 46f));
            Image knobImage = knob.gameObject.AddComponent<Image>();
            knobImage.color = Gold;
            slider.handleRect = knob;
            slider.targetGraphic = knobImage;
            UnityEventTools.AddPersistentListener(slider.onValueChanged, layout.ApplyScale);
            panel.gameObject.SetActive(false);

            Set(layout, "_root", root, "_settings", settings, "_input", input, "_joystick", joystick, "_panel", panel.gameObject, "_editLabel", edit.GetComponentInChildren<TMP_Text>(), "_scaleSlider", slider);
            SerializedObject layoutData = new SerializedObject(layout);
            SerializedProperty buttons = layoutData.FindProperty("_buttons");
            buttons.arraySize = actions.Count;
            for (int i = 0; i < actions.Count; i++)
            {
                buttons.GetArrayElementAtIndex(i).objectReferenceValue = actions[i].transform;
            }

            layoutData.ApplyModifiedPropertiesWithoutUndo();
            Set(view, "_input", input, "_joystick", joystick, "_layout", layout, "_abilities", AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_CoinGame/ScriptableObject/Role Ability Config.asset"));
            SerializedObject viewData = new SerializedObject(view);
            SerializedProperty buttonArray = viewData.FindProperty("_buttons");
            buttonArray.arraySize = actions.Count;
            for (int i = 0; i < actions.Count; i++)
            {
                buttonArray.GetArrayElementAtIndex(i).objectReferenceValue = actions[i];
            }

            viewData.ApplyModifiedPropertiesWithoutUndo();
            RuntimeFontAtlas atlas = root.GetComponentInParent<RuntimeFontAtlas>();
            SerializedObject fonts = new SerializedObject(atlas);
            SerializedProperty texts = fonts.FindProperty("_texts");
            TMP_Text[] all = atlas.GetComponentsInChildren<TMP_Text>(true);
            texts.arraySize = all.Length;
            for (int i = 0; i < all.Length; i++)
            {
                texts.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            }

            fonts.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static MobileActionButton Action(RectTransform parent, MobileControlInput input, MobileControlLayout layout, MobileAction action, string label, Vector2 point, Vector2 size)
        {
            RectTransform rect = Rect(action.ToString(), parent, point, size);
            Image face = rect.gameObject.AddComponent<Image>();
            face.sprite = _circle;
            face.color = Ink;
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.78f, 0.36f, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);
            CanvasGroup group = rect.gameObject.AddComponent<CanvasGroup>();
            RectTransform overlay = Rect("Cooldown", rect, Vector2.zero, size);
            overlay.anchorMin = overlay.anchorMax = new Vector2(0.5f, 0.5f);
            Image cooldown = overlay.gameObject.AddComponent<Image>();
            cooldown.sprite = _circle;
            cooldown.color = new Color(0f, 0f, 0f, 0.65f);
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillAmount = 0f;
            cooldown.raycastTarget = false;
            Text(rect, "Label", label, new Vector2(0f, 10f), new Vector2(size.x - 10f, 45f), 26f);
            TMP_Text detail = Text(rect, "Detail", "", new Vector2(0f, -28f), new Vector2(size.x, 35f), 19f);
            MobileActionButton button = rect.gameObject.AddComponent<MobileActionButton>();
            Set(button, "_input", input, "_layout", layout, "_group", group, "_face", face, "_cooldown", cooldown, "_detail", detail);
            SerializedObject data = new SerializedObject(button);
            data.FindProperty("_action").enumValueIndex = (int)action;
            data.ApplyModifiedPropertiesWithoutUndo();
            return button;
        }

        private static Button SimpleButton(string name, RectTransform parent, string label, Vector2 point, Vector2 size)
        {
            RectTransform rect = Rect(name, parent, point, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Ink;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text(rect, "Label", label, Vector2.zero, size, 22f);
            return button;
        }

        private static TMP_Text Text(RectTransform parent, string name, string value, Vector2 point, Vector2 size, float fontSize)
        {
            RectTransform rect = Rect(name, parent, point, size);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Gold;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 point, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.anchoredPosition = point;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Set(Object target, params object[] pairs)
        {
            SerializedObject data = new SerializedObject(target);
            for (int i = 0; i < pairs.Length; i += 2)
            {
                data.FindProperty((string)pairs[i]).objectReferenceValue = (Object)pairs[i + 1];
            }

            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
