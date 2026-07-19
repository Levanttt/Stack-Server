using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TileOccupancy
{
    public Vector2Int position;
    public GridTileType type;
}

public class BlockData : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("Gambar 2D untuk ditampilkan di tombol UI Card (1, 2, 3)")]
    public Sprite blockIcon;

    [Header("Block Shape Occupancy")]
    [Tooltip("Daftar ubin yang menyusun blok ini, lengkap dengan tipenya.")]
    public List<TileOccupancy> localTiles = new List<TileOccupancy>();

    public List<TileOccupancy> GetRotatedTiles(float rotationAngle)
    {
        List<TileOccupancy> rotatedTiles = new List<TileOccupancy>();
        int angle = Mathf.RoundToInt(rotationAngle) % 360;

        foreach (TileOccupancy tile in localTiles)
        {
            Vector2Int rotatedPos = tile.position;
            
            if (angle == 90 || angle == -270)
                rotatedPos = new Vector2Int(tile.position.y, -tile.position.x);
            else if (angle == 180 || angle == -180)
                rotatedPos = new Vector2Int(-tile.position.x, -tile.position.y);
            else if (angle == 270 || angle == -90)
                rotatedPos = new Vector2Int(-tile.position.y, tile.position.x);

            TileOccupancy rotatedTile = new TileOccupancy
            {
                position = rotatedPos,
                type = tile.type
            };

            rotatedTiles.Add(rotatedTile);
        }

        return rotatedTiles;
    }
}