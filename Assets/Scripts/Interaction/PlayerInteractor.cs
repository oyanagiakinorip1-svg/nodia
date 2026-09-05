using UnityEngine;
using UnityEngine.InputSystem;
using Nodia.Nodes;
using Nodia.Player;
using Nodia.UI;

namespace Nodia.Interaction
{
    // Single raycast from the camera drives interaction: plain left click
    // opens a node's note; Shift+left click starts/completes a connection
    // between two nodes; right click spawns a new node; Shift+right click
    // deletes a connection line. Deletion is deliberately on the *other*
    // mouse button from connecting - if both used Shift+left, a line sitting
    // between the camera and a node you're aiming at while linking could get
    // deleted by accident instead of registering the click on that node.
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private FPSController fpsController;
        [SerializeField] private NoteUIController noteUI;
        [SerializeField] private ConnectionManager connectionManager;
        [SerializeField] private NodeManager nodeManager;
        [SerializeField] private NodeSearchController searchController;
        [SerializeField] private SpaceSelectController spaceSelect;
        [SerializeField] private SettingsController settingsController;
        [SerializeField] private HelpController helpController;
        [SerializeField] private MainMenuController mainMenu;
        [SerializeField] private GameObject crosshair;
        [SerializeField] private float interactRange = 20f;
        [SerializeField] private float spawnDistance = 4f;

        private NodeView hoveredNode;
        private ConnectionLineView hoveredLine;

        private void Update()
        {
            bool uiOpen = noteUI.IsOpen
                || (searchController != null && searchController.IsOpen)
                || (spaceSelect != null && spaceSelect.IsOpen)
                || (settingsController != null && settingsController.IsOpen)
                || (helpController != null && helpController.IsOpen)
                || (mainMenu != null && mainMenu.IsOpen);
            if (crosshair != null) crosshair.SetActive(!uiOpen);

            // While the note panel or search overlay is open, left clicks
            // belong to their UI buttons, not the 3D raycast (otherwise a
            // "Save" click would also spawn a node or re-open something
            // behind the panel).
            if (uiOpen)
            {
                SetHoveredNode(null);
                hoveredLine = null;
                return;
            }

            NodeView newHoveredNode = null;
            hoveredLine = null;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out var hit, interactRange))
            {
                newHoveredNode = hit.collider.GetComponentInParent<NodeView>();
                if (newHoveredNode == null) hoveredLine = hit.collider.GetComponentInParent<ConnectionLineView>();
            }
            SetHoveredNode(newHoveredNode);

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleLeftClick();
            }
            else if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                HandleRightClick();
            }
        }

        private void SetHoveredNode(NodeView node)
        {
            if (node == hoveredNode) return;
            hoveredNode?.SetHovered(false);
            node?.SetHovered(true);
            hoveredNode = node;
        }

        private void HandleLeftClick()
        {
            if (Keyboard.current.shiftKey.isPressed)
            {
                if (hoveredNode != null) connectionManager.HandleNodeClicked(hoveredNode);
                return;
            }

            if (hoveredNode == null) return;
            fpsController.SetCursorLocked(false);
            noteUI.Open(hoveredNode);
        }

        private void HandleRightClick()
        {
            if (Keyboard.current.shiftKey.isPressed)
            {
                if (hoveredLine != null) connectionManager.HandleLineClicked(hoveredLine);
                return;
            }

            Vector3 spawnPoint = playerCamera.transform.position + playerCamera.transform.forward * spawnDistance;
            nodeManager.CreateNodeAt(spawnPoint);
        }

        public void OnNoteClosed()
        {
            fpsController.SetCursorLocked(true);
        }
    }
}
