# 3dChess
This is an implementation of chess with vertical movement in Unity.

To edit / run, open this project in Unity 6 via Unity Hub
Specific Instructions:
Download Unity Hub: https://unity.com/download
In Unity Hub, click on Add

If there are missing textures in the editor, download and import this: https://assetstore.unity.com/packages/3d/props/low-poly-chess-set-board-and-timer-216547?srsltid=AfmBOooFZRlYbNjlymStOru7y1q1Xtv-05Wla9-L8E3tpbNJ8_CLj83P

Game Controls:
Movement is WASD and hold right click to rotate camers. 
You can left click a chess piece to see possible movement options (green tiles)
    You can then left click a green tile to move the piece there
    Clicking a non-green tile resets the piece, click on a piece again to see green tiles
The turns go in order white -> black -> white etc
Clicking on a black tile during white's turn does nothing and vice versa

All the code is in the Assets/Scripts folder.

Currently, there are some know issues:
Pawn does not move properly and has no promotion
King's movement is incomplete (sort of)
There is no castling
No checkmate or check system
No win condition
No multiplayer
No Ai bot opponent
The knight's movement is bugged, but this is on purpose. The fix is to replace the return statement with:
        if (c == t || (board[(int)t.x, (int)t.y, (int)t.z].currentPiece != null && board[(int)t.x, (int)t.y, (int)t.z].currentPiece.isWhite == isWhite))
        {
            return false;
        }
        return isLShapedMove;