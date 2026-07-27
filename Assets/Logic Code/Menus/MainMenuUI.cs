using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject menuPanel;     // Panel chứa nút Host / Join
    public GameObject inGamePanel;   // Panel hiển thị khi đã vào game (hoặc UI chơi game)

    [Header("UI Buttons")]
    public Button hostButton;
    public Button clientButton;
    public Button quitButton;

    [Header("Settings")]
    public string gameSceneName = "MainGameScene"; // Tên Scene chơi game của bạn

    private void Awake()
    {
        // Gán sự kiện click nút
        if (hostButton) hostButton.onClick.AddListener(StartHost);
        if (clientButton) clientButton.onClick.AddListener(StartClient);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);
    }

    private void Start()
    {
        if (menuPanel) menuPanel.SetActive(true);
        if (inGamePanel) inGamePanel.SetActive(false);
        
        // Hiện lại con trỏ chuột ở Menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton != null)
        {
            // Bắt đầu làm Host (Vừa là Server vừa là Người chơi 1)
            NetworkManager.Singleton.StartHost();
            HideMenuUI();
        }
        else
        {
            Debug.LogError("Không tìm thấy NetworkManager trong Scene!");
        }
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton != null)
        {
            // Bắt đầu làm Client (Người chơi kết nối vào Host)
            NetworkManager.Singleton.StartClient();
            HideMenuUI();
        }
        else
        {
            Debug.LogError("Không tìm thấy NetworkManager trong Scene!");
        }
    }

    private void HideMenuUI()
    {
        if (menuPanel) menuPanel.SetActive(false);
        if (inGamePanel) inGamePanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Đã thoát Game!");
    }
}