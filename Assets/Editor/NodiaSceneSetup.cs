using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Nodia.Networking;
using Nodia.Player;
using Nodia.Nodes;
using Nodia.UI;
using Nodia.Interaction;

namespace Nodia.EditorTools
{
    // One-click scene assembly so the manual GameObject/prefab wiring in
    // SETUP.md doesn't have to be done by hand under a 2-day deadline.
    // Run once via Nodia > Setup Scene, then fill in the Supabase/API keys
    // in the Inspector and press Play.
    public static class NodiaSceneSetup
    {
        private const string PrefabFolder = "Assets/Prefabs";

        [MenuItem("Nodia/Setup Scene")]
        public static void SetupScene()
        {
            if (GameObject.Find("Player") != null)
            {
                Debug.LogWarning("NODIA: 'Player' already exists in the scene - aborting so setup doesn't run twice. Delete Player/the manager objects/NoteCanvas first if you want to regenerate.");
                return;
            }

            EnsureFolder(PrefabFolder);

            var nodePrefab = CreateNodePrefab();
            var linePrefab = CreateLinePrefab();

            var auth = CreateManager<SupabaseAuth>("SupabaseAuth");
            var api = CreateManager<ApiClient>("ApiClient");
            var connectionManager = CreateManager<ConnectionManager>("ConnectionManager");
            var nodeManager = CreateManager<NodeManager>("NodeManager");
            var nodesParent = new GameObject("Nodes").transform;

            SetField(connectionManager, "linePrefab", linePrefab);
            SetField(nodeManager, "nodePrefab", nodePrefab);
            SetField(nodeManager, "nodesParent", nodesParent);
            SetField(nodeManager, "connectionManager", connectionManager);

            CreatePlayer(out var fpsController, out var camera, out var interactor);
            var noteUI = CreateNoteUI(nodeManager, interactor);

            SetField(fpsController, "playerCamera", camera);
            SetField(interactor, "playerCamera", camera);
            SetField(interactor, "fpsController", fpsController);
            SetField(interactor, "noteUI", noteUI);
            SetField(interactor, "connectionManager", connectionManager);
            SetField(interactor, "nodeManager", nodeManager);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("NODIA: scene setup complete. Set the Supabase URL/anon key on SupabaseAuth " +
                      "and the API base URL on ApiClient in the Inspector, then press Play. " +
                      "(_unused variable warnings for 'auth'/'api' are expected - they're wired via Inspector fields, not code.)");
        }

        private static GameObject CreateNodePrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "NodePrefab";
            go.transform.localScale = Vector3.one * 0.6f;
            var view = go.AddComponent<NodeView>();
            SetField(view, "nodeRenderer", go.GetComponent<MeshRenderer>());
            return SaveAndDestroy(go, $"{PrefabFolder}/NodePrefab.prefab");
        }

        private static GameObject CreateLinePrefab()
        {
            var go = new GameObject("LinePrefab");
            var line = go.AddComponent<LineRenderer>();
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;
            line.useWorldSpace = true;

            var materialPath = $"{PrefabFolder}/LineMaterial.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            line.material = material;

            return SaveAndDestroy(go, $"{PrefabFolder}/LinePrefab.prefab");
        }

        private static void CreatePlayer(out FPSController fps, out Camera camera, out PlayerInteractor interactor)
        {
            var player = new GameObject("Player", typeof(CharacterController));
            player.transform.position = new Vector3(0f, 1f, -5f);
            fps = player.AddComponent<FPSController>();
            interactor = player.AddComponent<PlayerInteractor>();

            var existingCamera = Camera.main;
            GameObject camGO;
            if (existingCamera != null)
            {
                camGO = existingCamera.gameObject;
                camGO.transform.SetParent(player.transform, false);
            }
            else
            {
                camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camGO.tag = "MainCamera";
                camGO.transform.SetParent(player.transform, false);
            }

            camGO.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            camGO.transform.localRotation = Quaternion.identity;
            camera = camGO.GetComponent<Camera>();
        }

        private static T CreateManager<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            return go.AddComponent<T>();
        }

        private static NoteUIController CreateNoteUI(NodeManager nodeManager, PlayerInteractor interactor)
        {
            var canvasGO = new GameObject("NoteCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            var panel = new GameObject("NotePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasGO.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560, 420);
            panelRect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

            var titleField = CreateInputField(panel.transform, "TitleField", new Vector2(0, 160), new Vector2(480, 50), false);
            var contentField = CreateInputField(panel.transform, "ContentField", new Vector2(0, 20), new Vector2(480, 220), true);

            var saveButton = CreateButton(panel.transform, "SaveButton", "保存", new Vector2(-160, -160));
            var closeButton = CreateButton(panel.transform, "CloseButton", "閉じる", new Vector2(0, -160));
            var deleteButton = CreateButton(panel.transform, "DeleteButton", "削除", new Vector2(160, -160));

            var controller = new GameObject("NoteUIController").AddComponent<NoteUIController>();
            SetField(controller, "panel", panel);
            SetField(controller, "titleField", titleField);
            SetField(controller, "contentField", contentField);
            SetField(controller, "saveButton", saveButton);
            SetField(controller, "closeButton", closeButton);
            SetField(controller, "deleteButton", deleteButton);
            SetField(controller, "nodeManager", nodeManager);
            SetField(controller, "interactor", interactor);

            return controller;
        }

        private static InputField CreateInputField(Transform parent, string name, Vector2 anchoredPos, Vector2 size, bool multiline)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 6);
            textRect.offsetMax = new Vector2(-10, -6);
            var text = textGO.GetComponent<Text>();
            text.font = DefaultFont();
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;

            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            if (multiline)
            {
                input.lineType = InputField.LineType.MultiLineNewline;
            }

            return input;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140, 44);
            rect.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f, 1f);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGO.GetComponent<Text>();
            text.text = label;
            text.font = DefaultFont();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;

            return go.GetComponent<Button>();
        }

        private static GameObject SaveAndDestroy(GameObject sceneInstance, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(sceneInstance, path);
            Object.DestroyImmediate(sceneInstance);
            return prefab;
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

        // Prefer the embedded Noto Sans JP (renders Japanese correctly in a
        // WebGL build too) and only fall back to the editor-only builtin font
        // if it hasn't been imported yet.
        private static Font DefaultFont()
        {
            var noto = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansJP-Regular.ttf");
            return noto != null ? noto : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var folderName = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
