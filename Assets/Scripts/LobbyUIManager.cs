using UnityEngine;
using UnityEngine.UI;
using TMPro; // Only needed if using TextMeshPro
using System;

public class LobbyUIManager : MonoBehaviour
{
    [Header("References")]
    public NetworkManagerHandler networkManager;
    public TMP_InputField joinCodeInput;         // Drag your JoinCodeInput here
    public TMP_Text roomCodeDisplayText;         // Drag RoomCodeDisplay here

    public void HostGame()
    {
        networkManager.HostWithLobby();

        // Show room code after short delay (relay + lobby creation is async)
        Invoke(nameof(UpdateRoomCodeDisplay), 2f);
    }

    public void JoinGame()
    {
        string code = joinCodeInput.text.Trim();
        if (!string.IsNullOrEmpty(code))
        {
            networkManager.JoinWithLobbyCode(code);
        }
        else
        {
            Debug.LogWarning("Join code is empty.");
        }
    }

    private void UpdateRoomCodeDisplay()
    {
        if (networkManager != null && networkManager.currentLobby != null)
        {
            roomCodeDisplayText.text = $"Room Code: {networkManager.currentLobby.LobbyCode}";
        }
        else
        {
            roomCodeDisplayText.text = "Room Code: ----";
        }
    }

    public void CopyRoomCodeToClipboard()
{
    if (networkManager != null && networkManager.currentLobby != null)
    {
        string code = networkManager.currentLobby.LobbyCode;
        GUIUtility.systemCopyBuffer = code;
        Debug.Log($"Copied room code to clipboard: {code}");
    }
    else
    {
        Debug.LogWarning("No room code to copy.");
    }
}
}
