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
    // Builds the startup screen (guest vs. email account) and wires it to
    // AuthStartupController. Reuses the rounded card sprite and Noto Sans JP
    // TMP fonts from Nodia > Style Note UI, so run that first.
    public static class NodiaAuthSetup
    {
        private static readonly Color PanelBg = new Color(0.118f, 0.118f, 0.133f, 0.99f);
        private static readonly Color FieldBg = new Color(1f, 1f, 1f, 0.05f);
        private static readonly Color Accent = new Color(0.42f, 0.56f, 0.96f);
        private static readonly Color Neutral = new Color(0.21f, 0.21f, 0.23f);
        private static readonly Color TextColor = new Color(0.90f, 0.90f, 0.92f);
        private static readonly Color MutedText = new Color(0.52f, 0.53f, 0.57f);

        [MenuItem("Nodia/Setup Auth Screen")]
        public static void SetupAuthScreen()
        {
            var canvas = GameObject.Find("NoteCanvas");
            var playerGO = GameObject.Find("Player");
            var fpsController = playerGO != null ? playerGO.GetComponent<FPSController>() : null;

            if (canvas == null || fpsController == null)
            {
                Debug.LogError("NODIA: NoteCanvas/Player not found - run Nodia > Setup Scene first.");
                return;
            }

            var cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/UI/Card.png");
            var dotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/UI/Dot.png");
            var regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansJP-Regular SDF.asset");
            var boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansJP-Bold SDF.asset");
            if (cardSprite == null || dotSprite == null || regularFont == null || boldFont == null)
            {
                Debug.LogError("NODIA: shared UI assets not found - run Nodia > Style Note UI first.");
                return;
            }

            var panel = GetOrCreatePanel(canvas.transform, cardSprite, dotSprite, boldFont);
            var guestButton = GetOrCreateButton(panel.transform, "GuestButton", cardSprite, boldFont,
                "お試しで始める（ログイン不要）", new Vector2(0, 145), new Vector2(560, 60), Accent, Color.black);

            GetOrCreateDivider(panel.transform, regularFont);

            var emailField = GetOrCreateField(panel.transform, "EmailField", cardSprite, regularFont,
                new Vector2(0, 0), "メールアドレス", false);
            var passwordField = GetOrCreateField(panel.transform, "PasswordField", cardSprite, regularFont,
                new Vector2(0, -70), "パスワード", true);

            var signUpButton = GetOrCreateButton(panel.transform, "SignUpButton", cardSprite, boldFont,
                "新規登録", new Vector2(-145, -140), new Vector2(270, 54), Accent, Color.black);
            var signInButton = GetOrCreateButton(panel.transform, "SignInButton", cardSprite, boldFont,
                "ログイン", new Vector2(145, -140), new Vector2(270, 54), Neutral, TextColor);

            var statusText = GetOrCreateStatusText(panel.transform, regularFont);

            var controllerGO = GameObject.Find("AuthStartupController");
            if (controllerGO == null) controllerGO = new GameObject("AuthStartupController");
            var controller = controllerGO.GetComponent<AuthStartupController>();
            if (controller == null) controller = controllerGO.AddComponent<AuthStartupController>();

            SetField(controller, "panel", panel);
            SetField(controller, "guestButton", guestButton);
            SetField(controller, "emailField", emailField);
            SetField(controller, "passwordField", passwordField);
            SetField(controller, "signUpButton", signUpButton);
            SetField(controller, "signInButton", signInButton);
            SetField(controller, "statusText", statusText);
            SetField(controller, "fpsController", fpsController);

            var interactor = playerGO != null ? playerGO.GetComponent<Nodia.Interaction.PlayerInteractor>() : null;
            if (interactor != null) SetField(interactor, "authStartup", controller);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("NODIA: auth startup screen ready. Run Nodia > Setup Space Select next to wire the screen after it.");
        }

        private static GameObject GetOrCreatePanel(Transform canvas, Sprite cardSprite, Sprite dotSprite, TMP_FontAsset boldFont)
        {
            var existing = canvas.Find("AuthPanel");
            var panel = existing != null ? existing.gameObject
                : new GameObject("AuthPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas, false);
            panel.transform.SetAsLastSibling();

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(640, 560);
            rect.anchoredPosition = Vector2.zero;

            var image = panel.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = PanelBg;

            // Every element in this panel anchors at center (0.5, 0.5) and
            // positions itself with a plain Y offset from panel center - mixing
            // a top-pinned anchor in here for just the dot/header caused them
            // to land on the exact same spot as the guest button before.
            var dotTransform = panel.transform.Find("HeaderDot");
            var dotGO = dotTransform != null ? dotTransform.gameObject
                : new GameObject("HeaderDot", typeof(RectTransform), typeof(Image));
            dotGO.transform.SetParent(panel.transform, false);
            var dotRect = dotGO.GetComponent<RectTransform>();
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(10, 10);
            dotRect.anchoredPosition = new Vector2(0, 240);
            var dotImage = dotGO.GetComponent<Image>();
            dotImage.sprite = dotSprite;
            dotImage.type = Image.Type.Sliced;
            dotImage.color = Accent;

            var headerTransform = panel.transform.Find("HeaderLabelTMP");
            var headerGO = headerTransform != null ? headerTransform.gameObject
                : new GameObject("HeaderLabelTMP", typeof(RectTransform));
            headerGO.transform.SetParent(panel.transform, false);
            var headerRect = headerGO.GetComponent<RectTransform>();
            headerRect.anchorMin = headerRect.anchorMax = new Vector2(0.5f, 0.5f);
            headerRect.sizeDelta = new Vector2(400, 34);
            headerRect.anchoredPosition = new Vector2(0, 210);
            var headerText = headerGO.GetComponent<TextMeshProUGUI>();
            if (headerText == null) headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = "NODIA";
            headerText.font = boldFont;
            headerText.fontSize = 22;
            headerText.color = TextColor;
            headerText.alignment = TextAlignmentOptions.Center;

            return panel;
        }

        private static void GetOrCreateDivider(Transform panel, TMP_FontAsset regularFont)
        {
            var existing = panel.Find("Divider");
            var go = existing != null ? existing.gameObject : new GameObject("Divider", typeof(RectTransform));
            go.transform.SetParent(panel, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 24);
            rect.anchoredPosition = new Vector2(0, 70);

            var text = go.GetComponent<TextMeshProUGUI>();
            if (text == null) text = go.AddComponent<TextMeshProUGUI>();
            text.text = "または メールアドレスでアカウント登録・ログイン";
            text.font = regularFont;
            text.fontSize = 15;
            text.color = MutedText;
            text.alignment = TextAlignmentOptions.Center;
        }

        private static TMP_InputField GetOrCreateField(Transform panel, string name, Sprite cardSprite,
            TMP_FontAsset regularFont, Vector2 anchoredPos, string placeholderText, bool isPassword)
        {
            var existing = panel.Find(name);
            var fieldGO = existing != null ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Image));
            fieldGO.transform.SetParent(panel, false);

            var rect = fieldGO.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(560, 56);
            rect.anchoredPosition = anchoredPos;

            var image = fieldGO.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = FieldBg;

            var textTransform = fieldGO.transform.Find("Text");
            var textGO = textTransform != null ? textTransform.gameObject : new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(fieldGO.transform, false);
            SetStretchRect(textGO.GetComponent<RectTransform>(), new Vector2(20, 8), new Vector2(-20, -8));
            var text = textGO.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textGO.AddComponent<TextMeshProUGUI>();
            text.font = regularFont;
            text.fontSize = 20;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.MidlineLeft;

            var placeholderTransform = fieldGO.transform.Find("Placeholder");
            var placeholderGO = placeholderTransform != null ? placeholderTransform.gameObject
                : new GameObject("Placeholder", typeof(RectTransform));
            placeholderGO.transform.SetParent(fieldGO.transform, false);
            SetStretchRect(placeholderGO.GetComponent<RectTransform>(), new Vector2(20, 8), new Vector2(-20, -8));
            var placeholder = placeholderGO.GetComponent<TextMeshProUGUI>();
            if (placeholder == null) placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholder.font = regularFont;
            placeholder.fontSize = 20;
            placeholder.color = MutedText;
            placeholder.text = placeholderText;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;

            var input = fieldGO.GetComponent<TMP_InputField>();
            if (input == null) input = fieldGO.AddComponent<TMP_InputField>();
            input.textViewport = rect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            if (isPassword)
            {
                input.contentType = TMP_InputField.ContentType.Password;
                text.text = "";
            }

            return input;
        }

        private static Button GetOrCreateButton(Transform panel, string name, Sprite cardSprite, TMP_FontAsset boldFont,
            string label, Vector2 anchoredPos, Vector2 size, Color bg, Color textColor)
        {
            var existing = panel.Find(name);
            var go = existing != null ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            var image = go.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = bg;

            var textTransform = go.transform.Find("Text");
            var textGO = textTransform != null ? textTransform.gameObject : new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            SetStretchRect(textGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var text = textGO.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.font = boldFont;
            text.fontSize = 18;
            text.color = textColor;
            text.alignment = TextAlignmentOptions.Center;

            return go.GetComponent<Button>();
        }

        private static TMP_Text GetOrCreateStatusText(Transform panel, TMP_FontAsset regularFont)
        {
            var existing = panel.Find("StatusText");
            var go = existing != null ? existing.gameObject : new GameObject("StatusText", typeof(RectTransform));
            go.transform.SetParent(panel, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(560, 40);
            rect.anchoredPosition = new Vector2(0, 30);

            var text = go.GetComponent<TextMeshProUGUI>();
            if (text == null) text = go.AddComponent<TextMeshProUGUI>();
            text.text = "";
            text.font = regularFont;
            text.fontSize = 15;
            text.color = MutedText;
            text.alignment = TextAlignmentOptions.Center;

            return text;
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
