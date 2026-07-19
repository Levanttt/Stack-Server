using System.Collections.Generic;
using UnityEngine;

public class VirusManager : MonoBehaviour
{
    public GridManager gridManager;

    public void NeutralizeVirus()
    {
        // 1. Cari semua Firewall di grid
        List<Vector2Int> firewallPositions = new List<Vector2Int>();
        foreach (var kvp in gridManager.gridMap)
        {
            if (kvp.Value.type == GridTileType.Firewall)
                firewallPositions.Add(kvp.Key);
        }

        // 2. Cek 4-arah dari setiap Firewall
        Vector2Int[] dirs4 = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        foreach (Vector2Int fPos in firewallPositions)
        {
            foreach (Vector2Int dir in dirs4)
            {
                Vector2Int targetPos = fPos + dir;
                if (gridManager.gridMap.ContainsKey(targetPos))
                {
                    TileData tile = gridManager.gridMap[targetPos];
                    if (tile.type == GridTileType.Malware)
                    {
                        tile.isNeutralized = true;
                        Debug.Log($"Malware di {targetPos} berhasil dinetralisir oleh Firewall!");
                    }
                }
            }
        }
    }
}