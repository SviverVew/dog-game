using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class RoomListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button joinButton;

    public void Bind(Lobby lobby, LobbyBrowserUI owner)
    {
        roomNameText.text = lobby.Name;
        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
        joinButton.interactable = lobby.AvailableSlots > 0;
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => owner.JoinPublicRoom(lobby.Id));
    }
}
