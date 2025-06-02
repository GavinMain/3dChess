using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bishop : Piece
{
    public bool hasMoved = false;

    public override bool isValidMove(Vector3 c, Vector3 t, Tile[,,] board)
    {
        float dx = Mathf.Abs(c.x - t.x);
        float dy = Mathf.Abs(c.y - t.y);
        float dz = Mathf.Abs(c.z - t.z);

        bool b = dx == dz && c.y == t.y ||
                dx == dy && c.z == t.z ||
                dy == dz && c.x == t.x;
                
        if (!b) { return false; }
        
        return base.isValidMove(c, t, board);
    }

    public override string getCode()
    {
        return "B";
    }

    public override void moveTo(Vector3 t)
    {
        base.moveTo(t + positionOffset);
    }
}
