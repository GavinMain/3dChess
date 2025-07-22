// Assets/Scripts/GameManager.cs
using Unity.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class GameManager : NetworkBehaviour
{
    // Constants
    private const int BOARD_SIZE = 8;
    private const float TILE_SCALE_FACTOR = 0.5f;

    // Prefabs
    [Header("Prefabs")]
    public GameObject tilePrefab;
    public GameObject pawnPrefab;
    public GameObject rookPrefab;
    public GameObject knightPrefab;
    public GameObject bishopPrefab;
    public GameObject queenPrefab;
    public GameObject kingPrefab;

    // Materials
    [Header("Materials")]
    public Material w;
    public Material b;
    public Material g;
    public Material redMaterial;
    public Material transparentMaterial;
    public static Material whiteTileMaterial;
    public static Material blackTileMaterial;
    public static Material greenMaterial;

    // Configuration
    [Header("Configuration")]
    public float scale = 4f;
    private string saveFileName = "Test.txt";
    public static bool isSinglePlayer = false;

    // Game State
    private Tile[,,] board = new Tile[BOARD_SIZE, BOARD_SIZE, BOARD_SIZE];
    private Renderer[,,] tileRenderers = new Renderer[BOARD_SIZE, BOARD_SIZE, BOARD_SIZE];
    private bool whiteTurn = true;
    private bool hasSelected = false;
    private Tile selectedTile = null;
    private Transform piecesFolder;

    // Piece creation mappings
    private Dictionary<char, GameObject> piecePrefabs;
    private Dictionary<char, System.Type> pieceComponents;

    public static GameManager Instance { get; private set; }
    private Tile promotingTile = null;
    private bool gameEnded = false;
    private bool isCastling = false;

    void Awake()
    {
        // Initialize piece mappings
        piecePrefabs = new Dictionary<char, GameObject>
        {
            { 'P', pawnPrefab },
            { 'R', rookPrefab },
            { 'N', knightPrefab },
            { 'B', bishopPrefab },
            { 'Q', queenPrefab },
            { 'K', kingPrefab }
        };

        pieceComponents = new Dictionary<char, System.Type>
        {
            { 'P', typeof(Pawn) },
            { 'R', typeof(Rook) },
            { 'N', typeof(Knight) },
            { 'B', typeof(Bishop) },
            { 'Q', typeof(Queen) },
            { 'K', typeof(King) }
        };

        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        whiteTileMaterial = w;
        blackTileMaterial = b;
        greenMaterial = g;
    }

    //creates board
    void Start()
    {
        // Create the folder for organization (can run on all, as it's local)
        piecesFolder = new GameObject("PiecesFolder").transform;

        if (IsServer)
        {
            // Server-only: Handle board creation, loading, and setup
            CreateBoard();  // Assuming tiles can be local; see notes below
            string filePath = GetSaveFilePath();
            if (File.Exists(filePath))
            {
                LoadBoard(filePath);  // Ensure LoadBoard spawns on server
            }
            else
            {
                SetupInitialPieces();
                SaveBoard(filePath);
            }
        }
        else if (IsClient)
        {
            // Optional: Client-specific setup (e.g., UI or waiting logic)
            // No need to create board or pieces—wait for server sync

            CreateBoard();
           // ColorPieceClientRpc();
        }
    }

    private void CreateBoard()
    {
        //initializes board as a tile array
        IterateBoard((x, y, z) =>
        {
            Vector3 position = new Vector3(x * scale, y * scale, z * scale);
            GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, piecesFolder);
            tileObj.transform.localScale *= (TILE_SCALE_FACTOR * scale);

            bool isWhite = (x + y + z) % 2 == 0;
            tileRenderers[x, y, z] = tileObj.GetComponent<Renderer>();
            tileRenderers[x, y, z].material = isWhite ? whiteTileMaterial : blackTileMaterial;

            Tile tile = tileObj.GetComponent<Tile>();
            tile.currentPiece = null;
            tile.boardPos = new Vector3(x, y, z);
            board[x, y, z] = tile;
        });
    }

    //initalizes board with default placements
    private void SetupInitialPieces()
    {
        // Place pawns
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            PlacePiece("P0", BOARD_SIZE - 1, i, 1); // Black pawns
            PlacePiece("P1", BOARD_SIZE - 1, i, BOARD_SIZE - 2); // White pawns (corrected from original)
        }

        // Place other pieces (corrected positions from original)
        string[] pieceOrder = { "R", "N", "B", "Q", "K", "B", "N", "R" };
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            PlacePiece($"{pieceOrder[i]}0", BOARD_SIZE - 1, i, 0); // Black pieces
            PlacePiece($"{pieceOrder[i]}1", BOARD_SIZE - 1, i, BOARD_SIZE - 1); // White pieces
        }
    }

    private void PlacePiece(string code, int y, int z, int x)
    {
        char pieceType = code[0];
        if (pieceType == '0') return;

        bool isWhite = code[1] == '1';

        if (!piecePrefabs.TryGetValue(pieceType, out GameObject prefab))
        {
            Debug.LogWarning($"Unknown piece type: {pieceType}");
            return;
        }
        Vector3 tilePos = board[x, y, z].transform.position;
        Quaternion rotation = isWhite ? Quaternion.Euler(0, -90, 0) : Quaternion.Euler(0, 90, 0);

        GameObject pieceObj = Instantiate(prefab, tilePos + prefab.transform.position, rotation);

        // Get the Piece BEFORE Spawn
        Piece piece = pieceObj.GetComponent<Piece>();
        if (piece == null)
        {
            Debug.LogError($"Piece component missing on prefab for type {pieceType}");
            Destroy(pieceObj);
            return;
        }

        piece.isWhite.Value = isWhite; // set NetworkVariable BEFORE spawn
        board[x, y, z].currentPiece = piece;
        board[x, y, z].currentPiece.positionOffset = prefab.transform.position;

        NetworkObject netObj = pieceObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(); // spawn AFTER setting variables
        }
    }

    [ClientRpc]
    private void ColorPieceClientRpc()
    {
        IterateBoard((x, y, z) =>
        {
            Tile tile = board[x, y, z];
            if (tile.currentPiece != null)
            {
                tile.currentPiece.ApplyMaterial();
            }
        });
    }

    //converts board state into string
    //format: ;00,00,00,00,00,00,00,00;00,00...
    //each substring separated by ';' is a column (forwards -> backwards, -x -> x)
    private string GetBoardStateString()
    {
        // Prepare a 2D string array to hold each row per layer
        string[,] layerData = new string[8, 8]; // [layer (y), row (x)]

        IterateBoard((x, y, z) =>
        {
            Tile tile = board[x, y, z];
            string code = tile.currentPiece == null
                ? "00"
                : $"{tile.currentPiece.getCode()}{(tile.currentPiece.isWhite.Value ? "1" : "0")}";

            if (string.IsNullOrEmpty(layerData[y, x]))
                layerData[y, x] = code;
            else
                layerData[y, x] += ";" + code;
        });

        List<string> layers = new List<string>();

        for (int y = 0; y < 8; y++) // for each layer
        {
            List<string> rows = new List<string>();
            for (int x = 0; x < 8; x++) // for each row in layer
            {
                rows.Add(layerData[y, x]);
            }
            layers.Add(string.Join("\n", rows));
        }

        return string.Join("\n\n", layers);
    }

    private void SaveBoard(string filePath)
    {
        try
        {
            File.WriteAllText(filePath, GetBoardStateString());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save board: {e.Message}");
        }
    }

    private void LoadBoard(string filePath)
    {
        try
        {
            string boardState = File.ReadAllText(filePath);
            string[] layerStrings = boardState.Split(new[] { "\n\n" }, StringSplitOptions.None);

            for (int y = 0; y < layerStrings.Length && y < 8; y++) // Each layer
            {
                string[] rowStrings = layerStrings[y].Split('\n');

                for (int x = 0; x < rowStrings.Length && x < 8; x++) // Each row in layer
                {
                    string[] cells = rowStrings[x].Split(';');

                    for (int z = 0; z < cells.Length && z < 8; z++) // Each column in row
                    {
                        PlacePiece(cells[z], y, z, x);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load board: {e.Message}");
        }
    }


    private string GetSaveFilePath()
    {
        return Path.Combine(Application.dataPath, "BoardStates", saveFileName);
    }

    public void OnTileClicked(Vector3 tilePos, ulong clientId)
    {
        if (gameEnded)
        {
            return;
        }
        ChessPlayer player = GetPlayerByClientId(clientId);
        if (player == null)
        {
            Debug.LogWarning($"No ChessPlayer found for clientId {clientId}");
            return;
        }
        
        // Optional: check if it's player's turn before allowing input
        if (!IsPlayerTurn(player)) return;

        // Your existing logic from HandleClick() simplified:
        Tile tile = board[(int)tilePos.x, (int)tilePos.y, (int)tilePos.z];

        if (tile.currentPiece == null && !tile.isValid) return;

        if (!hasSelected && tile.currentPiece != null && tile.currentPiece.isWhite.Value != whiteTurn) return;

        if (hasSelected)
        {
            if (selectedTile.currentPiece.isValidMove(selectedTile.boardPos, tile.boardPos, board))
            {
                selectedTile.currentPiece.moveTo(tile.boardPos);

                NetworkObject piece = selectedTile.currentPiece.GetComponent<NetworkObject>();

                if (isCastling)
                {
                    NetworkObject targetPiece = tile.currentPiece.GetComponent<NetworkObject>();
                    RequestCastle(piece, targetPiece);
                    (tile.currentPiece, selectedTile.currentPiece) = (selectedTile.currentPiece, tile.currentPiece);
                }
                else
                {
                    if (tile.currentPiece != null)
                    {
                        if (tile.currentPiece.GetComponent<King>() != null)
                        {
                            GameEndServerRpc(whiteTurn);
                            return;
                        }
                        Destroy(tile.currentPiece.gameObject);
                    }
                    RequestMovePiece(piece, tile.getPos() + selectedTile.currentPiece.positionOffset);
                    tile.currentPiece = selectedTile.currentPiece;
                    selectedTile.currentPiece = null;
                }

                PromotePawnCheck(tile, clientId);
                hasSelected = false;
                isCastling = false;
                selectedTile = null;
                ResetValidMoveDisplay(player.currentLayer, clientId);
                LookForCheck(clientId);
                whiteTurn = !whiteTurn;

            }
            else
            {
                hasSelected = false;
                selectedTile = null;
                ResetValidMoveDisplay(player.currentLayer, clientId);
            }
        }
        else if (tile.currentPiece != null)
        {
            hasSelected = true;
            selectedTile = tile;
            ShowValidMoves(tile.currentPiece, clientId);
        }
    }
    public void Castle()
    {
        isCastling = true;
    }
    [ServerRpc]
    private void GameEndServerRpc(bool wT)
    {
        string s = wT ? "Winner: White" : "Winner: Black";
        ShowEndScreenClientRpc(s);
        gameEnded = true;
    }
    [ClientRpc]
    private void ShowEndScreenClientRpc(string s)
    {
        FindObjectOfType<EndScreenUI>().ShowPopup(s);
    }

    public void PromotePawnCheck(Tile t, ulong clientId)
    {
        if (t.currentPiece.GetComponent<Pawn>() != null &&
                t.boardPos.x == (t.currentPiece.isWhite.Value ? 0 : 7))
        {
            promotingTile = t;
            ShowPromotePawnUIClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            });
        }
    }
    
    [ClientRpc]
    private void ShowPromotePawnUIClientRpc(ClientRpcParams clientRpcParams = default)
    {
        FindObjectOfType<PromotionUI>().ShowPopup();
    }

    public void PromotePawn(string code)
    {
        code += (promotingTile.currentPiece.isWhite.Value ? "1" : "0");
        Destroy(promotingTile.currentPiece.gameObject);
        PlacePiece(code, (int)promotingTile.boardPos.y, (int)promotingTile.boardPos.z, (int)promotingTile.boardPos.x);
        promotingTile = null;

    }

    public ChessPlayer GetPlayerByClientId(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            return client.PlayerObject.GetComponent<ChessPlayer>();
        }
        return null;
    }

    private bool IsPlayerTurn(ChessPlayer player)
    {
        return (isSinglePlayer) || (player.isWhite.Value && whiteTurn) || (!player.isWhite.Value && !whiteTurn);
    }

    // Call this when a move needs to happen (e.g., from player input)
    public void RequestMovePiece(NetworkObject pieceNetObj, Vector3 newPosition)
    {
        // Only the server processes the move for authority
        if (IsServer)
        {
            // Directly set the position on the server (no RPC needed here)
            MovePieceServer(pieceNetObj, newPosition);
        }
        else
        {
            // Clients request the server to move via ServerRpc
            RequestMovePieceServerRpc(pieceNetObj.NetworkObjectId, newPosition);
        }
    }

    public void RequestCastle(NetworkObject kingPiece, NetworkObject rookPiece)
    {
        // Only the server processes the move for authority
        if (IsServer)
        {
            // Directly set the position on the server (no RPC needed here)
            CastleServer(kingPiece, rookPiece);
        }
        else
        {
            // Clients request the server to move via ServerRpc
            RequestCastleServerRpc(kingPiece.NetworkObjectId, rookPiece.NetworkObjectId);
        }
    }

    // ServerRpc: Called by clients to request a move
    [ServerRpc(RequireOwnership = false)]  // Allow any client to request (you can add validation)
    private void RequestMovePieceServerRpc(ulong pieceNetworkId, Vector3 newPosition)
    {
        // Server validates and gets the NetworkObject
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceNetworkId, out NetworkObject pieceNetObj))
        {
            // Optional: Add game logic validation (e.g., is the move legal?)
            MovePieceServer(pieceNetObj, newPosition);
        }
    }

    // Server-side method to apply the move
    private void MovePieceServer(NetworkObject pieceNetObj, Vector3 newPosition)
    {
        if (pieceNetObj != null)
        {
            Piece piece = pieceNetObj.GetComponent<Piece>();
            if (piece != null)
            {
                //Debug.Log($"Server moving piece {pieceNetObj.NetworkObjectId} to {newPosition}");
                // Set the position directly on the server
                // NetworkTransform will sync this to clients with interpolation
                pieceNetObj.transform.position = newPosition;  // Instant on server, interpolated on clients
                
                // Optional: If you want server-side animation, you could start a coroutine here,
                // but NetworkTransform's interpolation usually makes it unnecessary.
            }
        }
    }
    
    private void CastleServer(NetworkObject kingPiece, NetworkObject rookPiece)
    {
        if (kingPiece != null && rookPiece != null)
        {
            Piece kPiece = kingPiece.GetComponent<Piece>();
            Piece rPiece = rookPiece.GetComponent<Piece>();
            if (kPiece != null && rPiece != null)
            {
                (rPiece.transform.position, kPiece.transform.position) = (kPiece.transform.position, rPiece.transform.position);
            }
        }
    }

    // ServerRpc: Called by clients to request a move
    [ServerRpc(RequireOwnership = false)]  // Allow any client to request (you can add validation)
    private void RequestCastleServerRpc(ulong kingPieceNetworkId, ulong rookPieceNetworkId)
    {
        // Server validates and gets the NetworkObject
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(kingPieceNetworkId, out NetworkObject kPieceNetObj) && 
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(rookPieceNetworkId, out NetworkObject rPieceNetObj))
        {
            // Optional: Add game logic validation (e.g., is the move legal?)
            CastleServer(kPieceNetObj, rPieceNetObj);
        }
    }

    private void HighlightTile(Vector3Int tilePos, char mode, ulong? clientId = null)
{
    if (clientId.HasValue)
    {
        HighlightTileClientRpc(tilePos, mode, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId.Value }
            }
        });
    }
    else
    {
        HighlightTileClientRpc(tilePos, mode); // broadcast to all
    }
}

    [ClientRpc]
    private void HighlightTileClientRpc(Vector3Int tilePos, char mode, ClientRpcParams clientRpcParams = default)
    {

        Material m = null;
        Renderer tileRenderer = tileRenderers[tilePos.x, tilePos.y, tilePos.z];

        switch (mode)
        {
            case 'd':
                bool isWhite = (tilePos.x + tilePos.y + tilePos.z) % 2 == 0;
                m = isWhite ? whiteTileMaterial : blackTileMaterial;
                break;
            case 'w':
                m = whiteTileMaterial;
                break;
            case 'b':
                m = blackTileMaterial;
                break;
            case 'g':
                m = greenMaterial;
                break;
            case 'r':
                m = redMaterial;
                break;
            case 't':
                m = transparentMaterial;
                break;
            default:
                break;

        }
        tileRenderer.material = m;
    }

    //highlights all valid moves of current piece
    private void ShowValidMoves(Piece piece, ulong clientId)
    {
        IterateBoard((x, y, z) =>
        {
            Vector3 targetPos = new Vector3(x, y, z);
            if (piece.isValidMove(selectedTile.boardPos, targetPos, board))
            {
                Tile tile = board[x, y, z];
                tile.isValid = true;
                HighlightTile(new Vector3Int(x, y, z), 'g', clientId);
            }
        });
    }
    private void LookForCheck(ulong clientId)
    {
        Tile k = null;
        IterateBoard((x, y, z) =>
        {
            if (board[x, y, z].currentPiece != null &&
                    board[x, y, z].currentPiece.GetComponent<King>() != null &&
                    board[x, y, z].currentPiece.isWhite.Value != whiteTurn)
            {
                k = board[x, y, z];
            }
        });
        

        if (k != null)
        {
            IterateBoard((x, y, z) =>
            {
                if (board[x, y, z].currentPiece != null &&
                        board[x, y, z].currentPiece.isWhite.Value != k.currentPiece.isWhite.Value &&
                        board[x, y, z].currentPiece.isValidMove(new Vector3(x, y, z), k.boardPos, board))
                {
                    if (isSinglePlayer)
                    {
                        HighlightTile(new Vector3Int((int)k.boardPos.x, (int)k.boardPos.y, (int)k.boardPos.z), 'r', clientId);
                    }
                    else
                    {
                        HighlightTile(new Vector3Int((int)k.boardPos.x, (int)k.boardPos.y, (int)k.boardPos.z), 'r');
                        HighlightTile(new Vector3Int((int)k.boardPos.x, (int)k.boardPos.y, (int)k.boardPos.z), 'd', clientId);
                    }
                    
                }
            });
        }
    }

    //reset all highlighted tiles
    private void ResetValidMoveDisplay(int layer, ulong clientId)
    {
        IterateBoard((x, y, z) =>
        {
            Tile tile = board[x, y, z];
            if (tile.isValid)
            {
                tile.isValid = false;
                HighlightTile(new Vector3Int(x, y, z), y >= layer ? 't' : 'd', clientId);
            }
        });
    }
    public void ShowHideLayer(int layer, bool show, ulong clientId)
    {
        for (int x = 0; x < 8; ++x)
        {
            for (int z = 0; z < 8; ++z)
            {
                if (!board[x, layer, z].isValid)
                {
                    char param = show ? 'd' : 't';
                    HighlightTile(new Vector3Int(x, layer, z), param, clientId);
                }
            }
        }
    }

    //iterates through the board in down -> up (y), left -> right (z), forward -> backward (x)
    private void IterateBoard(System.Action<int, int, int> action)
    {
        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                for (int z = 0; z < BOARD_SIZE; z++)
                {
                    action(x, y, z);
                }
            }
        }
    }
    
    
}