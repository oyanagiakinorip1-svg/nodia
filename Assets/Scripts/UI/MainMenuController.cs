using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Nodia.Player;

namespace Nodia.UI
{
    // Tab opens this menu. There's also a corner button (menuButtonRoot)
    // meant for a future touch build, but it's kept hidden for now: on
    // desktop the mouse cursor is pointer-locked during gameplay (hidden and
    // pinned in place for look control) so no on-screen button is actually
    // clickable there, and WebGL's Touchscreen device detection isn't
    // trustworthy enough (it can report present on a plain desktop
    // trackpad/browser) to gate it on automatically. Revisit once there's a
    // real touch build to test against. (Escape was tried first, but
    // browsers reserve it to force-exit fullscreen/pointer-lock and never
    // let page script override that, so it can't be a reliable app hotkey.)
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject menuButtonRoot;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button searchButton;
        [SerializeField] private Button spacesButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private FPSController fpsController;
        [SerializeField] private NoteUIController noteUI;
        [SerializeField] private NodeSearchController searchController;
        [SerializeField] private SpaceSelectController spaceSelect;
        [SerializeField] private SettingsController settingsController;
        [SerializeField] private HelpController helpController;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            panel.SetActive(false);
            if (menuButtonRoot != null) menuButtonRoot.SetActive(false);
            menuButton.onClick.AddListener(Show);
            searchButton.onClick.AddListener(() => OpenSub(searchController.Open));
            spacesButton.onClick.AddListener(() => OpenSub(spaceSelect.Show));
            settingsButton.onClick.AddListener(() => OpenSub(settingsController.Show));
            helpButton.onClick.AddListener(() => OpenSub(helpController.Show));
            resumeButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            if (IsOpen)
            {
                if (Keyboard.current.tabKey.wasPressedThisFrame) Close();
                return;
            }

            bool busy = IsBusyElsewhere();
            if (!busy && Keyboard.current.tabKey.wasPressedThisFrame) Show();
        }

        private bool IsBusyElsewhere()
        {
            if (noteUI != null && noteUI.IsOpen) return true;
            if (searchController != null && searchController.IsOpen) return true;
            if (spaceSelect != null && spaceSelect.IsOpen) return true;
            if (settingsController != null && settingsController.IsOpen) return true;
            if (helpController != null && helpController.IsOpen) return true;
            return false;
        }

        private void OpenSub(Action show)
        {
            panel.SetActive(false);
            show();
        }

        public void Show()
        {
            panel.SetActive(true);
            fpsController.SetCursorLocked(false);
        }

        private void Close()
        {
            panel.SetActive(false);
            fpsController.SetCursorLocked(true);
        }
    }
}
