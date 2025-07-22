using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : Piece
{
    public bool hasMoved = false;
    private bool hasCastled = false;
    private Tile[,,] board = null;

    public override bool isValidMove(Vector3 c, Vector3 t, Tile[,,] _board)
    {
        board = _board;

        // Castling (still allowed to "move" into rook to trigger it?)
        if (!hasCastled && board[(int)t.x, (int)t.y, (int)t.z].currentPiece is Rook &&
                           board[(int)t.x, (int)t.y, (int)t.z].currentPiece.isWhite.Value == isWhite.Value)
        {
            return true;
        }

        float dx = Mathf.Abs(c.x - t.x);
        float dy = Mathf.Abs(c.y - t.y);
        float dz = Mathf.Abs(c.z - t.z);

        // King can move in any of the 26 adjacent positions
        bool b = dx <= 1 && dy <= 1 && dz <= 1 && (dx + dy + dz > 0);

        return base.isValidMove(c, t, board) && b;
    }


    public override string getCode()
    {
        return "K";
    }

    public override void moveTo(Vector3 t)
    {
        if (!hasCastled && board[(int)t.x, (int)t.y, (int)t.z].currentPiece is Rook)
        {
            hasCastled = true;
            GameManager.Instance.Castle();
        }
        else
        {
            base.moveTo(t);
        }
        
    }
    
}
