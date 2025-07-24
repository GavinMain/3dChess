using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Tile : MonoBehaviour
{
    public Piece currentPiece;  //Piece on Tile
    public Vector3 boardPos;   //Index of Tile on board
    public bool isValid = false;   //Valid move for current selected piece

    // Returns world position of Tile
    public Vector3 getPos()
    {
        return transform.position;
    }

}
