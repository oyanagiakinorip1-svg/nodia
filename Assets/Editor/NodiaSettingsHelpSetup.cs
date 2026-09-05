using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Nodia.Player;
using Nodia.UI;

namespace Nodia.EditorTools
{
    // Builds the Settings and Help overlays, both opened from the main menu
    // (corner button / Tab) rather than their own hotkeys. Reuses the rounded card sprite,
    // the round dot sprite, and Noto Sans JP TMP fonts from
    // Nodia > Style Note UI, so run that first.
    public static class NodiaSettingsHelpSetup
    {
        private static readonly Color PanelBg = new Color(0.118f, 0.118f, 0.133f, 0.99f);
        private static readonly Color TrackBg = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color Accent = new Color(0.42f, 0.56f, 0.96f);
        private static readonly Color TextColor = new Color(0.90f, 0.90f, 0.92f);
        private static readonly Color MutedText = new Color(0.52f, 0.53f, 0.57f);

        private const float RowWidth = 480f;

        [MenuItem("Nodia/Setup Settings")]
        public static void SetupSettings()
        {
            if (!TryGetShared(out var canvas, out _, out var fpsController, out var cardSprite,
                    out var dotSprite, out var regularFont, out var boldFont))
            {
                return;
            }

            var panel = GetOrCreatePanel(canvas.transform, "SettingsPanel", "設定", cardSprite, dotSprite, boldFont,
                new Vector2(640, 520));

            var sensSlider = GetOrCreateSlider(panel.transform, "SensitivitySlider", new Vector2(0, 95), 0.05f, 0.6f, dotSprite);
            var sensValue = CreateRow(panel.transform, "Sensitivity", "マウス感度", 130, regularFont);

            var moveSlider = GetOrCreateSlider(panel.transform, "MoveSpeedSlider", new Vector2(0, -25), 2f, 15f, dotSprite);
            var moveValue = CreateRow(panel.transform, "MoveSpeed", "移動速度", 10, regularFont);

            var vertSlider = GetOrCreateSlider(panel.transform, "VerticalSpeedSlider", new Vector2(0, -145), 1f, 10f, dotSprite);
            var vertValue = CreateRow(panel.transform, "VerticalSpeed", "上下移動速度", -110, regularFont);

            var closeButton = GetOrCreateCloseButton(panel.transform, cardSprite, boldFont);

            var controllerGO = GameObject.Find("SettingsController");
            if (controllerGO == null) controllerGO = new GameObject("SettingsController");
            var controller = controllerGO.GetComponent<SettingsController>();
            if (controller == null) controller = controllerGO.AddComponent<SettingsController>();

            SetField(controller, "panel", panel);
            SetField(controller, "sensitivitySlider", sensSlider);
            SetField(controller, "moveSpeedSlider", moveSlider);
            SetField(controller, "verticalSpeedSlider", vertSlider);
            SetField(controller, "sensitivityValueText", sensValue);
            SetField(controller, "moveSpeedValueText", moveValue);
            SetField(controller, "verticalSpeedValueText", vertValue);
            SetField(controller, "closeButton", closeButton);
            SetField(controller, "fpsController", fpsController);

            var menuGO = GameObject.Find("MainMenuController");
            if (menuGO != null) SetField(menuGO.GetComponent<MainMenuController>(), "settingsController", controller);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("NODIA: settings screen ready - opened from the main menu (Tab).");
        }

