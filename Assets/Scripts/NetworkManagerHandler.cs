using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Networking.Transport.Relay;

public class NetworkManagerHandler : MonoBehaviour
{
    private bool isSinglePlayer = false;
    public Lobby currentLobby;

    private const int MAX_PLAYERS = 2;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (!isSinglePlayer && NetworkManager.Singleton.ConnectedClients.Count >= MAX_PLAYERS)
        {
            response.Approved = false;
            response.Reason = "Game is full";
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = true;
        response.PlayerPrefabHash = null;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (isSinglePlayer || NetworkManager.Singleton.ConnectedClients.Count == MAX_PLAYERS)
            {
                NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
            }
        }
    }

    // Call this from UI to start a single-player game
    public void StartSinglePlayer()
    {
        // Check if NetworkManager is already running
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("NetworkManager is already running. Cannot start new game.");
            return;
        }

        isSinglePlayer = true;
        GameManager.isSinglePlayer = true;
        NetworkManager.Singleton.StartHost();
    }

    // Call this from UI to host a multiplayer game
    public async void HostWithLobby()
    {
        // Check if NetworkManager is already running
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("NetworkManager is already running. Cannot start new game.");
            return;
        }

        isSinglePlayer = false;

        // 1. Create Relay allocation
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);


        // 2. Setup UnityTransport with Relay
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(new RelayServerData(allocation, "udp"));

        // 3. Start NGO host
        NetworkManager.Singleton.StartHost();

        // 4. Create Unity Lobby and store join code in data
        var options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Data = new Dictionary<string, DataObject>
            {
                { "relayCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
            }
        };

        currentLobby = await LobbyService.Instance.CreateLobbyAsync("ChessRoom", MAX_PLAYERS, options);

        Debug.Log($"Lobby Created. Join code: {currentLobby.LobbyCode}");
        // You can now show currentLobby.LobbyCode in the UI
    }

    // Call this from UI to join a game with a room code
    public async void JoinWithLobbyCode(string lobbyCode)
    {
        // Check if NetworkManager is already running
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("NetworkManager is already running. Cannot join new game.");
            return;
        }

        isSinglePlayer = false;

        try
        {
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            string relayJoinCode = currentLobby.Data["relayCode"].Value;

            // 1. Join Relay allocation
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            // 2. Set UnityTransport relay data
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(allocation, "udp"));

            // 3. Start NGO client
            NetworkManager.Singleton.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
        }
    }

    // Call this to properly clean up network state when returning to main menu
    public async Task CleanupNetworkState()
    {
        // Clean up lobby if we have one
        if (currentLobby != null)
        {
            try
            {
                if (NetworkManager.Singleton.IsHost)
                {
                    // If we're the host, delete the lobby
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                }
                else
                {
                    // If we're a client, leave the lobby
                    await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning($"Failed to clean up lobby: {e.Message}");
            }
            finally
            {
                currentLobby = null;
            }
        }

        // Reset single player flag
        isSinglePlayer = false;
        GameManager.isSinglePlayer = false;
    }
}
