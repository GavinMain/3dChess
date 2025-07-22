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
    // Start is called before the first frame update
    void Start()
    {
        panel.SetActive(false);

        if (NetworkManager.Singleton.LocalClient != null)
        {
            player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<ChessPlayer>();
        }
    }

    // Update is called once per frame
    public void ShowPopup(string s)
    {
        displayText.text = s;
        panel.SetActive(true);
    }

    public void returnToTitle()
    {
        player.OnClientPressedReturnToTitle();
    }

    
}
