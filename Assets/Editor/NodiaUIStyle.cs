using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Nodia.UI;

namespace Nodia.EditorTools
{
    // Restyles the note UI toward a Notion/Obsidian-like dark, minimal look
    // (soft layered shadow, muted palette, TextMeshPro + an embedded Noto
    // Sans JP so it renders crisply and also works in a WebGL build).
    // Rebuilds the InputField/Text children as TMP components each run -
    // safe to run more than once, but do expect it to recreate those
    // children rather than leave old legacy-UI ones behind.
    public static class NodiaUIStyle
    {
        private const string SpriteFolder = "Assets/Prefabs/UI";
        private const string TmpFontFolder = "Assets/Fonts/TMP";
        private const int SpriteSize = 72;
        private const int SpriteRadius = 18;

        private static readonly Color PanelBg = new Color(0.118f, 0.118f, 0.133f, 0.99f);
        private static readonly Color FieldBg = new Color(1f, 1f, 1f, 0.045f);
        private static readonly Color Accent = new Color(0.42f, 0.56f, 0.96f);
        private static readonly Color Danger = new Color(0.82f, 0.45f, 0.44f);
        private static readonly Color Neutral = new Color(0.21f, 0.21f, 0.23f);
        private static readonly Color TextColor = new Color(0.90f, 0.90f, 0.92f);
        private static readonly Color MutedText = new Color(0.52f, 0.53f, 0.57f);

        [MenuItem("Nodia/Style Note UI")]
        public static void StyleNoteUI()
        {
            var canvas = GameObject.Find("NoteCanvas");
            var panel = GameObject.Find("NoteCanvas/NotePanel");
            var controllerGO = GameObject.Find("NoteUIController");
            if (canvas == null || panel == null || controllerGO == null)
            {
                Debug.LogError("NODIA: NoteCanvas/NotePanel/NoteUIController not found - run Nodia > Setup Scene first.");
                return;
            }

            var regularSource = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansJP-Regular.ttf");
            var boldSource = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansJP-Bold.ttf");
            if (regularSource == null || boldSource == null)
            {
                Debug.LogError("NODIA: Assets/Fonts/NotoSansJP-Regular.ttf / NotoSansJP-Bold.ttf not found. " +
                                "Wait for Unity to finish importing them, then run this again.");
                return;
            }

            EnsureFolder(TmpFontFolder);
            var regularFont = GetOrCreateTmpFont(regularSource, $"{TmpFontFolder}/NotoSansJP-Regular SDF.asset");
            var boldFont = GetOrCreateTmpFont(boldSource, $"{TmpFontFolder}/NotoSansJP-Bold SDF.asset");
            if (regularFont == null || boldFont == null)
            {
                Debug.LogError("NODIA: failed to create TMP font assets - is the TextMeshPro package installed? " +
                                "Run Window > TextMeshPro > Import TMP Essential Resources first.");
                return;
            }

            var cardSprite = GetOrCreateRoundedSprite($"{SpriteFolder}/Card.png", SpriteSize, SpriteRadius);
            var dotSprite = GetOrCreateRoundedSprite($"{SpriteFolder}/Dot.png", 16, 8);

            ResizePanel(panel);
            RemoveObsoleteChild(panel.transform, "AccentStripe");
            var shadowGroup = StyleSoftShadow(canvas.transform, panel.transform, cardSprite);
            StylePanelBackground(panel, cardSprite);
            StyleHeader(panel.transform, boldFont, dotSprite);

            var titleField = RebuildField(panel.transform.Find("TitleField"), cardSprite, regularFont, boldFont,
                new Vector2(0, 200), new Vector2(760, 64), "タイトルを入力", 26, true, false);
            var contentField = RebuildField(panel.transform.Find("ContentField"), cardSprite, regularFont, boldFont,
                new Vector2(0, -15), new Vector2(760, 320), "ここにメモを書く…", 20, false, true);

            RebuildButtonLabel(panel.transform.Find("SaveButton"), cardSprite, boldFont,
                new Vector2(-240, -300), Accent, Color.black, "保存");
            RebuildButtonLabel(panel.transform.Find("CloseButton"), cardSprite, boldFont,
                new Vector2(0, -300), Neutral, TextColor, "閉じる");
            RebuildButtonLabel(panel.transform.Find("DeleteButton"), cardSprite, boldFont,
                new Vector2(240, -300), Danger, Color.white, "削除");

            var noteController = controllerGO.GetComponent<NoteUIController>();
            SetField(noteController, "titleField", titleField);
            SetField(noteController, "contentField", contentField);
            SetField(noteController, "shadowGroup", shadowGroup);

            // Match the panel's current shown/hidden state now that the shadow
            // group is a separate, previously-always-visible object.
            shadowGroup.SetActive(panel.activeSelf);

            StyleLineMaterial();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("NODIA: note UI restyled with TextMeshPro + Noto Sans JP.");
        }