        [MenuItem("Nodia/Setup Help")]
        public static void SetupHelp()
        {
            if (!TryGetShared(out var canvas, out _, out var fpsController, out var cardSprite,
                    out var dotSprite, out var regularFont, out var boldFont))
            {
                return;
            }

            var panel = GetOrCreatePanel(canvas.transform, "HelpPanel", "操作方法", cardSprite, dotSprite, boldFont,
                new Vector2(640, 700));

            var bodyTransform = panel.transform.Find("Body");
            var bodyGO = bodyTransform != null ? bodyTransform.gameObject : new GameObject("Body", typeof(RectTransform));
            bodyGO.transform.SetParent(panel.transform, false);
            var bodyRect = bodyGO.GetComponent<RectTransform>();
            bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.sizeDelta = new Vector2(540, 500);
            bodyRect.anchoredPosition = new Vector2(0, -20);
            var bodyText = bodyGO.GetComponent<TextMeshProUGUI>();
            if (bodyText == null) bodyText = bodyGO.AddComponent<TextMeshProUGUI>();
            bodyText.font = regularFont;
            bodyText.fontSize = 20;
            bodyText.color = TextColor;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.lineSpacing = 12;
            bodyText.text =
                "WASD : 移動\n" +
                "マウス : 視点操作\n" +
                "Space / Ctrl : 上昇 / 下降\n\n" +
                "右クリック : 空間にノードを作成\n" +
                "左クリック（ノードに向けて）: メモを開く\n" +
                "Shift + 左クリック : ノードを2つ選んで接続\n" +
                "Shift + 右クリック : 接続線を直接クリックして削除\n\n" +
                "Tab : メニューを開く（ノード検索・スペース切り替え・設定はここから）";

            var closeButton = GetOrCreateCloseButton(panel.transform, cardSprite, boldFont);

            var controllerGO = GameObject.Find("HelpController");
            if (controllerGO == null) controllerGO = new GameObject("HelpController");
            var controller = controllerGO.GetComponent<HelpController>();
            if (controller == null) controller = controllerGO.AddComponent<HelpController>();

            SetField(controller, "panel", panel);
            SetField(controller, "closeButton", closeButton);
            SetField(controller, "fpsController", fpsController);

            var menuGO = GameObject.Find("MainMenuController");
            if (menuGO != null) SetField(menuGO.GetComponent<MainMenuController>(), "helpController", controller);

            var spaceSelectGO = GameObject.Find("SpaceSelectController");
            if (spaceSelectGO != null) SetField(spaceSelectGO.GetComponent<SpaceSelectController>(), "helpController", controller);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("NODIA: help screen ready - opened from the main menu (Tab).");
        }

        private static bool TryGetShared(out Transform canvas, out GameObject playerGO, out FPSController fpsController,
            out Sprite cardSprite, out Sprite dotSprite, out TMP_FontAsset regularFont, out TMP_FontAsset boldFont)
        {
            var canvasGO = GameObject.Find("NoteCanvas");
            playerGO = GameObject.Find("Player");
            fpsController = playerGO != null ? playerGO.GetComponent<FPSController>() : null;
            canvas = canvasGO != null ? canvasGO.transform : null;

            cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/UI/Card.png");
            dotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/UI/Dot.png");
            regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansJP-Regular SDF.asset");
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansJP-Bold SDF.asset");

            if (canvas == null || fpsController == null)
            {
                Debug.LogError("NODIA: NoteCanvas/Player not found - run Nodia > Setup Scene first.");
                return false;
            }
            if (cardSprite == null || dotSprite == null || regularFont == null || boldFont == null)
            {
                Debug.LogError("NODIA: shared UI assets not found - run Nodia > Style Note UI first.");
                return false;
            }
            return true;
        }

        private static GameObject GetOrCreatePanel(Transform canvas, string name, string title, Sprite cardSprite,
            Sprite dotSprite, TMP_FontAsset boldFont, Vector2 size)
        {
            var existing = canvas.Find(name);
            var panel = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas, false);
            panel.transform.SetAsLastSibling();

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            var image = panel.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = PanelBg;

            var dotGO = GetOrCreateChild(panel.transform, "HeaderDot", typeof(Image));
            var dotRect = dotGO.GetComponent<RectTransform>();
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(10, 10);
            dotRect.anchoredPosition = new Vector2(0, size.y / 2f - 40f);
            var dotImage = dotGO.GetComponent<Image>();
            dotImage.sprite = dotSprite;
            dotImage.type = Image.Type.Sliced;
            dotImage.color = Accent;

            var headerGO = GetOrCreateChild(panel.transform, "HeaderLabelTMP", typeof(TextMeshProUGUI));
            var headerRect = headerGO.GetComponent<RectTransform>();
            headerRect.anchorMin = headerRect.anchorMax = new Vector2(0.5f, 0.5f);
            headerRect.sizeDelta = new Vector2(400, 34);
            headerRect.anchoredPosition = new Vector2(0, size.y / 2f - 70f);
            var headerText = headerGO.GetComponent<TextMeshProUGUI>();
            headerText.text = title;
            headerText.font = boldFont;
            headerText.fontSize = 20;
            headerText.color = TextColor;
            headerText.alignment = TextAlignmentOptions.Center;

