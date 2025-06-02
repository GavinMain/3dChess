using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public bool isWhite;
    public Vector3 positionOffset;

    // Virtual method to be overridden by each piece type
    public virtual bool isValidMove(Vector3 c, Vector3 t, Tile[,,] board)
    {
        if (c == t || (board[(int)t.x, (int)t.y, (int)t.z].currentPiece != null && board[(int)t.x, (int)t.y, (int)t.z].currentPiece.isWhite == isWhite))
        {
            return false;
        }

        int xDirection = t.x == c.x ? 0 : (t.x > c.x ? 1 : -1);
        int yDirection = t.y == c.y ? 0 : (t.y > c.y ? 1 : -1);
        int zDirection = t.z == c.z ? 0 : (t.z > c.z ? 1 : -1);
    
        int x = (int) c.x + xDirection;
        int y = (int) c.y + yDirection;
        int z = (int) c.z + zDirection;
        
        // Check if path is clear
        while (x != t.x || y != t.y || z != t.z)
        {

            if (x > -1 && x < 8 && y > -1 && y < 8 && z > -1 && z < 8)
            {
                if (board[x, y, z].currentPiece != null)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            x += xDirection;
            y += yDirection;
            z += zDirection;
        }

        return true;
    }

    public virtual string getCode()
    {
        return "";
    }

    public virtual void moveTo(Vector3 t)
    {
        StartCoroutine(MoveOverTime(t, 1f));
    }

    private IEnumerator MoveOverTime(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target; // Ensure exact position at the end
    }
}

