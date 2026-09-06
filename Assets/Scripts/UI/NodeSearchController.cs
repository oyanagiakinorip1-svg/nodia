using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Nodia.Nodes;
using Nodia.Player;

namespace Nodia.UI
{
    // Opened from the main menu (Tab); click a result (or its row) to fly
    // the player next to that node. No dedicated hotkey of its own - one
    // menu key is easier to remember and carries over to a touch button
    // later, rather than every screen claiming its own letter.
    public class NodeSearchController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_InputField queryField;
        [SerializeField] private Transform resultsContainer;
        [SerializeField] private GameObject resultRowPrefab;
        [SerializeField] private NodeManager nodeManager;
        [SerializeField] private FPSController fpsController;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private CharacterController playerController;
        [SerializeField] private int maxResults = 8;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            panel.SetActive(false);
            queryField.onValueChanged.AddListener(_ => RefreshResults());
            WebGLTextInputFocus.Wire(queryField);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Open()
        {
            IsOpen = true;
            panel.SetActive(true);
            fpsController.SetCursorLocked(false);
            queryField.text = "";
            queryField.ActivateInputField();
            RefreshResults();
        }

        private void Close()
        {
            IsOpen = false;
            panel.SetActive(false);
            fpsController.SetCursorLocked(true);
        }

        private void RefreshResults()
        {
            foreach (Transform child in resultsContainer)
            {
                Destroy(child.gameObject);
            }

            string query = queryField.text.Trim();
            var matches = nodeManager.GetAllNodes()
                .Where(n => string.IsNullOrEmpty(query) || Matches(n, query))
                .Take(maxResults);

            foreach (var node in matches)
            {
                var row = Instantiate(resultRowPrefab, resultsContainer);
                var label = row.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = string.IsNullOrEmpty(node.Data.title) ? "(無題のノート)" : node.Data.title;
                }

                var button = row.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => JumpTo(node));
                }
                else
                {
                    Debug.LogWarning($"NODIA: search result row prefab has no Button component ({row.name}).");
                }
            }
        }

        private static bool Matches(NodeView node, string query)
        {
            return (node.Data.title ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                || (node.Data.content ?? "").Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        private void JumpTo(NodeView node)
        {
            Vector3 direction = playerTransform.position - node.transform.position;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.back;
            Vector3 destination = node.transform.position + direction.normalized * 3f;

            // CharacterController fights a direct transform.position write while
            // enabled (it resolves movement itself each frame), so a teleport
            // has to disable it first.
            // Face back toward the node so it's what's actually in view after
            // the jump - otherwise the player keeps whatever heading they had
            // before, and a completely different node can end up in view
            // instead, looking like the jump landed on the wrong one.
            Vector3 lookDirection = -direction;
            lookDirection.y = 0f;
            Quaternion facing = lookDirection.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
                : playerTransform.rotation;

            if (playerController != null)
            {
                playerController.enabled = false;
                playerTransform.SetPositionAndRotation(destination, facing);
                playerController.enabled = true;
            }
            else
            {
                playerTransform.SetPositionAndRotation(destination, facing);
            }

            Close();
        }
    }
}
