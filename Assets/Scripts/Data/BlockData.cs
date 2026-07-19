using System.Collections.Generic;
using UnityEngine;

public class BlockData : MonoBehaviour
{
    [Header("Block Shape Ocupancy")]
    [Tooltip("Posisi ubin yang akan dimakan oleh blok ini (relatif terhadap titik pusat 0,0)")]
    public List<Vector2Int> localTiles = new List<Vector2Int> { new Vector2Int(0, 0) };

    public List<Vector2Int> GetRotatedTiles(float rotationAngle)
    {
        List<Vector2Int> rotatedTiles = new List<Vector2Int>();
        int angle = Mathf.RoundToInt(rotationAngle) % 360;

        foreach (Vector2Int tile in localTiles)
        {
            Vector2Int rotatedTile = tile;
            
            if (angle == 90 || angle == -270)
                rotatedTile = new Vector2Int(tile.y, -tile.x);
            else if (angle == 180 || angle == -180)
                rotatedTile = new Vector2Int(-tile.x, -tile.y);
            else if (angle == 270 || angle == -90)
                rotatedTile = new Vector2Int(-tile.y, tile.x);

            rotatedTiles.Add(rotatedTile);
        }

        return rotatedTiles;
    }
}