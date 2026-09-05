using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Nodia.Nodes;
using Nodia.Player;
using Nodia.UI;

namespace Nodia.EditorTools
{
    // Pushes the 3D space toward "simple, high-visibility, not realistic"
    // per the design doc: a dark void instead of the default bright sky,
    // flat low-key lighting instead of realistic directional shadows, so the
    // emissive nodes/connections (boosted in NodeView/ConnectionManager) are
    // what actually reads in the scene.
    public static class NodiaEnvironmentStyle
    {
        // Lighter slate instead of near-black - "dark and simple" shouldn't
        // mean straining to see the space you're navigating in.
        private static readonly Color VoidColor = new Color(0.185f, 0.20f, 0.245f, 1f);
        private static readonly Color AmbientColor = new Color(0.27f, 0.285f, 0.335f, 1f);

        [MenuItem("Nodia/Style Environment")]
        public static void StyleEnvironment()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = VoidColor;
            }

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientColor;

            var light = Object.FindFirstObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                light.intensity = 0.9f;
                light.shadows = LightShadows.None;
                light.color = new Color(0.85f, 0.88f, 1f);
            }

            BoostNodePrefabBrightness();
            EnlargeNodePrefab();
            AddNodeTitleLabel();
            TunePlayerLookSensitivity();
            MakeCanvasResponsive();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("NODIA: environment restyled - lighter background, brighter/bigger nodes, faster mouse look, responsive UI scaling.");
        }

        // The Player GameObject lives directly in the scene (not a prefab),
        // so its FPSController field values were baked in at Setup Scene time
        // and need to be poked directly rather than via a script default.
        private static void TunePlayerLookSensitivity()
        {
            var player = GameObject.Find("Player");
            var fps = player != null ? player.GetComponent<FPSController>() : null;
            if (fps == null) return;

            var so = new SerializedObject(fps);
            so.FindProperty("mouseSensitivity").floatValue = 0.25f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // This is a web app, not a fixed game window - the browser window
        // gets resized to arbitrary aspect ratios, which a static
        // CanvasScaler match value handles badly at the extremes.
        private static void MakeCanvasResponsive()
        {
            var canvasGO = GameObject.Find("NoteCanvas");
            var scaler = canvasGO != null ? canvasGO.GetComponent<CanvasScaler>() : null;
            if (scaler == null) return;

            if (canvasGO.GetComponent<ResponsiveCanvasScaler>() == null)
            {
                canvasGO.AddComponent<ResponsiveCanvasScaler>();
            }
        }

        // Floating title above each node so it can be told apart from
        // others at a distance, without flying up and opening it first.
        private static void AddNodeTitleLabel()
        {
            const string path = "Assets/Prefabs/NodePrefab.prefab";
            var regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansJP-Regular SDF.asset");
            if (regularFont == null)
            {
                Debug.LogError("NODIA: Noto Sans JP TMP font not found - run Nodia > Style Note UI first.");
                return;
            }

            // Adding a child GameObject to a prefab loaded via
            // AssetDatabase.LoadAssetAtPath is disallowed (Unity blocks
            // re-parenting under a prefab asset to prevent data corruption)
            // - editing an in-memory copy of the prefab's contents and
            // saving that back is the correct way to restructure a prefab.
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var nodeView = contents.GetComponent<NodeView>();
                if (nodeView == null) return;

                var labelTransform = contents.transform.Find("TitleLabel");
                GameObject labelGO;
                if (labelTransform != null)
                {
                    labelGO = labelTransform.gameObject;
                }
                else
                {
                    labelGO = new GameObject("TitleLabel");
                    labelGO.transform.SetParent(contents.transform, false);
                }
                labelGO.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                labelGO.transform.localRotation = Quaternion.identity;
                labelGO.transform.localScale = Vector3.one;

                var tmp = labelGO.GetComponent<TextMeshPro>();
                if (tmp == null) tmp = labelGO.AddComponent<TextMeshPro>();
                tmp.font = regularFont;
                tmp.fontSize = 3.5f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);

                if (labelGO.GetComponent<Billboard>() == null) labelGO.AddComponent<Billboard>();

                var so = new SerializedObject(nodeView);
                so.FindProperty("titleLabel").objectReferenceValue = tmp;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // Small nodes at a distance are hard to aim the crosshair at - a
        // bigger sphere (and matching collider, since it scales with the
        // transform) makes hitting one far less fiddly.
        private static void EnlargeNodePrefab()
        {
            const string path = "Assets/Prefabs/NodePrefab.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            prefab.transform.localScale = Vector3.one * 1f;
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
        }

        // NodeView's [SerializeField] defaults only apply to *new* prefabs -
        // NodePrefab.prefab already has its own saved values, so bumping the
        // brightness in code alone wouldn't touch the existing asset.
        private static void BoostNodePrefabBrightness()
        {
            const string path = "Assets/Prefabs/NodePrefab.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            var nodeView = prefab.GetComponent<NodeView>();
            if (nodeView == null) return;

            var so = new SerializedObject(nodeView);
            so.FindProperty("emissionColor").colorValue = new Color(0.35f, 0.85f, 1f);
            so.FindProperty("hoverColor").colorValue = new Color(0.4f, 1f, 0.5f);
            so.FindProperty("emissionIntensity").floatValue = 4.5f;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
        }
    }
}
