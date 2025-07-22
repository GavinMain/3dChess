using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Piece : NetworkBehaviour
{
    public NetworkVariable<bool> isWhite = new NetworkVariable<bool>(
        default,  // Default value
        NetworkVariableReadPermission.Everyone,  // Anyone can read
        NetworkVariableWritePermission.Server  // Only server can write
    );
    public Vector3 positionOffset;

    // Virtual method to be overridden by each piece type
    public virtual bool isValidMove(Vector3 c, Vector3 t, Tile[,,] board)
    {
        if (c == t || (board[(int)t.x, (int)t.y, (int)t.z].currentPiece != null && board[(int)t.x, (int)t.y, (int)t.z].currentPiece.isWhite.Value == isWhite.Value))
        {
            return false;
        }

        int xDirection = t.x == c.x ? 0 : (t.x > c.x ? 1 : -1);
        int yDirection = t.y == c.y ? 0 : (t.y > c.y ? 1 : -1);
        int zDirection = t.z == c.z ? 0 : (t.z > c.z ? 1 : -1);
    
        int x = (int) c.x + xDirection;
        int y = (int) c.y + yDirection;
        int z = (int) c.z + zDirection;

        if (board[x, y, z].currentPiece != null && board[x, y, z].currentPiece.isWhite.Value == isWhite.Value )
        {
            return false;
        }
        
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

        if (board[x, y, z].currentPiece != null && board[x, y, z].currentPiece.isWhite.Value == isWhite.Value )
        {
            return false;
        }

        return true;
    }

    // Called when the object is spawned on the network (on both server and clients)
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Apply the material based on the synced isWhite value
        //Debug.Log("Spawn");
        ApplyMaterial();

        // Optional: Listen for changes to isWhite (if it could change mid-game)
        isWhite.OnValueChanged += OnIsWhiteChanged;
    }

    // Optional: Clean up listener on despawn
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isWhite.OnValueChanged -= OnIsWhiteChanged;
    }

    // Method to apply material (called on spawn or change)
    public void ApplyMaterial()
    {
        if (IsClient && !IsServer)
        {
            Debug.Log("Client: material check");
        }
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Reference materials from GameManager (centralized)
            renderer.material = isWhite.Value ? GameManager.whiteTileMaterial : GameManager.blackTileMaterial;
        }
    }

    // Callback for if isWhite changes (unlikely for chess, but good practice)
    private void OnIsWhiteChanged(bool oldValue, bool newValue)
    {
        ApplyMaterial();  // Re-apply if isWhite changes
    }

    public virtual string getCode()
    {
        return "";
    }

    public virtual void moveTo(Vector3 t)
    {
        //StartCoroutine(moveOverTime(t, 1f));
    }
    public ulong GetNetworkId()
    {
        return GetComponent<NetworkObject>().NetworkObjectId;
    }

    private IEnumerator moveOverTime(Vector3 target, float duration)
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

