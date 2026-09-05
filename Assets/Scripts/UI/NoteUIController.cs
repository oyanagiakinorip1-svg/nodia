using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nodia.Nodes;
using Nodia.Interaction;

namespace Nodia.UI
{
    // The 2D memo panel: title/content fields plus save, close and delete.
    public class NoteUIController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject shadowGroup;
        [SerializeField] private TMP_InputField titleField;
        [SerializeField] private TMP_InputField contentField;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private NodeManager nodeManager;
        [SerializeField] private PlayerInteractor interactor;

        private NodeView currentNode;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            SetVisible(false);
            saveButton.onClick.AddListener(Save);
            closeButton.onClick.AddListener(Close);
            deleteButton.onClick.AddListener(Delete);
            WebGLTextInputFocus.Wire(titleField);
            WebGLTextInputFocus.Wire(contentField);
        }

        public void Open(NodeView node)
        {
            currentNode = node;
            titleField.text = node.Data.title;
            contentField.text = node.Data.content;
            SetVisible(true);
            IsOpen = true;
        }

        private void SetVisible(bool visible)
        {
            panel.SetActive(visible);
            if (shadowGroup != null) shadowGroup.SetActive(visible);
        }

        private void Save()
        {
            if (currentNode == null) return;
            currentNode.ApplyContent(titleField.text, contentField.text);
            nodeManager.SaveNode(currentNode);
            Close();
        }

        private void Delete()
        {
            if (currentNode == null) return;
            nodeManager.DeleteNode(currentNode);
            Close();
        }

        private void Close()
        {
            SetVisible(false);
            currentNode = null;
            IsOpen = false;
            interactor.OnNoteClosed();
        }
    }
}
