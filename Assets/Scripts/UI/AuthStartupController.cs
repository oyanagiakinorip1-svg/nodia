using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nodia.Networking;
using Nodia.Player;

namespace Nodia.UI
{
    // The very first screen: silently resumes a saved session if there is
    // one, otherwise offers a choice between a disposable guest session and
    // a real email+password account before anything else in the space loads.
    public class AuthStartupController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button guestButton;
        [SerializeField] private TMP_InputField emailField;
        [SerializeField] private TMP_InputField passwordField;
        [SerializeField] private Button signUpButton;
        [SerializeField] private Button signInButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private FPSController fpsController;
        [SerializeField] private SpaceSelectController spaceSelect;

        private void Awake()
        {
            panel.SetActive(false);
            guestButton.onClick.AddListener(OnGuestClicked);
            signUpButton.onClick.AddListener(OnSignUpClicked);
            signInButton.onClick.AddListener(OnSignInClicked);
        }

        private void Start()
        {
            fpsController.SetCursorLocked(false);
            SupabaseAuth.Instance.TryResumeSession(OnAuthReady, ShowChoiceScreen);
        }

        private void ShowChoiceScreen()
        {
            panel.SetActive(true);
        }

        private void OnGuestClicked()
        {
            SetBusy("接続中…");
            SupabaseAuth.Instance.SignInAsGuest(OnAuthReady, OnAuthError);
        }

        private void OnSignUpClicked()
        {
            if (!TryReadCredentials(out var email, out var password)) return;
            SetBusy("登録中…");
            SupabaseAuth.Instance.SignUpWithEmail(email, password, OnAuthReady, OnAuthError);
        }

        private void OnSignInClicked()
        {
            if (!TryReadCredentials(out var email, out var password)) return;
            SetBusy("ログイン中…");
            SupabaseAuth.Instance.SignInWithEmail(email, password, OnAuthReady, OnAuthError);
        }

        private bool TryReadCredentials(out string email, out string password)
        {
            email = emailField.text.Trim();
            password = passwordField.text;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                statusText.text = "メールアドレスとパスワードを入力してください。";
                return false;
            }
            return true;
        }

        private void SetBusy(string message)
        {
            statusText.text = message;
            guestButton.interactable = false;
            signUpButton.interactable = false;
            signInButton.interactable = false;
        }

        private void OnAuthError(string message)
        {
            Debug.LogError(message);
            statusText.text = "失敗しました。メールアドレス・パスワードをご確認ください(登録直後の場合、メール確認が必要な設定になっている可能性があります)。";
            guestButton.interactable = true;
            signUpButton.interactable = true;
            signInButton.interactable = true;
        }

        private void OnAuthReady()
        {
            panel.SetActive(false);
            spaceSelect.Show();
        }
    }
}
