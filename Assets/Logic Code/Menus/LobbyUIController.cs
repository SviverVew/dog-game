using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIController : NetworkBehaviour
{
    [Header("1. Main Menu Panel")]
    public GameObject mainMenuPanel;
    public TMP_InputField nameInputField;
    public TMP_InputField joinCodeInputField;
    public Button createRoomButton;
    public Button joinRoomButton;

    [Header("2. Lobby Room Panel")]
    public GameObject lobbyRoomPanel;
    public TMP_Text roomCodeText;
    public TMP_Text playerListText;

    [Header("Cấu hình Scene Game")]
    public string inGameSceneName = "House1_Scene"; // Tên scene chơi game chính

    private bool isInLobby = false; // Cờ kiểm tra xem đang ở màn hình phòng chờ chưa

    private void Start()
    {
        // Hiển thị Panel Menu chính, ẩn Panel Phòng chờ
        mainMenuPanel.SetActive(true);
        lobbyRoomPanel.SetActive(false);
        isInLobby = false;

        if (createRoomButton) createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        if (joinRoomButton) joinRoomButton.onClick.AddListener(OnJoinRoomClicked);

        // Đăng ký sự kiện khi có client kết nối/ngắt kết nối
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
        }
    }

    private void Update()
    {
        // Bắt sự kiện ENTER từ Host
        if (isInLobby && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            // Nhận cả Enter phím chính lẫn Numpad Enter
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                // Bỏ Focus khỏi các Input Field để không bị nuốt phím
                if (UnityEngine.EventSystems.EventSystem.current != null)
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                }

                Debug.Log("<color=yellow>[Lobby] Đã nhận phím ENTER trên bản Build!</color>");
                StartGameLogic();
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
        }
    }

    private async void OnCreateRoomClicked()
    {
        SavePlayerName();
        if (createRoomButton) createRoomButton.interactable = false;

        string code = await NetworkRelayManager.Instance.CreateRelayHost(maxPlayers: 4);
        if (!string.IsNullOrEmpty(code))
        {
            ShowLobbyRoom(code, isHost: true);
        }
        else
        {
            if (createRoomButton) createRoomButton.interactable = true;
        }
    }

    private async void OnJoinRoomClicked()
    {
        SavePlayerName();
        
        if (joinCodeInputField == null) return;
        string inputCode = joinCodeInputField.text.Trim();

        if (string.IsNullOrEmpty(inputCode))
        {
            Debug.LogWarning("Vui lòng nhập Mã Phòng!");
            return;
        }

        if (joinRoomButton) joinRoomButton.interactable = false;
        bool success = await NetworkRelayManager.Instance.JoinRelayClient(inputCode);
        
        if (success)
        {
            ShowLobbyRoom(inputCode, isHost: false);
        }
        else
        {
            if (joinRoomButton) joinRoomButton.interactable = true;
        }
    }

    private void SavePlayerName()
    {
        if (nameInputField != null)
        {
            string pName = nameInputField.text.Trim();
            if (!string.IsNullOrEmpty(pName) && NetworkRelayManager.Instance != null)
            {
                NetworkRelayManager.Instance.PlayerName = pName;
            }
        }
    }

    private void ShowLobbyRoom(string code, bool isHost)
    {
        mainMenuPanel.SetActive(false);
        lobbyRoomPanel.SetActive(true);
        isInLobby = true; // Bật cờ đang ở Lobby để cho phép bấm Enter

        if (roomCodeText) roomCodeText.text = "MÃ PHÒNG: " + code;

        UpdatePlayerList();
    }

    private void OnPlayerConnected(ulong clientId) => UpdatePlayerList();
    private void OnPlayerDisconnected(ulong clientId) => UpdatePlayerList();

    private void UpdatePlayerList()
    {
        if (playerListText != null && NetworkManager.Singleton != null)
        {
            int count = NetworkManager.Singleton.ConnectedClientsIds.Count;
            
            // Dòng chữ hướng dẫn cho Host bấm Enter
            if (NetworkManager.Singleton.IsHost)
            {
                playerListText.text = $"Số người chơi trong phòng: {count}/4\n<color=yellow>(Nhấn ENTER để Bắt Đầu Game)</color>";
            }
            else
            {
                playerListText.text = $"Số người chơi trong phòng: {count}/4\n(Chờ Host nhấn ENTER...)";
            }
        }
    }

    // Logic Bắt Đầu Game
    // Logic Bắt Đầu Game
    private void StartGameLogic()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        int currentPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;

        // 📌 ĐIỀU KIỆN SỐ LƯỢNG NGƯỜI CHƠI (Để = 1 nếu muốn test 1 mình, để = 2 khi test 2 máy)
        if (currentPlayers < 2)
        {
            Debug.LogWarning($"[Lobby] Chưa đủ người chơi! Đang có {currentPlayers}/4. Cần ít nhất 2 người!");
            return;
        }

        Debug.Log("<color=green>[Lobby] Đủ điều kiện! Đang bắt đầu Game và Phân Role...</color>");

        // 1. Gửi lệnh ẩn UI Lobby cho toàn bộ Client
        HideLobbyUIClientRpc();

        // 2. Ra lệnh cho GameMatchManager thực hiện Phân Role & Spawn nhân vật đúng vị trí!
        if (GameMatchManager.Instance != null)
        {
            GameMatchManager.Instance.StartMatchAndAssignRoles();
        }
        else
        {
            Debug.LogError("[Lobby] Không tìm thấy GameMatchManager trong Scene!");
        }
    }

    [ClientRpc]
    private void HideLobbyUIClientRpc()
    {
        if (lobbyRoomPanel) lobbyRoomPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isInLobby = false;
    }
}