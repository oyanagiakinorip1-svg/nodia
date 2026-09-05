using TMPro;
using UnityEngine;
using Nodia.Data;

namespace Nodia.Nodes
{
    // Scene representation of one memo node. Holds the server-side NodeData and
    // keeps its Emission lit so it stays readable without complex scene lighting.
    public class NodeView : MonoBehaviour
    {
        [SerializeField] private Renderer nodeRenderer;
        [SerializeField] private TextMeshPro titleLabel;
        [SerializeField] private Color emissionColor = new Color(0.35f, 0.85f, 1f);
        // Hover = "you're aiming at this" (any node, no key needed). Pending =
        // "Shift+clicked, this is the connection's start/end" - a further,
        // more obvious step up from hover.
        [SerializeField] private Color hoverColor = new Color(0.4f, 1f, 0.5f);
        [SerializeField] private Color pendingConnectionColor = new Color(1f, 0.7f, 0.15f);
        // Emission colors max out at 1.0 per channel, which sits right at (or
        // under) the Bloom threshold - multiplying past 1.0 is what actually
        // makes the node glow instead of just looking like a flat lit color.
        [SerializeField] private float emissionIntensity = 4.5f;

        public NodeData Data { get; private set; }

        private Vector3 baseScale;
        private bool isHovered;
        private bool isPending;

        public void Initialize(NodeData data)
        {
            Data = data;
            transform.position = new Vector3(data.position_x, data.position_y, data.position_z);
            baseScale = transform.localScale;
            RefreshVisual();
            RefreshTitleLabel();
        }

        public void ApplyContent(string title, string content)
        {
            Data.title = title;
            Data.content = content;
            RefreshTitleLabel();
        }

        // Floating above the node so it can be told apart from others at a
        // distance, without having to fly up and open it first.
        private void RefreshTitleLabel()
        {
            if (titleLabel == null) return;
            titleLabel.text = string.IsNullOrEmpty(Data.title) ? "無題" : Data.title;
        }

        public void SetHovered(bool hovered)
        {
            isHovered = hovered;
            RefreshVisual();
        }

        // "This is the node a connection will start/end from" - takes
        // priority over plain hover, and bumps scale too since it's a
        // deliberate selection, not just where the crosshair happens to be.
        public void SetPendingConnection(bool pending)
        {
            isPending = pending;
            transform.localScale = pending ? baseScale * 1.15f : baseScale;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            Color color = isPending ? pendingConnectionColor : isHovered ? hoverColor : emissionColor;
            ApplyEmission(color);
        }

        private void ApplyEmission(Color color)
        {
            if (nodeRenderer == null) return;
            var mat = nodeRenderer.material;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionIntensity);
        }
    }
}
