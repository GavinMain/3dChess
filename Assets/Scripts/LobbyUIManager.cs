using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LobbyUIManager : MonoBehaviour
{
    [Header("References")]
    public NetworkManagerHandler networkManager;
    public TMP_InputField joinCodeInput;         
    public TMP_Text roomCodeDisplayText;         

    // Host button
    public void HostGame()
    {
        networkManager.HostWithLobby();
        Invoke(nameof(UpdateRoomCodeDisplay), 2f);
    }

    // Client button
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

    // Shows room code
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

    //Copy button
    // Copies room code to clipboard
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
