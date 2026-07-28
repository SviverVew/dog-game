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
        // ⌨️ BẮT SỰ KIỆN PHÍM ENTER / RETURN
        if (isInLobby && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Debug.Log("<color=yellow>[Lobby] Đã nhận thao tác bấm ENTER từ Host!</color>");
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
    private void StartGameLogic()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        HideLobbyUIClientRpc();

        int currentPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;

        // 📌 ĐIỀU KIỆN SỐ LƯỢNG NGƯỜI CHƠI (Nếu muốn test 1 mình thì đổi số 2 thành 1)
        if (currentPlayers < 2)
        {
            Debug.LogWarning($"[Lobby] Chưa đủ người chơi! Đang có {currentPlayers}/4. Cần ít nhất 2 người!");
            return;
        }

        Debug.Log("<color=green>[Lobby] Đủ điều kiện! Đang bắt đầu Game...</color>");

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == inGameSceneName)
        {
            HideLobbyUIClientRpc();
        }
        else
        {
            NetworkManager.Singleton.SceneManager.LoadScene(inGameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
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