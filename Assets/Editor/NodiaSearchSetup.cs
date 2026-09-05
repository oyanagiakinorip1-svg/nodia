using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Nodia.Interaction;
using Nodia.Nodes;
using Nodia.Player;
using Nodia.UI;

namespace Nodia.EditorTools
{
    // Builds the node search overlay (F to open, type to filter, click a
    // result to jump) and wires it into PlayerInteractor. Reuses the rounded
    // card sprite and Noto Sans JP TMP fonts from Nodia > Style Note UI, so
    // run that first. Safe to run more than once.
    public static class NodiaSearchSetup
    {
        private static readonly Color PanelBg = new Color(0.118f, 0.118f, 0.133f, 0.99f);
        private static readonly Color FieldBg = new Color(1f, 1f, 1f, 0.05f);
        private static readonly Color RowBg = new Color(1f, 1f, 1f, 0.045f);
        private static readonly Color Accent = new Color(0.42f, 0.56f, 0.96f);
        private static readonly Color TextColor = new Color(0.90f, 0.90f, 0.92f);
        private static readonly Color MutedText = new Color(0.52f, 0.53f, 0.57f);

        [MenuItem("Nodia/Setup Search")]
        public static void SetupSearch()
        {
            var canvas = GameObject.Find("NoteCanvas");
            var nodeManagerGO = GameObject.Find("NodeManager");
            var playerGO = GameObject.Find("Player");
            var interactor = playerGO != null ? playerGO.GetComponent<PlayerInteractor>() : null;
            var fpsController = playerGO != null ? playerGO.GetComponent<FPSController>() : null;

            if (canvas == null || nodeManagerGO == null || interactor == null || fpsController == null)
            {
                Debug.LogError("NODIA: NoteCanvas/NodeManager/Player not found - run Nodia > Setup Scene first.");
                return;
            }

            var cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/UI/Card.png");
            var dotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/UI/Dot.png");
            var regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansJP-Regular SDF.asset");
            var boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansJP-Bold SDF.asset");
            if (cardSprite == null || dotSprite == null || regularFont == null || boldFont == null)
            {
                Debug.LogError("NODIA: shared UI assets not found - run Nodia > Style Note UI first (it creates the " +
                                "rounded card sprite and the Noto Sans JP TMP fonts this reuses).");
                return;
            }

            var resultRowPrefab = GetOrCreateResultRowPrefab(cardSprite, regularFont);
            var panel = GetOrCreatePanel(canvas.transform, cardSprite, dotSprite, boldFont);
            var queryField = GetOrCreateQueryField(panel.transform, cardSprite, regularFont);
            var resultsContainer = GetOrCreateResultsContainer(panel.transform);
            GetOrCreateHint(panel.transform, regularFont);

            var controllerGO = GameObject.Find("NodeSearchController");
            if (controllerGO == null) controllerGO = new GameObject("NodeSearchController");
            var controller = controllerGO.GetComponent<NodeSearchController>();
            if (controller == null) controller = controllerGO.AddComponent<NodeSearchController>();

            SetField(controller, "panel", panel);
            SetField(controller, "queryField", queryField);
            SetField(controller, "resultsContainer", resultsContainer);
            SetField(controller, "resultRowPrefab", resultRowPrefab);
            SetField(controller, "nodeManager", nodeManagerGO.GetComponent<NodeManager>());
            SetField(controller, "fpsController", fpsController);
            SetField(controller, "playerTransform", playerGO.transform);
            SetField(controller, "playerController", playerGO.GetComponent<CharacterController>());

            SetField(interactor, "searchController", controller);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("NODIA: search overlay ready - opened from the main menu (Tab).");
        }

