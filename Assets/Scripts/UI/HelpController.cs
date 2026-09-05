using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Nodia.Player;

namespace Nodia.UI
{
    // Opened from the main menu (Tab): a static reference of every control -
    // nothing in the game otherwise explains itself (no tutorial, no
    // on-screen hints).
    public class HelpController : MonoBehaviour
    {
        private const string PrefSeenHelp = "nodia_seen_help";

        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private FPSController fpsController;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            panel.SetActive(false);
            closeButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Show()
        {
            panel.SetActive(true);
            fpsController.SetCursorLocked(false);
        }

        // Called right after a space finishes loading - nothing else in the
        // game explains its own controls, so a brand new player would
        // otherwise have no way to discover WASD/click/Tab on their own.
        public void ShowIfFirstTime()
        {
            if (PlayerPrefs.GetInt(PrefSeenHelp, 0) != 0) return;
            PlayerPrefs.SetInt(PrefSeenHelp, 1);
            Show();
        }

        private void Close()
        {
            panel.SetActive(false);
            fpsController.SetCursorLocked(true);
        }
    }
}
