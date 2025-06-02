using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rook : Piece
{
    public bool hasMoved = false;

    public override bool isValidMove(Vector3 c, Vector3 t, Tile[,,] board)
    {
        bool b = ((c.x == t.x && c.y == t.y) ||
                (c.y == t.y && c.z == t.z) ||
                (c.z == t.z && c.x == t.x));

        if (!b)
        {
            return false;
        }
        
        return base.isValidMove(c, t, board);
    }

    public override string getCode()
    {
        return "R";
    }

    public override void moveTo(Vector3 t)
    {
        base.moveTo(t + positionOffset);
    }
}
