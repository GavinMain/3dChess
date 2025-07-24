using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;

public class EndScreenUI : MonoBehaviour
{
    public TMP_Text displayText;
    public GameObject panel;

    private ChessPlayer player;
    void Start()
    {
        panel.SetActive(false);

        if (NetworkManager.Singleton.LocalClient != null)
        {
            player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<ChessPlayer>();
        }
    }

    public void ShowPopup(string s)
    {
        displayText.text = s;
        panel.SetActive(true);
    }

    // Activated with button. Logic is in ChessPlayer.cs
    // Returns to Main Menu after game end
    public void returnToTitle()
    {
        player.OnClientPressedReturnToTitle();
    }

    
}
