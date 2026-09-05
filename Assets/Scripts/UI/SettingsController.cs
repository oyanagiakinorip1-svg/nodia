using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Nodia.Player;

namespace Nodia.UI
{
    // Opened from the main menu (Tab): a small panel to live-tune FPS
    // look/move feel. Values persist via FPSController's own
    // PlayerPrefs-backed properties.
    public class SettingsController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Slider moveSpeedSlider;
        [SerializeField] private Slider verticalSpeedSlider;
        [SerializeField] private TMP_Text sensitivityValueText;
        [SerializeField] private TMP_Text moveSpeedValueText;
        [SerializeField] private TMP_Text verticalSpeedValueText;
        [SerializeField] private Button closeButton;
        [SerializeField] private FPSController fpsController;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            panel.SetActive(false);
            closeButton.onClick.AddListener(Close);

            sensitivitySlider.onValueChanged.AddListener(v =>
            {
                fpsController.MouseSensitivity = v;
                sensitivityValueText.text = v.ToString("0.00");
            });
            moveSpeedSlider.onValueChanged.AddListener(v =>
            {
                fpsController.MoveSpeed = v;
                moveSpeedValueText.text = v.ToString("0.0");
            });
            verticalSpeedSlider.onValueChanged.AddListener(v =>
            {
                fpsController.VerticalSpeed = v;
                verticalSpeedValueText.text = v.ToString("0.0");
            });
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

            sensitivitySlider.SetValueWithoutNotify(fpsController.MouseSensitivity);
            moveSpeedSlider.SetValueWithoutNotify(fpsController.MoveSpeed);
            verticalSpeedSlider.SetValueWithoutNotify(fpsController.VerticalSpeed);
            sensitivityValueText.text = fpsController.MouseSensitivity.ToString("0.00");
            moveSpeedValueText.text = fpsController.MoveSpeed.ToString("0.0");
            verticalSpeedValueText.text = fpsController.VerticalSpeed.ToString("0.0");
        }

        private void Close()
        {
            panel.SetActive(false);
            fpsController.SetCursorLocked(true);
        }
    }
}
