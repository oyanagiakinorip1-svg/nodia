using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Nodia.Interaction;

namespace Nodia.EditorTools
{
    // A small always-on-top dot at the exact center of the screen, so it's
    // clear where the interaction raycast (and thus the player's facing) is
    // actually pointing - without it there's no way to tell what you're
    // about to click on, or which way you're even facing after a jump.
    public static class NodiaCrosshairSetup
    {
        [MenuItem("Nodia/Setup Crosshair")]
        public static void SetupCrosshair()
        {
            var canvas = GameObject.Find("NoteCanvas");
            var playerGO = GameObject.Find("Player");
            var interactor = playerGO != null ? playerGO.GetComponent<PlayerInteractor>() : null;
            if (canvas == null || interactor == null)
            {
                Debug.LogError("NODIA: NoteCanvas/Player not found - run Nodia > Setup Scene first.");
                return;
            }

            var existing = canvas.transform.Find("Crosshair");
            var crosshair = existing != null ? existing.gameObject
                : new GameObject("Crosshair", typeof(RectTransform), typeof(Image), typeof(Outline));
            crosshair.transform.SetParent(canvas.transform, false);
            crosshair.transform.SetAsLastSibling(); // always drawn above the note/search panels

            var rect = crosshair.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(8, 8);
            rect.anchoredPosition = Vector2.zero;

            var image = crosshair.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.9f);
            image.raycastTarget = false;

            var outline = crosshair.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(1f, 1f);
            outline.useGraphicAlpha = true;

            SetField(interactor, "crosshair", crosshair);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("NODIA: crosshair added.");
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
