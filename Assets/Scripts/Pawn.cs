using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece
{
    //Incomplete movement
    //no promotion
    public bool hasMoved = false;

    public override bool isValidMove(Vector3 c, Vector3 t, Tile[,,] board)
    {
        int direction = isWhite ? -1 : 1;

        // One step forward
        if (t.x == c.x + direction &&
            t.y == c.y &&
            t.z == c.z)
        {
            return true;
        }

        // One step down
        if (t.x == c.x  &&
            t.y == c.y + direction &&
            t.z == c.z)
        {
            return true;
        }

        // One step up
        if (t.x == c.x  &&
            t.y == c.y - direction &&
            t.z == c.z)
        {
            return true;
        }

        // Two steps forward on first move
        if (!hasMoved &&
            t.x == c.x + (2 * direction) &&
            t.y == c.y &&
            t.z == c.z)
        {
            return true;
        }

        // Two steps down on first move
        if (!hasMoved &&
            t.x == c.x &&
            t.y == c.y + (2 * direction) &&
            t.z == c.z)
        {
            return true;
        }

        // Two steps up on first move
        if (!hasMoved &&
            t.x == c.x  &&
            t.y == c.y - (2 * direction)&&
            t.z == c.z)
        {
            return true;
        }

        return false;
    }

    public override string getCode()
    {
        return "P";
    }

    public override void moveTo(Vector3 t)
    {
        base.moveTo(t + positionOffset);
        hasMoved = true;
    }
}