            return panel;
        }

        // One row = a label (left) and its live value (right) on the same
        // line, directly above its slider - everything centered on the
        // panel's x=0 so both edges of every row line up symmetrically.
        private static TMP_Text CreateRow(Transform parent, string idPrefix, string labelText, float rowY, TMP_FontAsset font)
        {
            var labelGO = GetOrCreateChild(parent, $"{idPrefix}Label", typeof(TextMeshProUGUI));
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(RowWidth * 0.6f, 28);
            labelRect.anchoredPosition = new Vector2(-RowWidth / 2f + (RowWidth * 0.3f), rowY);
            var label = labelGO.GetComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.font = font;
            label.fontSize = 18;
            label.color = MutedText;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            var valueGO = GetOrCreateChild(parent, $"{idPrefix}Value", typeof(TextMeshProUGUI));
            var valueRect = valueGO.GetComponent<RectTransform>();
            valueRect.anchorMin = valueRect.anchorMax = new Vector2(0.5f, 0.5f);
            valueRect.sizeDelta = new Vector2(RowWidth * 0.4f, 28);
            valueRect.anchoredPosition = new Vector2(RowWidth / 2f - (RowWidth * 0.2f), rowY);
            var value = valueGO.GetComponent<TextMeshProUGUI>();
            value.font = font;
            value.fontSize = 18;
            value.color = TextColor;
            value.alignment = TextAlignmentOptions.MidlineRight;

            return value;
        }

        // Flat, unrounded track + fill (the rounded card sprite's corner
        // radius badly distorted a bar this thin) with the round dot sprite
        // as a simple circular handle - much less noisy than trying to force
        // the same 9-sliced card everywhere.
        private static Slider GetOrCreateSlider(Transform parent, string name, Vector2 anchoredPos, float minValue, float maxValue, Sprite dotSprite)
        {
            var go = GetOrCreateChild(parent, name, typeof(Slider));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(RowWidth, 8);
            rect.anchoredPosition = anchoredPos;

            var slider = go.GetComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.direction = Slider.Direction.LeftToRight;

            var bgGO = GetOrCreateChild(go.transform, "Background", typeof(Image));
            SetStretchRect(bgGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            bgGO.GetComponent<Image>().color = TrackBg;

            var fillAreaGO = GetOrCreateChild(go.transform, "Fill Area", null);
            SetStretchRect(fillAreaGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            var fillGO = GetOrCreateChild(fillAreaGO.transform, "Fill", typeof(Image));
            SetStretchRect(fillGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            fillGO.GetComponent<Image>().color = Accent;

            var handleAreaGO = GetOrCreateChild(go.transform, "Handle Slide Area", null);
            SetStretchRect(handleAreaGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            var handleGO = GetOrCreateChild(handleAreaGO.transform, "Handle", typeof(Image));
            var handleRect = handleGO.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(22, 22);
            var handleImage = handleGO.GetComponent<Image>();
            handleImage.sprite = dotSprite;
            handleImage.type = Image.Type.Sliced;
            handleImage.color = Color.white;

            slider.fillRect = fillGO.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            return slider;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name, System.Type extraComponent)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = extraComponent != null
                    ? new GameObject(name, typeof(RectTransform), extraComponent)
                    : new GameObject(name, typeof(RectTransform));
            }
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Button GetOrCreateCloseButton(Transform panel, Sprite cardSprite, TMP_FontAsset boldFont)
        {
            var go = GetOrCreateChild(panel, "CloseButton", typeof(Image));
            if (go.GetComponent<Button>() == null) go.AddComponent<Button>();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(44, 44);
            rect.anchoredPosition = new Vector2(-20, -20);

            var image = go.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.06f);

            var textGO = GetOrCreateChild(go.transform, "Text", typeof(TextMeshProUGUI));
            SetStretchRect(textGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var text = textGO.GetComponent<TextMeshProUGUI>();
            text.text = "×";
            text.font = boldFont;
            text.fontSize = 22;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.Center;

            return go.GetComponent<Button>();
        }

        private static void SetStretchRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetField(Object target, string fieldName, Object value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"NODIA setup: field '{fieldName}' not found on {target.GetType().Name}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
