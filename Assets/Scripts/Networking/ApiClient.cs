using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Nodia.Data;

namespace Nodia.Networking
{
    // Thin UnityWebRequest wrapper around the Hono API. Every call attaches the
    // current Supabase access token so the server (and Postgres RLS) can scope
    // the request to this anonymous user.
    public class ApiClient : MonoBehaviour
    {
        public static ApiClient Instance { get; private set; }

        [SerializeField] private string apiBaseUrl = "https://nodia-server.vercel.app/api";

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

        public void GetSpaces(Action<SpacesListResponse> onSuccess, Action<string> onError)
            => StartCoroutine(Send("/spaces", "GET", null, onSuccess, onError));

        public void CreateSpace(string name, Action<SpaceData> onSuccess, Action<string> onError)
            => StartCoroutine(Send("/spaces", "POST", JsonUtility.ToJson(new CreateSpaceRequest { name = name }), onSuccess, onError));

        public void GetSpaceSnapshot(string spaceId, Action<SpaceSnapshot> onSuccess, Action<string> onError)
            => StartCoroutine(Send($"/space?space_id={UnityWebRequest.EscapeURL(spaceId)}", "GET", null, onSuccess, onError));

        public void DeleteSpace(string spaceId, Action onSuccess, Action<string> onError)
            => StartCoroutine(Send<SpaceData>($"/spaces/{spaceId}", "DELETE", null, _ => onSuccess?.Invoke(), onError));

        public void CreateNode(CreateNodeRequest req, Action<NodeData> onSuccess, Action<string> onError)
            => StartCoroutine(Send("/nodes", "POST", JsonUtility.ToJson(req), onSuccess, onError));

        public void UpdateNode(string id, UpdateNodeRequest req, Action<NodeData> onSuccess, Action<string> onError)
            => StartCoroutine(Send($"/nodes/{id}", "PUT", JsonUtility.ToJson(req), onSuccess, onError));

        public void DeleteNode(string id, Action onSuccess, Action<string> onError)
            => StartCoroutine(Send<NodeData>($"/nodes/{id}", "DELETE", null, _ => onSuccess?.Invoke(), onError));

        public void CreateConnection(string fromNode, string toNode, string spaceId, Action<ConnectionData> onSuccess, Action<string> onError)
        {
            var body = JsonUtility.ToJson(new CreateConnectionRequest { from_node = fromNode, to_node = toNode, space_id = spaceId });
            StartCoroutine(Send("/connections", "POST", body, onSuccess, onError));
        }

        public void DeleteConnection(string id, Action onSuccess, Action<string> onError)
            => StartCoroutine(Send<ConnectionData>($"/connections/{id}", "DELETE", null, _ => onSuccess?.Invoke(), onError));

        private IEnumerator Send<T>(string path, string method, string jsonBody, Action<T> onSuccess, Action<string> onError)
        {
            using var request = new UnityWebRequest(apiBaseUrl + path, method);
            if (!string.IsNullOrEmpty(jsonBody))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {SupabaseAuth.Instance.AccessToken}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"{method} {path} failed: {request.error} {request.downloadHandler.text}");
                yield break;
            }

            if (string.IsNullOrEmpty(request.downloadHandler.text))
            {
                onSuccess?.Invoke(default);
                yield break;
            }

            onSuccess?.Invoke(JsonUtility.FromJson<T>(request.downloadHandler.text));
        }
    }
}
