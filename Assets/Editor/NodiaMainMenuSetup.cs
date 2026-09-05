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
    // Builds the main menu (persistent corner button, Tab as a desktop
    // shortcut) - a single symmetric column of buttons that replaces the
    // separate F/M/P/H hotkeys. Reuses the rounded card sprite, round dot
    // sprite, and Noto Sans JP TMP fonts from Nodia > Style Note UI, so run
    // that (plus Setup Search / Setup Space Select / Setup Settings / Setup
    // Help, so this can find and wire them) first.
    public static class NodiaMainMenuSetup
    {
        private static readonly Color PanelBg = new Color(0.118f, 0.118f, 0.133f, 0.99f);
        private static readonly Color Neutral = new Color(0.24f, 0.24f, 0.27f);
        private static readonly Color Accent = new Color(0.42f, 0.56f, 0.96f);
        private static readonly Color TextColor = new Color(0.90f, 0.90f, 0.92f);

        [MenuItem("Nodia/Setup Main Menu")]
        public static void SetupMainMenu()
        {
            var canvasGO = GameObject.Find("NoteCanvas");
            var playerGO = GameObject.Find("Player");
            var fpsController = playerGO != null ? playerGO.GetComponent<FPSController>() : null;
            var cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/UI/Card.png");
            var dotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/UI/Dot.png");
            var regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansJP-Regular SDF.asset");
            var boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansJP-Bold SDF.asset");

            if (canvasGO == null || fpsController == null)
            {
                Debug.LogError("NODIA: NoteCanvas/Player not found - run Nodia > Setup Scene first.");
                return;
            }
            if (cardSprite == null || dotSprite == null || regularFont == null || boldFont == null)
            {
                Debug.LogError("NODIA: shared UI assets not found - run Nodia > Style Note UI first.");
                return;
            }

            var panel = GetOrCreatePanel(canvasGO.transform, cardSprite, dotSprite, boldFont, regularFont);
            var menuButton = GetOrCreateMenuButton(canvasGO.transform, cardSprite, boldFont);

            var searchButton = GetOrCreateButton(panel.transform, "SearchButton", "ノード検索", 140, Neutral, cardSprite, boldFont);
            var spacesButton = GetOrCreateButton(panel.transform, "SpacesButton", "スペース切り替え", 60, Neutral, cardSprite, boldFont);
            var settingsButton = GetOrCreateButton(panel.transform, "SettingsButton", "設定", -20, Neutral, cardSprite, boldFont);
            var helpButton = GetOrCreateButton(panel.transform, "HelpButton", "操作方法", -100, Neutral, cardSprite, boldFont);
            var resumeButton = GetOrCreateButton(panel.transform, "ResumeButton", "閉じる", -200, Accent, cardSprite, boldFont, textColor: Color.black);

            var controllerGO = GameObject.Find("MainMenuController");
            if (controllerGO == null) controllerGO = new GameObject("MainMenuController");
            var controller = controllerGO.GetComponent<MainMenuController>();
            if (controller == null) controller = controllerGO.AddComponent<MainMenuController>();

            SetField(controller, "panel", panel);
            SetField(controller, "menuButtonRoot", menuButton.gameObject);
            SetField(controller, "menuButton", menuButton);
            SetField(controller, "searchButton", searchButton);
            SetField(controller, "spacesButton", spacesButton);
            SetField(controller, "settingsButton", settingsButton);
            SetField(controller, "helpButton", helpButton);
            SetField(controller, "resumeButton", resumeButton);
            SetField(controller, "fpsController", fpsController);

            var noteUIGO = GameObject.Find("NoteUIController");
            if (noteUIGO != null) SetField(controller, "noteUI", noteUIGO.GetComponent<NoteUIController>());
            var searchGO = GameObject.Find("NodeSearchController");
            if (searchGO != null) SetField(controller, "searchController", searchGO.GetComponent<NodeSearchController>());
            var spaceGO = GameObject.Find("SpaceSelectController");
            if (spaceGO != null) SetField(controller, "spaceSelect", spaceGO.GetComponent<SpaceSelectController>());
            var settingsGO = GameObject.Find("SettingsController");
            if (settingsGO != null) SetField(controller, "settingsController", settingsGO.GetComponent<SettingsController>());
            var helpGO = GameObject.Find("HelpController");
            if (helpGO != null) SetField(controller, "helpController", helpGO.GetComponent<HelpController>());

            var interactor = playerGO.GetComponent<Nodia.Interaction.PlayerInteractor>();
            if (interactor != null) SetField(interactor, "mainMenu", controller);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("NODIA: main menu ready - press Tab in Play mode (the corner button only shows on a touchscreen).");
        }

        // Corner button that opens the menu with a click - MainMenuController
        // only shows it on a touchscreen, since on desktop the mouse is
        // pointer-locked during gameplay and can't reach any on-screen
        // button anyway (Tab is the real way in there).
        private static Button GetOrCreateMenuButton(Transform canvas, Sprite cardSprite, TMP_FontAsset boldFont)
        {
            var go = GetOrCreateChild(canvas, "MenuButton", typeof(Image));
            if (go.GetComponent<Button>() == null) go.AddComponent<Button>();
            go.transform.SetAsLastSibling();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(56, 56);
            rect.anchoredPosition = new Vector2(-24, -24);

            var image = go.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = Neutral;

            var textGO = GetOrCreateChild(go.transform, "Text", typeof(TextMeshProUGUI));
            SetStretchRect(textGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var text = textGO.GetComponent<TextMeshProUGUI>();
            text.text = "≡";
            text.font = boldFont;
            text.fontSize = 28;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.Center;

            return go.GetComponent<Button>();
        }

        private static GameObject GetOrCreatePanel(Transform canvas, Sprite cardSprite, Sprite dotSprite,
            TMP_FontAsset boldFont, TMP_FontAsset regularFont)
        {
            var existing = canvas.Find("MainMenuPanel");
            var panel = existing != null ? existing.gameObject : new GameObject("MainMenuPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas, false);
            panel.transform.SetAsLastSibling();

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(560, 620);
            rect.anchoredPosition = Vector2.zero;

            var image = panel.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = PanelBg;

            var dotGO = GetOrCreateChild(panel.transform, "HeaderDot", typeof(Image));
            var dotRect = dotGO.GetComponent<RectTransform>();
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(10, 10);
            dotRect.anchoredPosition = new Vector2(0, 270);
            var dotImage = dotGO.GetComponent<Image>();
            dotImage.sprite = dotSprite;
            dotImage.type = Image.Type.Sliced;
            dotImage.color = Accent;

            var headerGO = GetOrCreateChild(panel.transform, "HeaderLabelTMP", typeof(TextMeshProUGUI));
            var headerRect = headerGO.GetComponent<RectTransform>();
            headerRect.anchorMin = headerRect.anchorMax = new Vector2(0.5f, 0.5f);
            headerRect.sizeDelta = new Vector2(400, 34);
            headerRect.anchoredPosition = new Vector2(0, 240);
            var headerText = headerGO.GetComponent<TextMeshProUGUI>();
            headerText.text = "メニュー";
            headerText.font = boldFont;
            headerText.fontSize = 20;
            headerText.color = TextColor;
            headerText.alignment = TextAlignmentOptions.Center;

            return panel;
        }

        private static Button GetOrCreateButton(Transform panel, string name, string label, float y, Color bg,
            Sprite cardSprite, TMP_FontAsset boldFont, Color? textColor = null)
        {
            var go = GetOrCreateChild(panel, name, typeof(Image));
            if (go.GetComponent<Button>() == null) go.AddComponent<Button>();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(380, 60);
            rect.anchoredPosition = new Vector2(0, y);

            var image = go.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = bg;

            var textGO = GetOrCreateChild(go.transform, "Text", typeof(TextMeshProUGUI));
            SetStretchRect(textGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var text = textGO.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.font = boldFont;
            text.fontSize = 20;
            text.color = textColor ?? TextColor;
            text.alignment = TextAlignmentOptions.Center;

            return go.GetComponent<Button>();
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
