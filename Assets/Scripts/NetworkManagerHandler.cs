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

    // Single player button
    // Starts single player game
    public void StartSinglePlayer()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("NetworkManager is already running. Cannot start new game.");
            return;
        }

        isSinglePlayer = true;
        GameManager.isSinglePlayer = true;
        NetworkManager.Singleton.StartHost();
    }

    // Host a game
    public async void HostWithLobby()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("NetworkManager is already running. Cannot start new game.");
            return;
        }

        isSinglePlayer = false;

        // Create Relay allocation
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);


        // Setup UnityTransport with Relay
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(new RelayServerData(allocation, "udp"));

        // Start NGO host
        NetworkManager.Singleton.StartHost();

        // Create Unity Lobby and store join code in data
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
    }

    // Join as client
    public async void JoinWithLobbyCode(string lobbyCode)
    {
        
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

            // Join Relay allocation
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            // Set UnityTransport relay data
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(allocation, "udp"));

            // Start NGO client
            NetworkManager.Singleton.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
        }
    }

    // Clean Up - used in ChessPlayer.cs HandleReturnToTitle()
    public async Task CleanupNetworkState()
    {
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