        private static void RemoveObsoleteChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        private static void ResizePanel(GameObject panel)
        {
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 700);
        }

        private static void StylePanelBackground(GameObject panel, Sprite sprite)
        {
            var image = panel.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = PanelBg;
        }

        // Three stacked, growing, fading copies behind the panel fake a soft
        // gaussian-blurred drop shadow without needing an actual blur shader.
        // They live under one group GameObject so NoteUIController can show
        // and hide them together with the panel (they're siblings of it, not
        // children, so a shadow can render behind the panel's own background).
        private static GameObject StyleSoftShadow(Transform canvas, Transform panel, Sprite sprite)
        {
            // Clean up every earlier shadow generation - the original single
            // flat shadow, and the ungrouped 3-layer version - all of which
            // were direct children of the canvas and nothing ever hid them.
            RemoveObsoleteChild(canvas, "PanelShadow");
            RemoveObsoleteChild(canvas, "PanelShadow0");
            RemoveObsoleteChild(canvas, "PanelShadow1");
            RemoveObsoleteChild(canvas, "PanelShadow2");

            var groupTransform = canvas.Find("PanelShadowGroup");
            var group = groupTransform != null ? groupTransform.gameObject
                : new GameObject("PanelShadowGroup", typeof(RectTransform));
            group.transform.SetParent(canvas, false);
            group.transform.SetSiblingIndex(panel.GetSiblingIndex());
            var groupRect = group.GetComponent<RectTransform>();
            groupRect.anchorMin = Vector2.zero;
            groupRect.anchorMax = Vector2.one;
            groupRect.offsetMin = Vector2.zero;
            groupRect.offsetMax = Vector2.zero;

            var panelRect = panel.GetComponent<RectTransform>();
            var layers = new (float growth, float offsetY, float alpha)[]
            {
                (48f, -6f, 0.07f),
                (28f, -4f, 0.10f),
                (12f, -2f, 0.14f),
            };

            for (var i = 0; i < layers.Length; i++)
            {
                var name = $"PanelShadow{i}";
                var existing = group.transform.Find(name);
                GameObject go = existing != null ? existing.gameObject
                    : new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(group.transform, false);

                var (growth, offsetY, alpha) = layers[i];
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = panelRect.anchorMin;
                rect.anchorMax = panelRect.anchorMax;
                rect.pivot = panelRect.pivot;
                rect.sizeDelta = panelRect.sizeDelta + new Vector2(growth, growth);
                rect.anchoredPosition = panelRect.anchoredPosition + new Vector2(0f, offsetY);

                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = new Color(0f, 0f, 0f, alpha);
                // Purely decorative and larger than the panel itself (it
                // grows outward on every side) - without this it sits in
                // front of the panel in click/raycast order and silently
                // eats every click meant for the title/content fields and
                // Save/Close/Delete buttons underneath.
                image.raycastTarget = false;
            }

            return group;
        }

