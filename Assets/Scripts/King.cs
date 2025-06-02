using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : Piece
{
    public bool hasMoved = false;

    public override bool isValidMove(Vector3 c, Vector3 t, Tile[,,] board)
    {
        float dx = Mathf.Abs(c.x - t.x);
        float dy = Mathf.Abs(c.y - t.y);
        float dz = Mathf.Abs(c.z - t.z);

        //doesnt work for vertical diagnals
        bool b = (dx <= 1 && dz <= 1 && c.y == t.y) ||
                (dz <= 1 && dy <= 1 && c.x == t.x) ||
                (dx <= 1 && dy <= 1 && c.z == t.z);
                
        return base.isValidMove(c, t, board) && b;
    }

    public override string getCode()
    {
        return "K";
    }

    public override void moveTo(Vector3 t)
    {
        base.moveTo(t + positionOffset);
    }
}
