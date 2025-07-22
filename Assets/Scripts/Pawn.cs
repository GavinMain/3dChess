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
        int direction = isWhite.Value ? -1 : 1;

        if (t.x == c.x + direction &&
            Mathf.Abs(t.z - c.z) == 1 && t.y == c.y)
        {
            Tile targetTile = board[(int)t.x, (int)t.y, (int)t.z];
            if (targetTile.currentPiece != null &&
                targetTile.currentPiece.isWhite.Value != this.isWhite.Value)
            {
                return true; // valid capture
            }
        }

        //blocked by piece
        if (board[(int)t.x, (int)t.y, (int)t.z].currentPiece != null)
        {
            return false;
        }
        // One step forward
        if (t.x == c.x + direction &&
            t.y == c.y &&
            t.z == c.z)
        {
            return base.isValidMove(c, t, board);
        }

        // One step down
        if (t.x == c.x  &&
            t.y == c.y + direction &&
            t.z == c.z)
        {
            return base.isValidMove(c, t, board);
        }

        // One step up
        if (t.x == c.x  &&
            t.y == c.y - direction &&
            t.z == c.z)
        {
            return base.isValidMove(c, t, board);
        }

        // Two steps forward on first move
        if (!hasMoved &&
            t.x == c.x + (2 * direction) &&
            t.y == c.y &&
            t.z == c.z)
        {
            return base.isValidMove(c, t, board);
        }

        // Two steps down on first move
        if (!hasMoved &&
            t.x == c.x &&
            t.y == c.y + (2 * direction) &&
            t.z == c.z)
        {
            return base.isValidMove(c, t, board);
        }

        // Two steps up on first move
        if (!hasMoved &&
            t.x == c.x  &&
            t.y == c.y - (2 * direction)&&
            t.z == c.z)
        {
            return base.isValidMove(c, t, board);
        }

        return false;
    }

    public override string getCode()
    {
        return "P";
    }

    public override void moveTo(Vector3 t)
    {
        base.moveTo(t);
        hasMoved = true;
    }
}
