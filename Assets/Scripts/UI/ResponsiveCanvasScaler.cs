using UnityEngine;
using UnityEngine.UI;

namespace Nodia.UI
{
    // A web page's window can be resized to any aspect ratio, unlike a fixed
    // game window - CanvasScaler's single "Match Width Or Height" value only
    // scales cleanly for the one direction it's locked to (e.g. Match Width
    // makes everything shrink whenever the window gets narrower, even if
    // there's plenty of height to spare). Switching between matching width
    // and height depending on which one is currently more constrained keeps
    // panels a readable size at any window shape without ever overflowing.
    [RequireComponent(typeof(CanvasScaler))]
    public class ResponsiveCanvasScaler : MonoBehaviour
    {
        private CanvasScaler scaler;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
        }

        private void Update()
        {
            float referenceAspect = scaler.referenceResolution.x / scaler.referenceResolution.y;
            float screenAspect = (float)Screen.width / Screen.height;
            scaler.matchWidthOrHeight = screenAspect < referenceAspect ? 0f : 1f;
        }
    }
}
