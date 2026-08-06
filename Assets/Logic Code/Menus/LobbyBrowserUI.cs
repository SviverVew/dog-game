using System;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyBrowserUI : MonoBehaviour
{
    [Header("Main menu: exactly Play, Settings, Exit")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject playPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button settingsBackButton;

    [Header("Play panel")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Toggle publicToggle;
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Transform roomListContent;
    [SerializeField] private RoomListItem roomItemPrefab;
    [SerializeField] private TMP_Text statusText;

    [Header("Scenes")]
    [SerializeField] private string waitingRoomScene = "WaitingRoom";

    private bool busy;

    private void Start()
    {
        mainMenuPanel.SetActive(true);
        playPanel.SetActive(false);
        settingsPanel.SetActive(false);
        playButton.onClick.AddListener(OpenPlay);
        settingsButton.onClick.AddListener(() => settingsPanel.SetActive(true));
        exitButton.onClick.AddListener(ExitGame);
        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(() => settingsPanel.SetActive(false));
        createButton.onClick.AddListener(CreateRoom);
        joinCodeButton.onClick.AddListener(JoinByCode);
        refreshButton.onClick.AddListener(RefreshRooms);
        backButton.onClick.AddListener(BackToMain);
    }

    private void OpenPlay()
    {
        mainMenuPanel.SetActive(false);
        playPanel.SetActive(true);
        RefreshRooms();
    }

    private void BackToMain()
    {
        playPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    private void SaveName()
    {
        string value = playerNameInput == null ? "" : playerNameInput.text.Trim();
        NetworkRelayManager.Instance.PlayerName = string.IsNullOrEmpty(value) ? "Player" : value;
    }

    private async void CreateRoom()
    {
        if (busy) return;
        SetBusy(true, "Đang tạo phòng...");
        try
        {
            SaveName();
            Lobby lobby = await NetworkRelayManager.Instance.CreateRoomAsync(
                roomNameInput == null ? "" : roomNameInput.text,
                publicToggle == null || publicToggle.isOn);
            if (lobby == null) throw new Exception("Không thể khởi động host.");
            NetworkManager.Singleton.SceneManager.LoadScene(waitingRoomScene, LoadSceneMode.Single);
        }
        catch (Exception e) { SetBusy(false, "Tạo phòng thất bại: " + e.Message); }
    }

    private async void JoinByCode()
    {
        string code = roomCodeInput == null ? "" : roomCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code)) { statusText.text = "Hãy nhập mã Lobby."; return; }
        if (busy) return;
        SetBusy(true, "Đang vào phòng...");
        try
        {
            SaveName();
            if (!await NetworkRelayManager.Instance.JoinLobbyCodeAsync(code))
                throw new Exception("Không thể kết nối Relay.");
            // Netcode automatically synchronizes this client to the host's WaitingRoom scene.
        }
        catch (Exception e) { SetBusy(false, "Vào phòng thất bại: " + e.Message); }
    }

    public async void JoinPublicRoom(string lobbyId)
    {
        if (busy) return;
        SetBusy(true, "Đang vào phòng public...");
        try
        {
            SaveName();
            if (!await NetworkRelayManager.Instance.JoinPublicLobbyAsync(lobbyId))
                throw new Exception("Không thể kết nối Relay.");
        }
        catch (Exception e) { SetBusy(false, "Vào phòng thất bại: " + e.Message); }
    }

    private async void RefreshRooms()
    {
        if (busy) return;
        SetBusy(true, "Đang tải danh sách phòng...");
        try
        {
            foreach (Transform child in roomListContent) Destroy(child.gameObject);
            var rooms = await NetworkRelayManager.Instance.QueryPublicRoomsAsync();
            foreach (Lobby room in rooms) Instantiate(roomItemPrefab, roomListContent).Bind(room, this);
            SetBusy(false, rooms.Count == 0 ? "Chưa có phòng public." : $"Tìm thấy {rooms.Count} phòng.");
        }
        catch (Exception e) { SetBusy(false, "Không tải được phòng: " + e.Message); }
    }

    private void SetBusy(bool value, string message)
    {
        busy = value;
        createButton.interactable = !value;
        joinCodeButton.interactable = !value;
        refreshButton.interactable = !value;
        if (statusText != null) statusText.text = message;
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
