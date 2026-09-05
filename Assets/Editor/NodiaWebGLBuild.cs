using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nodia.EditorTools
{
    // One-click WebGL build into a sibling folder (next to NODIA/ and
    // nodia-server/), ready to deploy to Vercel as a static site. Compression
    // is disabled so it can be served as plain static files without needing
    // any special server-side headers for .br/.gz content-encoding.
    public static class NodiaWebGLBuild
    {
        [MenuItem("Nodia/Build WebGL")]
        public static void BuildWebGL()
        {
            // BuildPipeline.BuildPlayer reads scenes from disk, not the
            // in-memory Editor state - without this, any unsaved changes
            // (e.g. from the Nodia setup menu items) silently get left out
            // of the build.
            if (!EditorSceneManager.SaveOpenScenes())
            {
                Debug.LogError("NODIA: failed to save open scene(s) - aborting build. Save manually and retry.");
                return;
            }

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                const string fallback = "Assets/Scenes/SampleScene.unity";
                Debug.LogWarning($"NODIA: no scenes in Build Settings - falling back to {fallback}.");
                scenes = new[] { fallback };
            }

            var outputDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "nodia-web"));
            Directory.CreateDirectory(outputDir);

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

            Debug.Log($"NODIA: building WebGL to {outputDir} - this can take several minutes.");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });

            if (report.summary.result == BuildResult.Succeeded)
            {
                MakeCanvasFillViewport(outputDir);
                Debug.Log($"NODIA: WebGL build succeeded -> {outputDir} " +
                          $"({report.summary.totalSize / (1024f * 1024f):0.0} MB)");
            }
            else
            {
                Debug.LogError($"NODIA: WebGL build failed - result: {report.summary.result}. Check the log above for the actual error.");
            }
        }

        // Unity's default WebGL template renders desktop browsers into a
        // fixed 960x600 box centered on the page - going fullscreen (via the
        // browser's own fullscreen shortcut, not Unity's in-page button)
        // doesn't resize that box, so the page's plain background shows
        // through at the corners/edges. Patched here (instead of a custom
        // WebGLTemplate) so it's regenerated correctly on every build.
        private static void MakeCanvasFillViewport(string outputDir)
        {
            var indexPath = Path.Combine(outputDir, "index.html");
            var cssPath = Path.Combine(outputDir, "TemplateData", "style.css");

            if (File.Exists(indexPath))
            {
                var html = File.ReadAllText(indexPath);
                html = html.Replace(
                    "canvas.style.width = \"960px\";\n        canvas.style.height = \"600px\";",
                    "canvas.style.width = \"100%\";\n        canvas.style.height = \"100%\";");
                File.WriteAllText(indexPath, html);
            }

            if (File.Exists(cssPath))
            {
                var css = File.ReadAllText(cssPath);
                const string marker = "/* nodia: fill viewport */";
                if (!css.Contains(marker))
                {
                    css += "\n" + marker + "\n" +
                           "html, body { width: 100%; height: 100%; overflow: hidden; background: #2f333e; }\n" +
                           "#unity-container.unity-desktop { position: fixed; left: 0; top: 0; transform: none; width: 100%; height: 100%; }\n" +
                           // The canvas grabs keyboard focus to receive input, and
                           // browsers draw a default focus ring around a focused
                           // element - at 960x600 that was barely visible, but
                           // stretched to fill the screen it traces the whole edge.
                           "#unity-canvas { width: 100%; height: 100%; display: block; outline: none; }\n";
                    File.WriteAllText(cssPath, css);
                }
            }
        }
    }
}
