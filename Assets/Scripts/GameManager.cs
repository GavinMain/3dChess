using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


//no win condition right now
public class GameManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public GameObject pawnPrefab;
    public GameObject rookPrefab;
    public GameObject knightPrefab;
    public GameObject bishopPrefab;
    public GameObject queenPrefab;
    public GameObject kingPrefab;
    public Material whiteTileMaterial;
    public Material blackTileMaterial;
    public Material greenMaterial;

    public float scale = 4;

    private bool hasSelected = false;
    private Tile selectedTile = null;
    private Tile[,,] board = new Tile[8, 8, 8];
    private string f = "Test.txt";

    private bool whiteTurn = true;
    private Transform folder;


    // Start is called before the first frame update
    void Start()
    {
        folder = new GameObject("Folder").transform;
        placeBoard();
        string filePath = Path.Combine(Application.dataPath, "BoardStates", f);
        if (File.Exists(filePath))
        {
            loadBoard(f);
        }
        else
        {
            for (int i = 0; i < 8; ++i)
            {
                codeToPieceOnBoard("P0", 7, i, 1);
                codeToPieceOnBoard("P1", 7, i, 6);
            }
            codeToPieceOnBoard("R0", 7, 0, 0);
            codeToPieceOnBoard("R1", 7, 0, 7);
            codeToPieceOnBoard("N0", 7, 1, 0);
            codeToPieceOnBoard("N1", 7, 1, 7);
            codeToPieceOnBoard("B0", 7, 2, 0);
            codeToPieceOnBoard("B1", 7, 2, 7);
            codeToPieceOnBoard("Q0", 7, 3, 0);
            codeToPieceOnBoard("Q1", 7, 3, 7);
            codeToPieceOnBoard("K0", 7, 4, 0);
            codeToPieceOnBoard("K1", 7, 4, 7);
            codeToPieceOnBoard("R0", 7, 7, 0);
            codeToPieceOnBoard("R1", 7, 7, 7);
            codeToPieceOnBoard("N0", 7, 6, 0);
            codeToPieceOnBoard("N1", 7, 6, 7);
            codeToPieceOnBoard("B0", 7, 5, 0);
            codeToPieceOnBoard("B1", 7, 5, 7);
            saveBoard(f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) //left click
        {
            onClick();
        }
    }

    void placeBoard()
    {
        //down to up y value
        for (float height = 0f; height < 8f; ++height)
        {
            //left to right z value
            for (float col = 0f; col < 8f; ++col)
            {
                //forward to backward x value
                for (float row = 0f; row < 8f; ++row)
                {
                    //prefab is about twice the size of a normal cube
                    GameObject tile = Instantiate(tilePrefab, new Vector3(row * scale, height * scale, col * scale), Quaternion.identity, folder);
                    tile.transform.localScale *= (0.5f * scale);
                    bool iswhite = (row + col + height) % 2 == 0;
                    Material m = iswhite ? whiteTileMaterial : blackTileMaterial;

                    tile.GetComponent<Renderer>().material = m;

                    Tile t = tile.AddComponent<Tile>();
                    t.currentPiece = null;

                    board[(int)row, (int)height, (int)col] = t;
                    board[(int)row, (int)height, (int)col].boardPos = new Vector3(row, height, col);
                }
            }
        }
    }

    string boardToString()
    {
        List<string> str = new List<string>();
        //down to up y value
        for (int height = 0; height < 8; ++height)
        {
            //left to right z value
            for (int col = 0; col < 8; ++col)
            {
                List<string> rowStr = new List<string>();
                //forward to backward x value
                for (int row = 0; row < 8; ++row)
                {

                    Tile tile = board[row, height, col];
                    // Example: "P1" for white pawn, "r0" for black rook, "00" for empty
                    string code = "";
                    if (tile.currentPiece == null)
                    {
                        code += "00";
                    }
                    else
                    {
                        code += tile.currentPiece.getCode();
                        code += tile.currentPiece.isWhite ? "1" : "0";
                    }
                    rowStr.Add(code);
                }
                str.Add(string.Join(",", rowStr));

            }
        }
        return string.Join(";", str);
    }

    void codeToPieceOnBoard(string code, int height, int col, int row)
    {
        char pieceType = code[0];
        if (pieceType != '0')
        {
            bool isWhite = code[1] == '1' ? true : false;
            GameObject prefab = null;

            switch (pieceType)
            {
                case 'P':
                    prefab = pawnPrefab;
                    break;
                case 'R':
                    prefab = rookPrefab;
                    break;
                case 'N':
                    prefab = knightPrefab;
                    break;
                case 'B':
                    prefab = bishopPrefab;
                    break;
                case 'Q':
                    prefab = queenPrefab;
                    break;
                case 'K':
                    prefab = kingPrefab;
                    break;
                default:
                    Debug.LogWarning($"Unknown piece type: {pieceType}");
                    break;
            }
            Quaternion rotation = isWhite
                ? Quaternion.Euler(0, 90, 0)   // +X direction
                : Quaternion.Euler(0, -90, 0); // -X direction
            GameObject piece = Instantiate(prefab, board[row, height, col].getPos() + prefab.transform.position, rotation, folder);

            Piece p = null;
            switch (pieceType)
            {
                case 'P':
                    p = piece.AddComponent<Pawn>();
                    p.isWhite = isWhite;
                    break;
                case 'R':
                    p = piece.AddComponent<Rook>();
                    p.isWhite = isWhite;
                    break;
                case 'N':
                    p = piece.AddComponent<Knight>();
                    p.isWhite = isWhite;
                    break;
                case 'B':
                    p = piece.AddComponent<Bishop>();
                    p.isWhite = isWhite;
                    break;
                case 'Q':
                    p = piece.AddComponent<Queen>();
                    p.isWhite = isWhite;
                    break;
                case 'K':
                    p = piece.AddComponent<King>();
                    p.isWhite = isWhite;
                    break;
                default:
                    Debug.LogWarning($"Unknown piece type: {pieceType}");
                    break;
            }
            p.GetComponent<Renderer>().material = isWhite ? whiteTileMaterial : blackTileMaterial;
            board[row, height, col].currentPiece = p;
            board[row, height, col].currentPiece.positionOffset = prefab.transform.position;

        }
    }

    void saveBoard(string fileName)
    {
        string filePath = Path.Combine(Application.dataPath, "BoardStates", fileName);
        File.WriteAllText(filePath, boardToString());
    }

    void loadBoard(string fileName)
    {
        string filePath = Path.Combine(Application.dataPath, "BoardStates", fileName);
        if (File.Exists(filePath))
        {
            string currentBoardState = File.ReadAllText(filePath);
            string[] columns = currentBoardState.Split(';');

            int h = 0;
            int r = 0;
            int c = 0;
            foreach (string col in columns)
            {
                string[] values = col.Split(',');
                foreach (string val in values)
                {

                    codeToPieceOnBoard(val, h, c, r);
                    r = (r + 1) % 8;
                }
                if (c == 7)
                {
                    h += 1;
                }
                c = (c + 1) % 8;
            }



        }
        else
        {
            Debug.LogWarning("No save file found at: " + filePath);
        }
    }

    void onClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        // Sort the hits by distance (closest first)
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            GameObject obj = hit.collider.gameObject;

            Tile tile = obj.GetComponent<Tile>();

            if (tile != null)
            {
                if (tile.currentPiece == null && !tile.isValid)
                {
                    continue;
                }
                if (tile.currentPiece != null && tile.currentPiece.isWhite != whiteTurn && !hasSelected)
                {
                    break;
                }
                if (hasSelected)
                {

                    if (selectedTile.currentPiece.isValidMove(selectedTile.boardPos, tile.boardPos, board))
                    {
                        if (tile.currentPiece != null)
                        {
                            Destroy(tile.currentPiece.gameObject, 1f);
                            tile.currentPiece = null;
                        }
                        selectedTile.currentPiece.moveTo(tile.getPos());
                        tile.currentPiece = selectedTile.currentPiece;
                        selectedTile.currentPiece = null;
                        hasSelected = false;
                        selectedTile = null;
                        resetDisplayedValidMoves();
                        whiteTurn = !whiteTurn;
                    }
                    else
                    {
                        hasSelected = false;
                        selectedTile = null;
                        resetDisplayedValidMoves();
                    }
                }
                else
                {
                    if (tile.currentPiece != null)
                    {
                        hasSelected = true;
                        selectedTile = tile;
                        showValidMoves(tile.currentPiece);
                    }
                }
                break;
            }
        }
    }

    void showValidMoves(Piece p)
    {
        for (int height = 0; height < 8; ++height)
        {
            //left to right z value
            for (int col = 0; col < 8; ++col)
            {
                //forward to backward x value
                for (int row = 0; row < 8; ++row)
                {
                    if (p.isValidMove(selectedTile.boardPos, new Vector3(row, height, col), board))
                    {
                        board[row, height, col].GetComponent<Renderer>().material = greenMaterial;
                        board[row, height, col].isValid = true;

                    }

                }
            }
        }
    }

    void resetDisplayedValidMoves()
    {
        for (int height = 0; height < 8; ++height)
        {
            //left to right z value
            for (int col = 0; col < 8; ++col)
            {
                //forward to backward x value
                for (int row = 0; row < 8; ++row)
                {
                    bool iswhite = (row + col) % 2 == 0;
                    Material m = iswhite ? whiteTileMaterial : blackTileMaterial;

                    board[row, height, col].GetComponent<Renderer>().material = m;
                    board[row, height, col].isValid = false;
                }
            }
        }
    }

}
