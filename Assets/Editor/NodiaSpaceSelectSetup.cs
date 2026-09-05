using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Nodia.Nodes;
using Nodia.Player;
using Nodia.UI;

namespace Nodia.EditorTools
{
    // Builds the space-picker screen (list existing spaces + create new +
    // delete with confirmation) and wires it to SpaceSelectController. Reuses
    // the rounded card sprite and Noto Sans JP TMP fonts from Nodia > Style
    // Note UI, so run that first.
    public static class NodiaSpaceSelectSetup
    {
        private static readonly Color PanelBg = new Color(0.118f, 0.118f, 0.133f, 0.99f);
        private static readonly Color FieldBg = new Color(1f, 1f, 1f, 0.05f);
        private static readonly Color Accent = new Color(0.42f, 0.56f, 0.96f);
        private static readonly Color Danger = new Color(0.82f, 0.45f, 0.44f);
        private static readonly Color Neutral = new Color(0.21f, 0.21f, 0.23f);
        private static readonly Color TextColor = new Color(0.90f, 0.90f, 0.92f);
        private static readonly Color MutedText = new Color(0.52f, 0.53f, 0.57f);

        [MenuItem("Nodia/Setup Space Select")]
        public static void SetupSpaceSelect()
        {
            var canvas = GameObject.Find("NoteCanvas");
            var playerGO = GameObject.Find("Player");
            var nodeManagerGO = GameObject.Find("NodeManager");
            var fpsController = playerGO != null ? playerGO.GetComponent<FPSController>() : null;

            if (canvas == null || fpsController == null || nodeManagerGO == null)
            {
                Debug.LogError("NODIA: NoteCanvas/Player/NodeManager not found - run Nodia > Setup Scene first.");
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

            var rowPrefab = GetOrCreateSpaceRowPrefab(cardSprite, regularFont, boldFont);

            var panel = GetOrCreatePanel(canvas.transform, cardSprite, dotSprite, boldFont);
            var listContainer = GetOrCreateListContainer(panel.transform);
            var nameField = GetOrCreateField(panel.transform, cardSprite, regularFont);
            var createButton = GetOrCreateButton(panel.transform, cardSprite, boldFont);
            var closeButton = GetOrCreateCloseButton(panel.transform, cardSprite, boldFont);
            var statusText = GetOrCreateStatusText(panel.transform, regularFont);
            var (confirmPanel, confirmText, confirmButton, cancelButton) =
                GetOrCreateConfirmPanel(panel.transform, cardSprite, regularFont, boldFont);

            var controllerGO = GameObject.Find("SpaceSelectController");
            if (controllerGO == null) controllerGO = new GameObject("SpaceSelectController");
            var controller = controllerGO.GetComponent<SpaceSelectController>();
            if (controller == null) controller = controllerGO.AddComponent<SpaceSelectController>();

            SetField(controller, "panel", panel);
            SetField(controller, "listContainer", listContainer);
            SetField(controller, "rowPrefab", rowPrefab);
            SetField(controller, "newSpaceNameField", nameField);
            SetField(controller, "createButton", createButton);
            SetField(controller, "closeButton", closeButton);
            SetField(controller, "statusText", statusText);
            SetField(controller, "fpsController", fpsController);
            SetField(controller, "nodeManager", nodeManagerGO.GetComponent<NodeManager>());
            SetField(controller, "confirmDeletePanel", confirmPanel);
            SetField(controller, "confirmDeleteText", confirmText);
            SetField(controller, "confirmDeleteButton", confirmButton);
            SetField(controller, "cancelDeleteButton", cancelButton);

            var authGO = GameObject.Find("AuthStartupController");
            if (authGO != null) SetField(authGO.GetComponent<AuthStartupController>(), "spaceSelect", controller);

            var helpGO = GameObject.Find("HelpController");
            if (helpGO != null) SetField(controller, "helpController", helpGO.GetComponent<HelpController>());

            var interactor = playerGO.GetComponent<Nodia.Interaction.PlayerInteractor>();
            if (interactor != null) SetField(interactor, "spaceSelect", controller);

            // ConnectionManager reads nodeManager.CurrentSpaceId when creating a
            // connection - this reference was never wired up until now.
            var connectionManagerGO = GameObject.Find("ConnectionManager");
            if (connectionManagerGO != null)
            {
                SetField(connectionManagerGO.GetComponent<ConnectionManager>(), "nodeManager", nodeManagerGO.GetComponent<NodeManager>());
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("NODIA: space select screen ready - opened from the main menu (Tab).");
        }

        private static GameObject GetOrCreatePanel(Transform canvas, Sprite cardSprite, Sprite dotSprite, TMP_FontAsset boldFont)
        {
            var existing = canvas.Find("SpaceSelectPanel");
            var panel = existing != null ? existing.gameObject
                : new GameObject("SpaceSelectPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas, false);
            panel.transform.SetAsLastSibling();

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(640, 680);
            rect.anchoredPosition = Vector2.zero;

            var image = panel.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = PanelBg;

            var dotTransform = panel.transform.Find("HeaderDot");
            var dotGO = dotTransform != null ? dotTransform.gameObject
                : new GameObject("HeaderDot", typeof(RectTransform), typeof(Image));
            dotGO.transform.SetParent(panel.transform, false);
            var dotRect = dotGO.GetComponent<RectTransform>();
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(10, 10);
            dotRect.anchoredPosition = new Vector2(0, 300);
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
            headerRect.anchoredPosition = new Vector2(0, 270);
            var headerText = headerGO.GetComponent<TextMeshProUGUI>();
            if (headerText == null) headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = "スペースを選択";
            headerText.font = boldFont;
            headerText.fontSize = 20;
            headerText.color = TextColor;
            headerText.alignment = TextAlignmentOptions.Center;

            return panel;
        }

        private static Transform GetOrCreateListContainer(Transform panel)
        {
            var existing = panel.Find("SpaceListContainer");
            var containerGO = existing != null ? existing.gameObject
                : new GameObject("SpaceListContainer", typeof(RectTransform));
            containerGO.transform.SetParent(panel, false);

            var rect = containerGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-80, 0);
            rect.anchoredPosition = new Vector2(0, 210);

            var layout = containerGO.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = containerGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            var fitter = containerGO.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = containerGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return containerGO.transform;
        }

        private static TMP_InputField GetOrCreateField(Transform panel, Sprite cardSprite, TMP_FontAsset regularFont)
        {
            var existing = panel.Find("NewSpaceNameField");
            var fieldGO = existing != null ? existing.gameObject
                : new GameObject("NewSpaceNameField", typeof(RectTransform), typeof(Image));
            fieldGO.transform.SetParent(panel, false);

            var rect = fieldGO.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(350, 56);
            rect.anchoredPosition = new Vector2(-105, -250);

            var image = fieldGO.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = FieldBg;

            var textTransform = fieldGO.transform.Find("Text");
            var textGO = textTransform != null ? textTransform.gameObject : new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(fieldGO.transform, false);
            SetStretchRect(textGO.GetComponent<RectTransform>(), new Vector2(18, 8), new Vector2(-18, -8));
            var text = textGO.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textGO.AddComponent<TextMeshProUGUI>();
            text.font = regularFont;
            text.fontSize = 18;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.MidlineLeft;

            var placeholderTransform = fieldGO.transform.Find("Placeholder");
            var placeholderGO = placeholderTransform != null ? placeholderTransform.gameObject
                : new GameObject("Placeholder", typeof(RectTransform));
            placeholderGO.transform.SetParent(fieldGO.transform, false);
            SetStretchRect(placeholderGO.GetComponent<RectTransform>(), new Vector2(18, 8), new Vector2(-18, -8));
            var placeholder = placeholderGO.GetComponent<TextMeshProUGUI>();
            if (placeholder == null) placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholder.font = regularFont;
            placeholder.fontSize = 18;
            placeholder.color = MutedText;
            placeholder.text = "新しいスペース名";
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;

            var input = fieldGO.GetComponent<TMP_InputField>();
            if (input == null) input = fieldGO.AddComponent<TMP_InputField>();
            input.textViewport = rect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;

            return input;
        }

        private static Button GetOrCreateButton(Transform panel, Sprite cardSprite, TMP_FontAsset boldFont)
        {
            var existing = panel.Find("CreateSpaceButton");
            var go = existing != null ? existing.gameObject
                : new GameObject("CreateSpaceButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(160, 56);
            rect.anchoredPosition = new Vector2(170, -250);

            var image = go.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = Accent;

            var textTransform = go.transform.Find("Text");
            var textGO = textTransform != null ? textTransform.gameObject : new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            SetStretchRect(textGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var text = textGO.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "新規作成";
            text.font = boldFont;
            text.fontSize = 18;
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.Center;

            return go.GetComponent<Button>();
        }

        private static Button GetOrCreateCloseButton(Transform panel, Sprite cardSprite, TMP_FontAsset boldFont)
        {
            var existing = panel.Find("CloseButton");
            var go = existing != null ? existing.gameObject
                : new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(44, 44);
            rect.anchoredPosition = new Vector2(-20, -20);

            var image = go.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.06f);

            var textTransform = go.transform.Find("Text");
            var textGO = textTransform != null ? textTransform.gameObject : new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            SetStretchRect(textGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var text = textGO.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "×";
            text.font = boldFont;
            text.fontSize = 22;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.Center;

            return go.GetComponent<Button>();
        }

        // A row is a plain container with two overlapping buttons: SelectButton
        // fills most of the width (enters the space), DeleteButton is a small
        // square pinned to the right edge on top of it (so clicks there hit
        // delete, not select).
        private static GameObject GetOrCreateSpaceRowPrefab(Sprite cardSprite, TMP_FontAsset regularFont, TMP_FontAsset boldFont)
        {
            const string path = "Assets/Prefabs/SpaceRow.prefab";
            AssetDatabase.DeleteAsset(path);

            var root = new GameObject("SpaceRow", typeof(RectTransform), typeof(LayoutElement));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.sizeDelta = new Vector2(0f, 56f);
            var rootLayout = root.GetComponent<LayoutElement>();
            rootLayout.preferredHeight = 56f;
            rootLayout.minHeight = 56f;

            var selectGO = new GameObject("SelectButton", typeof(RectTransform), typeof(Image), typeof(Button));
            selectGO.transform.SetParent(root.transform, false);
            var selectRect = selectGO.GetComponent<RectTransform>();
            selectRect.anchorMin = Vector2.zero;
            selectRect.anchorMax = Vector2.one;
            selectRect.offsetMin = Vector2.zero;
            selectRect.offsetMax = new Vector2(-60, 0);
            var selectImage = selectGO.GetComponent<Image>();
            selectImage.sprite = cardSprite;
            selectImage.type = Image.Type.Sliced;
            selectImage.color = FieldBg;

            var labelGO = new GameObject("Text", typeof(RectTransform));
            labelGO.transform.SetParent(selectGO.transform, false);
            SetStretchRect(labelGO.GetComponent<RectTransform>(), new Vector2(18, 6), new Vector2(-18, -6));
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.font = regularFont;
            label.fontSize = 18;
            label.color = TextColor;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.textWrappingMode = TextWrappingModes.NoWrap;

            var deleteGO = new GameObject("DeleteButton", typeof(RectTransform), typeof(Image), typeof(Button));
            deleteGO.transform.SetParent(root.transform, false);
            var deleteRect = deleteGO.GetComponent<RectTransform>();
            deleteRect.anchorMin = deleteRect.anchorMax = new Vector2(1f, 0.5f);
            deleteRect.sizeDelta = new Vector2(48, 48);
            deleteRect.anchoredPosition = new Vector2(-26, 0);
            var deleteImage = deleteGO.GetComponent<Image>();
            deleteImage.color = new Color(0.45f, 0.2f, 0.2f, 0.7f);

            var deleteLabelGO = new GameObject("Text", typeof(RectTransform));
            deleteLabelGO.transform.SetParent(deleteGO.transform, false);
            SetStretchRect(deleteLabelGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var deleteLabel = deleteLabelGO.AddComponent<TextMeshProUGUI>();
            deleteLabel.text = "×";
            deleteLabel.font = boldFont;
            deleteLabel.fontSize = 20;
            deleteLabel.color = Color.white;
            deleteLabel.alignment = TextAlignmentOptions.Center;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // A small modal-within-the-modal that blocks accidental deletes.
        private static (GameObject, TMP_Text, Button, Button) GetOrCreateConfirmPanel(
            Transform parentPanel, Sprite cardSprite, TMP_FontAsset regularFont, TMP_FontAsset boldFont)
        {
            var existing = parentPanel.Find("ConfirmDeletePanel");
            var go = existing != null ? existing.gameObject
                : new GameObject("ConfirmDeletePanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parentPanel, false);
            go.transform.SetAsLastSibling();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(480, 260);
            rect.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.08f, 0.06f, 0.06f, 0.99f);

            var textTransform = go.transform.Find("Text");
            var textGO = textTransform != null ? textTransform.gameObject : new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.sizeDelta = new Vector2(-48, 130);
            textRect.anchoredPosition = new Vector2(0, -30);
            var text = textGO.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textGO.AddComponent<TextMeshProUGUI>();
            text.font = regularFont;
            text.fontSize = 18;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.Top;

            var confirmButton = GetOrCreateModalButton(go.transform, "ConfirmDeleteButton", cardSprite, boldFont,
                "削除する", new Vector2(-115, -90), Danger, Color.black);
            var cancelButton = GetOrCreateModalButton(go.transform, "CancelDeleteButton", cardSprite, boldFont,
                "キャンセル", new Vector2(115, -90), Neutral, TextColor);

            return (go, text, confirmButton, cancelButton);
        }

        private static Button GetOrCreateModalButton(Transform parent, string name, Sprite cardSprite,
            TMP_FontAsset boldFont, string label, Vector2 anchoredPos, Color bg, Color textColor)
        {
            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200, 54);
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
