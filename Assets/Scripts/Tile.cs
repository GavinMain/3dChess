using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public Piece currentPiece;
    public Vector3 boardPos;
    public bool isValid = false;

    public Vector3 getPos()
    {
        return transform.position;
    }

}
