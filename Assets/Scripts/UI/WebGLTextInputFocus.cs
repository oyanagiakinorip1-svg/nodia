using TMPro;

namespace Nodia.UI
{
    // TextMeshPro's InputField can't reliably do Japanese/CJK IME
    // composition on WebGL (Unity reads raw keyboard input directly rather
    // than handing composition off to the browser). kou-yeung/WebGLInput
    // (github.com/kou-yeung/WebGLInput) solves this the well-tested way: it
    // auto-detects the TMP_InputField on the same GameObject and overlays a
    // real HTML input for it, so IME is handled by the browser itself.
    public static class WebGLTextInputFocus
    {
        public static void Wire(TMP_InputField field)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (field.GetComponent<WebGLSupport.WebGLInput>() == null)
            {
                field.gameObject.AddComponent<WebGLSupport.WebGLInput>();
            }
#endif
        }
    }
}
