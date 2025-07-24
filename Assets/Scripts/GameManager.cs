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
    // Static materials used in Piece.cs ApplyMaterial()
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
    private Tile promotingTile = null;
    private bool gameEnded = false;
    private bool isCastling = false;

    // Piece creation mappings
    private Dictionary<char, GameObject> piecePrefabs;
    public static GameManager Instance { get; private set; }
    

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

        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        whiteTileMaterial = w;
        blackTileMaterial = b;
        greenMaterial = g;
    }

    // Create board
    // Save or load pieces (future implement)
    void Start()
    {
        piecesFolder = new GameObject("PiecesFolder").transform;

        if (IsServer)
        {
            // Server-only: Handle board creation, loading, and setup
            CreateBoard();  
            string filePath = GetSaveFilePath();
            if (File.Exists(filePath))
            {
                LoadBoard(filePath); 
            }
            else
            {
                SetupInitialPieces();
                SaveBoard(filePath);
            }
        }
        else if (IsClient)
        {
            CreateBoard();
        }
    }

    // Initialize board as Tile array. Place Tiles in world
    private void CreateBoard()
    {
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

    // Initalizes board with default pieces (if no save file exists)
    private void SetupInitialPieces()
    {
        // Place pawns
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            PlacePiece("P0", BOARD_SIZE - 1, i, 1); // Black pawns
            PlacePiece("P1", BOARD_SIZE - 1, i, BOARD_SIZE - 2); // White pawns 
        }

        // Place other pieces 
        string[] pieceOrder = { "R", "N", "B", "Q", "K", "B", "N", "R" };
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            PlacePiece($"{pieceOrder[i]}0", BOARD_SIZE - 1, i, 0); // Black pieces
            PlacePiece($"{pieceOrder[i]}1", BOARD_SIZE - 1, i, BOARD_SIZE - 1); // White pieces
        }
    }

    // Place individual Piece 
    // x, y, z are board indices
    // Notice it is y, z, x (this made sense in the beginning of development)
    private void PlacePiece(string code, int y, int z, int x)
    {
        // Get Piece Information
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

        // Initialize Piece
        GameObject pieceObj = Instantiate(prefab, tilePos + prefab.transform.position, rotation);

        Piece piece = pieceObj.GetComponent<Piece>();
        if (piece == null)
        {
            Debug.LogError($"Piece component missing on prefab for type {pieceType}");
            Destroy(pieceObj);
            return;
        }

        // Update data
        piece.isWhite.Value = isWhite; 
        board[x, y, z].currentPiece = piece;
        board[x, y, z].currentPiece.positionOffset = prefab.transform.position;

        // Spawn as network object
        NetworkObject netObj = pieceObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(); 
        }
    }

    // Converts board state into string
    // Format: Each block is a layer (0-7), separated by empty line
    // Each layer is 8 rows, 8 col like a usual chess board
    /*
        Example, populated layer on top of empty layer:
        00;00;00;00;00;00;00;00
        00;00;00;00;00;00;00;00
        00;00;00;00;00;00;00;00
        00;00;00;00;00;00;00;00
        00;00;00;00;00;00;00;00
        00;00;00;00;00;00;00;00
        00;00;00;00;00;00;00;00
        00;00;00;00;00;00;00;00

        R0;N0;B0;Q0;K0;B0;N0;R0
        P0;P0;P0;P0;P0;P0;P0;P0
        00;00;00;00;00;00;00;00
        00;00;00;00;00;00;00;00
        00;00;00;00;00;00;00;00
        00;00;00;00;00;00;00;00
        P1;P1;P1;P1;P1;P1;P1;P1
        R1;N1;B1;Q1;K1;B1;N1;R1
    */
    private string GetBoardStateString()
    {
        string[,] layerData = new string[8, 8]; 

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

        for (int y = 0; y < 8; y++) // For each layer
        {
            List<string> rows = new List<string>();
            for (int x = 0; x < 8; x++) // For each row in layer
            {
                rows.Add(layerData[y, x]);
            }
            layers.Add(string.Join("\n", rows));
        }

        return string.Join("\n\n", layers);
    }

    // Saves board state as file (future)
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

    // Loads board state as file (future)
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


    // Creates a folder for board states if it does not already exist
    private string GetSaveFilePath()
    {
        string directory = Path.Combine(System.Environment.CurrentDirectory, "BoardStates");

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        return Path.Combine(directory, saveFileName);
    }

    // Function to hanlde tile clicked by each player
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

        if (!IsPlayerTurn(player)) return;

        Tile tile = board[(int)tilePos.x, (int)tilePos.y, (int)tilePos.z];

        if (tile.currentPiece == null && !tile.isValid) return;

        if (!hasSelected && tile.currentPiece != null && tile.currentPiece.isWhite.Value != whiteTurn) return;

        // Already selected a Piece from previous click (Piece means Tile containing a Piece)
        if (hasSelected)
        {
            // Move Piece to new location
            if (selectedTile.currentPiece.isValidMove(selectedTile.boardPos, tile.boardPos, board))
            {
                // Used to set Piece specific flags
                // More useful in the future for animations
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
                        // Game End Logic
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
            else // User clicked off the selected Piece (Piece means Tile containing a Piece)
            {
                hasSelected = false;
                selectedTile = null;
                ResetValidMoveDisplay(player.currentLayer, clientId);
            }
        }
        else if (tile.currentPiece != null)  // Have not selected a Piece or clicked off a selected one (Piece means Tile containing a Piece)
        {
            hasSelected = true;
            selectedTile = tile;
            ShowValidMoves(tile.currentPiece, clientId);
        }
    }
    // Used in King.cs to set a flag
    public void Castle()
    {
        isCastling = true;
    }

    // Game End / End Screen Logic
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

    // Promotion Logic
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

    // Client handling Logic
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

    // Move Piece Logic
    public void RequestMovePiece(NetworkObject pieceNetObj, Vector3 newPosition)
    {
        // Only the server processes the move for authority
        if (IsServer)
        {
            MovePieceServer(pieceNetObj, newPosition);
        }
        else
        {
            RequestMovePieceServerRpc(pieceNetObj.NetworkObjectId, newPosition);
        }
    }

    [ServerRpc(RequireOwnership = false)]  
    private void RequestMovePieceServerRpc(ulong pieceNetworkId, Vector3 newPosition)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceNetworkId, out NetworkObject pieceNetObj))
        {
            MovePieceServer(pieceNetObj, newPosition);
        }
    }

    private void MovePieceServer(NetworkObject pieceNetObj, Vector3 newPosition)
    {
        if (pieceNetObj != null)
        {
            Piece piece = pieceNetObj.GetComponent<Piece>();
            if (piece != null)
            {
                // Set the position directly on the server
                // NetworkTransform will sync this to clients with interpolation
                // (future): replace with animation
                pieceNetObj.transform.position = newPosition;  
                
            }
        }
    }

    // Castling Logic
    public void RequestCastle(NetworkObject kingPiece, NetworkObject rookPiece)
    {
        // Only the server processes the move for authority
        if (IsServer)
        {
            CastleServer(kingPiece, rookPiece);
        }
        else
        {
            RequestCastleServerRpc(kingPiece.NetworkObjectId, rookPiece.NetworkObjectId);
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

    [ServerRpc(RequireOwnership = false)] 
    private void RequestCastleServerRpc(ulong kingPieceNetworkId, ulong rookPieceNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(kingPieceNetworkId, out NetworkObject kPieceNetObj) && 
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(rookPieceNetworkId, out NetworkObject rPieceNetObj))
        {
            CastleServer(kPieceNetObj, rPieceNetObj);
        }
    }

    // Tile color change Logic
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

    // Highlights all valid moves of current piece
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
    
    // Highlights the King Tile in red if it is in check
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

    // Reset all highlighted tiles
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

    // Iterates through the board in down -> up (y), left -> right (z), forward -> backward (x)
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