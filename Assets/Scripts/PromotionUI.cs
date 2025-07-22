using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PromotionUI : MonoBehaviour
{
    public GameObject panel;

    private ChessPlayer player; // Reference to the player NetworkBehaviour that contains the ServerRpc

    void Start()
    {
        panel.SetActive(false);

        // Get the local player’s PlayerNetwork script
        if (NetworkManager.Singleton.LocalClient != null)
        {
            player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<ChessPlayer>();
        }
    }

    public void ShowPopup()
    {
        panel.SetActive(true);
    }

    public void QueenSelected()
    {
        PromotePawn("Q");
    }

    public void RookSelected()
    {
        PromotePawn("R");
    }

    public void KnightSelected()
    {
        PromotePawn("N");
    }

    public void BishopSelected()
    {
        PromotePawn("B");
    }

    private void PromotePawn(string pieceCode)
    {
        if (player != null && player.IsOwner)
        {
            player.PromotePawnServerRpc(pieceCode);
        }
        else
        {
            Debug.LogWarning("Player reference missing or not the owner.");
        }

        panel.SetActive(false);
    }
}
