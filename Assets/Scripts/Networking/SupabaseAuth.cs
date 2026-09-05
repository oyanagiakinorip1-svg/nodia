using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Nodia.Networking
{
    // Supabase session handling for three entry points: resuming a previous
    // session from its refresh token, a throwaway guest/anonymous session
    // (requires "Anonymous Sign-ins" enabled in Supabase), and a real
    // email+password account. Whichever one succeeds, the resulting session
    // persists the same way (refresh token in PlayerPrefs).
    public class SupabaseAuth : MonoBehaviour
    {
        public static SupabaseAuth Instance { get; private set; }

        [SerializeField] private string supabaseUrl = "https://YOUR-PROJECT.supabase.co";
        [SerializeField] private string supabaseAnonKey = "YOUR-ANON-KEY";

        private const string RefreshTokenKey = "nodia_refresh_token";

        public string AccessToken { get; private set; }
        public string UserId { get; private set; }

        [Serializable] private class AuthUser { public string id; }
        [Serializable] private class AuthSession { public string access_token; public string refresh_token; public AuthUser user; }
        [Serializable] private class RefreshRequest { public string refresh_token; }
        [Serializable] private class EmailPasswordRequest { public string email; public string password; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Silently resumes a previously saved session. Calls onNoSession
        // (not onError) when there's nothing to resume from, or the refresh
        // itself is rejected - either way the caller should fall back to
        // showing the guest/sign-in choice, not treat it as a hard failure.
        public void TryResumeSession(Action onReady, Action onNoSession)
        {
            string storedRefreshToken = PlayerPrefs.GetString(RefreshTokenKey, "");
            if (string.IsNullOrEmpty(storedRefreshToken))
            {
                onNoSession?.Invoke();
                return;
            }

            StartCoroutine(RefreshSession(storedRefreshToken, onReady, _ => onNoSession?.Invoke()));
        }

        public void SignInAsGuest(Action onReady, Action<string> onError)
        {
            StartCoroutine(PostAuth("/auth/v1/signup", "{}", onReady, onError, "guest sign-in"));
        }

        public void SignUpWithEmail(string email, string password, Action onReady, Action<string> onError)
        {
            string json = JsonUtility.ToJson(new EmailPasswordRequest { email = email, password = password });
            StartCoroutine(PostAuth("/auth/v1/signup", json, onReady, onError, "sign-up"));
        }

        public void SignInWithEmail(string email, string password, Action onReady, Action<string> onError)
        {
            string json = JsonUtility.ToJson(new EmailPasswordRequest { email = email, password = password });
            StartCoroutine(PostAuth("/auth/v1/token?grant_type=password", json, onReady, onError, "sign-in"));
        }

        private IEnumerator PostAuth(string path, string jsonBody, Action onReady, Action<string> onError, string label)
        {
            using var request = new UnityWebRequest($"{supabaseUrl}{path}", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", supabaseAnonKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"{label} failed: {request.error} {request.downloadHandler.text}");
                yield break;
            }

            // A 200 response doesn't always mean a usable session: sign-up
            // with "Confirm email" enabled returns just the user object with
            // no session until the email link is clicked. Without this check
            // we'd silently proceed with an empty access token and every
            // API call would fail as unauthorized.
            if (!TryApplySession(request.downloadHandler.text))
            {
                onError?.Invoke($"{label}: no session in response (email confirmation likely required) - {request.downloadHandler.text}");
                yield break;
            }

            onReady?.Invoke();
        }

        private IEnumerator RefreshSession(string refreshToken, Action onReady, Action<string> onError)
        {
            using var request = new UnityWebRequest($"{supabaseUrl}/auth/v1/token?grant_type=refresh_token", "POST");
            string json = JsonUtility.ToJson(new RefreshRequest { refresh_token = refreshToken });
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", supabaseAnonKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(request.error);
                yield break;
            }

            if (!TryApplySession(request.downloadHandler.text))
            {
                onError?.Invoke("refresh: no session in response");
                yield break;
            }

            onReady?.Invoke();
        }

        private bool TryApplySession(string json)
        {
            var session = JsonUtility.FromJson<AuthSession>(json);
            if (session == null || string.IsNullOrEmpty(session.access_token))
            {
                return false;
            }

            AccessToken = session.access_token;
            UserId = session.user?.id;
            PlayerPrefs.SetString(RefreshTokenKey, session.refresh_token);
            PlayerPrefs.Save();
            return true;
        }
    }
}
