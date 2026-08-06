using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaitingRoomController : NetworkBehaviour
{
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text playerListText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private string gameSceneName = "House1_Scene";
    [SerializeField] private string menuSceneName = "Menu_Game";
    [SerializeField] private int minimumPlayers = 2;
    [SerializeField] private float countdownSeconds = 30f;

    private readonly NetworkVariable<float> timeLeft = new(-1f);
    private bool loadingGame;

    public override void OnNetworkSpawn()
    {
        startButton.gameObject.SetActive(IsHost);
        startButton.onClick.AddListener(StartNow);
        leaveButton.onClick.AddListener(LeaveRoom);
        NetworkManager.OnClientConnectedCallback += OnPlayersChanged;
        NetworkManager.OnClientDisconnectCallback += OnPlayersChanged;
        timeLeft.OnValueChanged += OnCountdownChanged;
        RefreshUI();
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= OnPlayersChanged;
            NetworkManager.OnClientDisconnectCallback -= OnPlayersChanged;
        }
        timeLeft.OnValueChanged -= OnCountdownChanged;
    }

    private void Update()
    {
        if (!IsServer || loadingGame) return;
        int count = NetworkManager.ConnectedClientsIds.Count;
        if (count < minimumPlayers)
        {
            if (timeLeft.Value >= 0f) timeLeft.Value = -1f;
            return;
        }

        if (timeLeft.Value < 0f) timeLeft.Value = countdownSeconds;
        timeLeft.Value -= Time.deltaTime;
        if (timeLeft.Value <= 0f) LoadGame();
    }

    private void StartNow()
    {
        if (!IsHost) return;
        if (NetworkManager.ConnectedClientsIds.Count < minimumPlayers)
        {
            countdownText.text = $"Cần ít nhất {minimumPlayers} người chơi";
            return;
        }
        LoadGame();
    }

    private void LoadGame()
    {
        if (!IsServer || loadingGame) return;
        loadingGame = true;
        NetworkManager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private void OnPlayersChanged(ulong _) => RefreshUI();
    private void OnCountdownChanged(float _, float value) => RefreshUI();

    private void RefreshUI()
    {
        int count = NetworkManager == null ? 0 : NetworkManager.ConnectedClientsIds.Count;
        playerListText.text = $"Người chơi: {count}/4";
        string code = NetworkRelayManager.Instance?.CurrentLobby?.LobbyCode ?? "------";
        roomCodeText.text = "Mã phòng: " + code;
        countdownText.text = timeLeft.Value < 0f
            ? $"Đang chờ đủ {minimumPlayers} người..."
            : $"Bắt đầu sau {Mathf.CeilToInt(timeLeft.Value)} giây";
    }

    private async void LeaveRoom()
    {
        await NetworkRelayManager.Instance.LeaveLobbyAsync();
        SceneManager.LoadScene(menuSceneName);
    }
}
