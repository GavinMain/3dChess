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

        // Knight moves: 2 squares in one direction, 1 in the other
        bool isLShapedMove = (dx == 2 && dz == 1 && dy == 0) ||
                                (dx == 1 && dz == 2 && dy == 0) ||
                                (dx == 0 && dz == 2 && dy == 1) ||
                                (dx == 0 && dz == 1 && dy == 2) ||
                                (dx == 1 && dz == 0 && dy == 2) ||
                                (dx == 2 && dz == 0 && dy == 1);

        return base.isValidMove(c, t, board) && isLShapedMove;
    }

    public override string getCode()
    {
        return "N";
    }

    public override void moveTo(Vector3 t)
    {
        base.moveTo(t + positionOffset);
    }
}
