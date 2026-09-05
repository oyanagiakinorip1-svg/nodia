using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Nodia.Data;
using Nodia.Networking;
using Nodia.Nodes;
using Nodia.Player;

namespace Nodia.UI
{
    // Shown right after auth resolves, and again whenever the main menu's
    // "spaces" button is used mid-game: lists the signed-in user's spaces
    // (e.g. one per class) and lets them enter one, create a new one, delete
    // one (with confirmation), or (once already in a space) close without
    // switching.
    public class SpaceSelectController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform listContainer;
        [SerializeField] private GameObject rowPrefab; // expects a "DeleteButton" child, see NodiaSpaceSelectSetup
        [SerializeField] private TMP_InputField newSpaceNameField;
        [SerializeField] private Button createButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private FPSController fpsController;
        [SerializeField] private NodeManager nodeManager;
        [SerializeField] private HelpController helpController;

        [Header("Delete confirmation")]
        [SerializeField] private GameObject confirmDeletePanel;
        [SerializeField] private TMP_Text confirmDeleteText;
        [SerializeField] private Button confirmDeleteButton;
        [SerializeField] private Button cancelDeleteButton;

        private string pendingDeleteSpaceId;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            panel.SetActive(false);
            confirmDeletePanel.SetActive(false);
            createButton.onClick.AddListener(OnCreateClicked);
            confirmDeleteButton.onClick.AddListener(OnConfirmDelete);
            cancelDeleteButton.onClick.AddListener(() => confirmDeletePanel.SetActive(false));
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            WebGLTextInputFocus.Wire(newSpaceNameField);
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
            confirmDeletePanel.SetActive(false);
            fpsController.SetCursorLocked(false);
            // Nothing to "cancel back to" the very first time a session
            // enters a space - only offer Close once one is already loaded.
            if (closeButton != null) closeButton.gameObject.SetActive(!string.IsNullOrEmpty(nodeManager.CurrentSpaceId));
            RefreshList();
        }

        private void Close()
        {
            panel.SetActive(false);
            fpsController.SetCursorLocked(true);
        }

        private void RefreshList()
        {
            statusText.text = "読み込み中…";
            ApiClient.Instance.GetSpaces(response =>
            {
                statusText.text = response.spaces.Length == 0 ? "まだスペースがありません。新しく作成してください。" : "";
                foreach (Transform child in listContainer)
                {
                    Destroy(child.gameObject);
                }

                foreach (var space in response.spaces)
                {
                    var row = Instantiate(rowPrefab, listContainer);
                    var label = row.transform.Find("SelectButton/Text")?.GetComponent<TMP_Text>();
                    if (label != null) label.text = space.name;

                    var selectButton = row.transform.Find("SelectButton")?.GetComponent<Button>();
                    if (selectButton != null) selectButton.onClick.AddListener(() => EnterSpace(space.id));

                    var deleteButton = row.transform.Find("DeleteButton")?.GetComponent<Button>();
                    if (deleteButton != null) deleteButton.onClick.AddListener(() => RequestDelete(space));
                }
            }, err =>
            {
                Debug.LogError(err);
                statusText.text = "スペース一覧の取得に失敗しました。";
            });
        }

        private void RequestDelete(SpaceData space)
        {
            pendingDeleteSpaceId = space.id;
            string name = string.IsNullOrEmpty(space.name) ? "無題のスペース" : space.name;
            confirmDeleteText.text = $"「{name}」を削除しますか？\n中のノート・接続もすべて削除され、元に戻せません。";
            confirmDeletePanel.SetActive(true);
        }

        private void OnConfirmDelete()
        {
            confirmDeletePanel.SetActive(false);
            if (string.IsNullOrEmpty(pendingDeleteSpaceId)) return;

            string spaceId = pendingDeleteSpaceId;
            pendingDeleteSpaceId = null;

            ApiClient.Instance.DeleteSpace(spaceId, () =>
            {
                nodeManager.ClearIfCurrentSpace(spaceId);
                RefreshList();
            }, err =>
            {
                Debug.LogError(err);
                statusText.text = "削除に失敗しました。";
            });
        }

        private void OnCreateClicked()
        {
            string name = newSpaceNameField.text.Trim();
            if (string.IsNullOrEmpty(name)) name = "無題のスペース";

            statusText.text = "作成中…";
            ApiClient.Instance.CreateSpace(name, space => EnterSpace(space.id), err =>
            {
                Debug.LogError(err);
                statusText.text = "作成に失敗しました。";
            });
        }

        private void EnterSpace(string spaceId)
        {
            panel.SetActive(false);
            fpsController.SetCursorLocked(true);
            nodeManager.LoadSpace(spaceId);
            if (helpController != null) helpController.ShowIfFirstTime();
        }
    }
}
