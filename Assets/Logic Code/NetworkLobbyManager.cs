using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer; // Dùng Namespace mới ở đây
using UnityEngine;
using TMPro;

public class NetworkLobbyManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField joinCodeInputField;
    public TMP_Text displayJoinCodeText;
    public GameObject mainMenuUI;

    private const int MAX_PLAYERS = 6;

    private async void Start()
    {
        // 1. Khởi tạo Unity Services và Login Anonymous
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Đã đăng nhập UGS! Player ID: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Lỗi khởi tạo Unity Services: {e.Message}");
        }
    }

    // 1. NÚT TẠO PHÒNG (HOST)
    public async void CreateRoomHost()
    {
        try
        {
            // Cấu hình Session với tối đa 6 người chơi
            var options = new SessionOptions
            {
                MaxPlayers = MAX_PLAYERS,
                IsPrivate = true // Đặt true để người chơi phải dùng Code mới vào được
            };

            Debug.Log("Đang khởi tạo phòng...");

            // Sử dụng MultiplayerService API mới để tạo Host Session
            ISession session = await MultiplayerService.Instance.CreateSessionAsync(options);

            // Lấy Code phòng để gửi cho bạn bè
            string joinCode = session.Code;
            Debug.Log($"Tạo phòng thành công! Code: {joinCode}");

            if (displayJoinCodeText != null)
                displayJoinCodeText.text = "Mã phòng: " + joinCode;

            // Bắt đầu Netcode Host (MultiplayerService đã tự gán Data cho NetworkManager)
            NetworkManager.Singleton.StartHost();

            if (mainMenuUI != null) mainMenuUI.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Lỗi tạo phòng: {e.Message}");
        }
    }

    // 2. NÚT VÀO PHÒNG BẰNG CODE (JOIN)
    public async void JoinRoomClient()
    {
        string joinCode = joinCodeInputField.text.Trim();

        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogWarning("Vui lòng nhập mã phòng!");
            return;
        }

        try
        {
            Debug.Log($"Đang tham gia phòng bằng code: {joinCode}...");

            // Sử dụng API mới để Join Session bằng Code
            ISession session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);

            Debug.Log("Đã vào Session thành công! Đang kết nối Netcode Client...");

            // Bắt đầu Kết nối Client
            NetworkManager.Singleton.StartClient();

            if (mainMenuUI != null) mainMenuUI.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Lỗi tham gia phòng: {e.Message}");
        }
    }
    public void OnClickStartGameButton()
        {
            // Chỉ Host mới có quyền bấm nút Bắt Đầu Game
            if (NetworkManager.Singleton.IsHost)
            {
                GameMatchManager.Instance.StartMatchAndAssignRoles();
                
                // Ẩn UI Lobby đi
                if (mainMenuUI != null) mainMenuUI.SetActive(false);
            }
        }
}