        private static GameObject GetOrCreatePanel(Transform canvas, Sprite cardSprite, Sprite dotSprite, TMP_FontAsset boldFont)
        {
            var existing = canvas.Find("SearchPanel");
            var panel = existing != null ? existing.gameObject
                : new GameObject("SearchPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas, false);

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(640, 560);
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
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(0f, 1f);
            dotRect.pivot = new Vector2(0f, 0.5f);
            dotRect.sizeDelta = new Vector2(10, 10);
            dotRect.anchoredPosition = new Vector2(36, -44);
            var dotImage = dotGO.GetComponent<Image>();
            dotImage.sprite = dotSprite;
            dotImage.type = Image.Type.Sliced;
            dotImage.color = Accent;

            var headerTransform = panel.transform.Find("HeaderLabelTMP");
            var headerGO = headerTransform != null ? headerTransform.gameObject
                : new GameObject("HeaderLabelTMP", typeof(RectTransform));
            headerGO.transform.SetParent(panel.transform, false);
            var headerRect = headerGO.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.sizeDelta = new Vector2(-100, 26);
            headerRect.anchoredPosition = new Vector2(54, -36);
            var headerText = headerGO.GetComponent<TextMeshProUGUI>();
            if (headerText == null) headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = "Search nodes";
            headerText.font = boldFont;
            headerText.fontSize = 15;
            headerText.color = MutedText;
            headerText.alignment = TextAlignmentOptions.TopLeft;

            return panel;
        }

        private static TMP_InputField GetOrCreateQueryField(Transform panel, Sprite cardSprite, TMP_FontAsset regularFont)
        {
            var existing = panel.Find("QueryField");
            var fieldGO = existing != null ? existing.gameObject
                : new GameObject("QueryField", typeof(RectTransform), typeof(Image));
            fieldGO.transform.SetParent(panel, false);

            var rect = fieldGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(560, 60);
            rect.anchoredPosition = new Vector2(0, 210);

            var image = fieldGO.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = FieldBg;

            var textTransform = fieldGO.transform.Find("Text");
            var textGO = textTransform != null ? textTransform.gameObject : new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(fieldGO.transform, false);
            SetStretchRect(textGO.GetComponent<RectTransform>(), new Vector2(22, 10), new Vector2(-22, -10));
            var text = textGO.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textGO.AddComponent<TextMeshProUGUI>();
            text.font = regularFont;
            text.fontSize = 22;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.MidlineLeft;

            var placeholderTransform = fieldGO.transform.Find("Placeholder");
            var placeholderGO = placeholderTransform != null ? placeholderTransform.gameObject
                : new GameObject("Placeholder", typeof(RectTransform));
            placeholderGO.transform.SetParent(fieldGO.transform, false);
            SetStretchRect(placeholderGO.GetComponent<RectTransform>(), new Vector2(22, 10), new Vector2(-22, -10));
            var placeholder = placeholderGO.GetComponent<TextMeshProUGUI>();
            if (placeholder == null) placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholder.font = regularFont;
            placeholder.fontSize = 22;
            placeholder.color = MutedText;
            placeholder.text = "タイトル・本文で検索…";
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

        private static Transform GetOrCreateResultsContainer(Transform panel)
        {
            var existing = panel.Find("ResultsContainer");
            var containerGO = existing != null ? existing.gameObject
                : new GameObject("ResultsContainer", typeof(RectTransform));
            containerGO.transform.SetParent(panel, false);

            var rect = containerGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-80, 0);
            rect.anchoredPosition = new Vector2(0, -110);

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

        private static void GetOrCreateHint(Transform panel, TMP_FontAsset regularFont)
        {
            var existing = panel.Find("Hint");
            var hintGO = existing != null ? existing.gameObject : new GameObject("Hint", typeof(RectTransform));
            hintGO.transform.SetParent(panel, false);

            var rect = hintGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(-40, 30);
            rect.anchoredPosition = new Vector2(0, 24);

            var text = hintGO.GetComponent<TextMeshProUGUI>();
            if (text == null) text = hintGO.AddComponent<TextMeshProUGUI>();
            text.text = "Tab で閉じる";
            text.font = regularFont;
            text.fontSize = 14;
            text.color = MutedText;
            text.alignment = TextAlignmentOptions.Center;
        }

        private static GameObject GetOrCreateResultRowPrefab(Sprite cardSprite, TMP_FontAsset regularFont)
        {
            const string path = "Assets/Prefabs/SearchResultRow.prefab";
            AssetDatabase.DeleteAsset(path);

            var go = new GameObject("SearchResultRow", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rect = go.GetComponent<RectTransform>();
            // Top-stretch anchor is the configuration VerticalLayoutGroup
            // expects from its children; LayoutElement.preferredHeight is
            // what it actually reads for stacking when childControlHeight is
            // off (relying on sizeDelta.y alone left every row stacked at 0).
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 52f);

            var layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 52f;
            layoutElement.minHeight = 52f;

            var image = go.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = RowBg;

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            SetStretchRect(textGO.GetComponent<RectTransform>(), new Vector2(18, 6), new Vector2(-18, -6));
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.font = regularFont;
            text.fontSize = 18;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
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