        private static void StyleHeader(Transform panel, TMP_FontAsset boldFont, Sprite dotSprite)
        {
            var dot = panel.Find("HeaderDot");
            GameObject dotGO = dot != null ? dot.gameObject
                : new GameObject("HeaderDot", typeof(RectTransform), typeof(Image));
            dotGO.transform.SetParent(panel, false);
            var dotRect = dotGO.GetComponent<RectTransform>();
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(0f, 1f);
            dotRect.pivot = new Vector2(0f, 0.5f);
            dotRect.sizeDelta = new Vector2(10, 10);
            dotRect.anchoredPosition = new Vector2(36, -44);
            var dotImage = dotGO.GetComponent<Image>();
            dotImage.sprite = dotSprite;
            dotImage.type = Image.Type.Sliced;
            dotImage.color = Accent;

            var oldHeader = panel.Find("HeaderLabel");
            if (oldHeader != null) Object.DestroyImmediate(oldHeader.gameObject);

            var headerTmp = panel.Find("HeaderLabelTMP");
            GameObject go = headerTmp != null ? headerTmp.gameObject
                : new GameObject("HeaderLabelTMP", typeof(RectTransform));
            go.transform.SetParent(panel, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(-100, 26);
            rect.anchoredPosition = new Vector2(54, -36);

            var text = go.GetComponent<TextMeshProUGUI>();
            if (text == null) text = go.AddComponent<TextMeshProUGUI>();
            text.text = "Nodia memo";
            text.font = boldFont;
            text.fontSize = 15;
            text.color = MutedText;
            text.alignment = TextAlignmentOptions.TopLeft;
        }

        private static TMP_InputField RebuildField(Transform field, Sprite cardSprite, TMP_FontAsset regularFont,
            TMP_FontAsset boldFont, Vector2 anchoredPos, Vector2 size, string placeholderText, float fontSize,
            bool bold, bool multiline)
        {
            if (field == null) return null;

            var oldInput = field.GetComponent<InputField>();
            if (oldInput != null) Object.DestroyImmediate(oldInput);

            // Only tear down "Text"/"Placeholder" if they're leftover legacy
            // Text children - if they're already TMP (from an earlier run of
            // this same method), reuse them so the TMP_InputField's internal
            // state doesn't get rebuilt out from under it on every re-run.
            var textChild = field.Find("Text");
            if (textChild != null && textChild.GetComponent<TextMeshProUGUI>() == null)
            {
                Object.DestroyImmediate(textChild.gameObject);
                textChild = null;
            }
            var placeholderChild = field.Find("Placeholder");
            if (placeholderChild != null && placeholderChild.GetComponent<TextMeshProUGUI>() == null)
            {
                Object.DestroyImmediate(placeholderChild.gameObject);
                placeholderChild = null;
            }

            var rect = field.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            var image = field.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = FieldBg;

            var textGO = textChild != null ? textChild.gameObject : new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(field, false);
            SetStretchRect(textGO.GetComponent<RectTransform>(), new Vector2(22, 12), new Vector2(-22, -12));
            var text = textGO.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textGO.AddComponent<TextMeshProUGUI>();
            text.font = bold ? boldFont : regularFont;
            text.fontSize = fontSize;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.TopLeft;

            var placeholderGO = placeholderChild != null ? placeholderChild.gameObject : new GameObject("Placeholder", typeof(RectTransform));
            placeholderGO.transform.SetParent(field, false);
            SetStretchRect(placeholderGO.GetComponent<RectTransform>(), new Vector2(22, 12), new Vector2(-22, -12));
            var placeholder = placeholderGO.GetComponent<TextMeshProUGUI>();
            if (placeholder == null) placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholder.font = regularFont;
            placeholder.fontSize = fontSize;
            placeholder.color = MutedText;
            placeholder.text = placeholderText;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.alignment = TextAlignmentOptions.TopLeft;

            var input = field.GetComponent<TMP_InputField>();
            if (input == null) input = field.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = rect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;

            return input;
        }

        private static void RebuildButtonLabel(Transform button, Sprite cardSprite, TMP_FontAsset boldFont,
            Vector2 anchoredPos, Color bg, Color textColor, string label)
        {
            if (button == null) return;

            var rect = button.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(210, 58);
            rect.anchoredPosition = anchoredPos;

            var image = button.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = bg;

            var oldText = button.GetComponentInChildren<Text>();
            if (oldText != null) Object.DestroyImmediate(oldText.gameObject);

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            GameObject textGO;
            if (tmp != null)
            {
                textGO = tmp.gameObject;
            }
            else
            {
                textGO = new GameObject("Text", typeof(RectTransform));
                textGO.transform.SetParent(button, false);
                SetStretchRect(textGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            }

            var text = textGO.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.font = boldFont;
            text.fontSize = 20;
            text.color = textColor;
            text.alignment = TextAlignmentOptions.Center;
        }

        private static void SetStretchRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void StyleLineMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/LineMaterial.mat");
            if (material == null) return;
            // Pushed past 1.0 (HDR) so the connection lines bloom against the
            // dark void background instead of just looking flat pale blue.
            material.color = new Color(1.1f, 1.7f, 2f, 0.9f);
            EditorUtility.SetDirty(material);
        }

        private static TMP_FontAsset GetOrCreateTmpFont(Font sourceFont, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            if (fontAsset == null) return null;

            AssetDatabase.CreateAsset(fontAsset, assetPath);
            if (fontAsset.atlasTexture != null) AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            if (fontAsset.material != null) AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        }

        private static Sprite GetOrCreateRoundedSprite(string path, int size, int radius)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, RoundedRectAlpha(x, y, size, radius)));
                }
            }
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = new Vector4(radius, radius, radius, radius);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static float RoundedRectAlpha(int x, int y, int size, int radius)
        {
            var px = x + 0.5f;
            var py = y + 0.5f;
            var dx = Mathf.Max(radius - px, px - (size - radius), 0f);
            var dy = Mathf.Max(radius - py, py - (size - radius), 0f);
            var dist = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(radius - dist + 0.5f);
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

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folderName = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
