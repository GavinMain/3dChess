using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class ChessPlayer : NetworkBehaviour
{
    public NetworkVariable<bool> isWhite = new NetworkVariable<bool>();
    public Camera playerCamera;

    private GameManager gameManager;

    public int currentLayer = 7;

    // Assigns player color and starting camera location
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Camera playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }

        if (IsServer)
        {
            isWhite.Value = NetworkManager.Singleton.ConnectedClientsIds.Count == 1;
        }
        if (isWhite.Value)
        {
            transform.position = new Vector3(38f, 41f, 14f);
            transform.rotation = Quaternion.Euler(34, -90, 0);
        }
        else
        {
            transform.position = new Vector3(-10f, 41f, 14f);
            transform.rotation = Quaternion.Euler(34, 90, 0);
        }
    }

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    // Listens for player input
    void Update()
    {
        if (!IsOwner) return; 

        if (Input.GetKeyDown(KeyCode.Q))
        {
            SendSHLayerServerRpc(currentLayer, false);
            currentLayer = currentLayer == 0 ? 0 : currentLayer - 1;

        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            currentLayer = currentLayer == 7 ? 7 : currentLayer + 1;
            SendSHLayerServerRpc(currentLayer, true);
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            SendTileClickToServerServerRpc(ray);
            
        }
    }

    // Clean up clients     
    void OnSceneLoaded(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName == "MainMenu" && NetworkManager.Singleton.IsServer)
        {
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (clientId != NetworkManager.Singleton.LocalClientId)
                    NetworkManager.Singleton.DisconnectClient(clientId);
            }
        }
    }

    // Sends click information to GameManager for processing
    [ServerRpc]
    private void SendTileClickToServerServerRpc(Ray ray, ServerRpcParams rpcParams = default)
    {
            RaycastHit[] hits = Physics.RaycastAll(ray);

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {                
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null)
                {
                    if (tile.isValid || tile.currentPiece != null)
                    {
                        GameManager.Instance.OnTileClicked(tile.boardPos, rpcParams.Receive.SenderClientId);
                        break;
                    }
                }
            }
        
    }

    [ServerRpc]
    private void SendSHLayerServerRpc(int layer, bool show, ServerRpcParams rpcParams = default)
    {
        GameManager.Instance.ShowHideLayer(layer, show, rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    public void PromotePawnServerRpc(string pieceCode)
    {
        GameManager.Instance.PromotePawn(pieceCode);
    }

    // Handles return to Main Menu after game
    [ServerRpc(RequireOwnership = false)]
    private void RequestReturnToTitleServerRpc()
    {
        _ = HandleReturnToTitle();
        HandleReturnClientRpc();
    }

    public async Task HandleReturnToTitle()
    {
        NetworkManagerHandler networkHandler = FindObjectOfType<NetworkManagerHandler>();
        if (networkHandler != null)
        {
            await networkHandler.CleanupNetworkState();
        }

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        await Task.Delay(100);

        SceneManager.LoadScene("MainMenu");

    }

    [ClientRpc]
    private void HandleReturnClientRpc()
    {
        _ = HandleReturnToTitle();
    }

    public void OnClientPressedReturnToTitle()
    {
        RequestReturnToTitleServerRpc();
    }

}
