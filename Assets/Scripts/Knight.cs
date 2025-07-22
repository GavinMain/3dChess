using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight : Piece
{
    public override bool isValidMove(Vector3 c, Vector3 t, Tile[,,] board)
    {
        float dx = Mathf.Abs(c.x - t.x);
        float dy = Mathf.Abs(c.y - t.y);
        float dz = Mathf.Abs(c.z - t.z);

        // Knight moves in 3D: 2 squares in one direction, 1 in another, 0 in the third
        bool isLShapedMove = (dx == 2 && dy == 1 && dz == 0) ||
                             (dx == 2 && dy == 0 && dz == 1) ||
                             (dx == 1 && dy == 2 && dz == 0) ||
                             (dx == 0 && dy == 2 && dz == 1) ||
                             (dx == 1 && dy == 0 && dz == 2) ||
                             (dx == 0 && dy == 1 && dz == 2);

        // For knights, we need to bypass the path checking in the base class
        // and only check if the destination is valid
        if (c == t || (board[(int)t.x, (int)t.y, (int)t.z].currentPiece != null && 
                       board[(int)t.x, (int)t.y, (int)t.z].currentPiece.isWhite == isWhite))
        {
            return false;
        }

        return isLShapedMove;
    }

    public override string getCode()
    {
        return "N";
    }

    public override void moveTo(Vector3 t)
    {
        base.moveTo(t);
    }
}
