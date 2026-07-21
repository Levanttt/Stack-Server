using System.Collections.Generic;
using UnityEngine;

public enum TileType { Kosong, Database, Cooling, Kabel_I, Kabel_L, Kabel_T, Kabel_Cross, Malware, Firewall }

public class GridLogic : MonoBehaviour
{
    public Dictionary<Vector2Int, TileData> gridData = new Dictionary<Vector2Int, TileData>();

    private readonly Vector2Int[] orthogonalDirs = new Vector2Int[]
    {
        Vector2Int.up,    
        Vector2Int.down,  
        Vector2Int.left,  
        Vector2Int.right 
    };

    /// <summary>
    /// Mengambil semua ubin yang menempel secara 4-arah dengan posisi target.
    /// </summary>
    public List<TileData> GetOrthogonalNeighbors(Vector2Int pos)
    {
        List<TileData> neighbors = new List<TileData>();

        foreach (Vector2Int dir in orthogonalDirs)
        {
            Vector2Int neighborPos = pos + dir;
            
            if (gridData.TryGetValue(neighborPos, out TileData neighborTile))
            {
                neighbors.Add(neighborTile);
            }
        }

        return neighbors;
    }
}