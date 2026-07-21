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

        // 2. Cek 8-arah (Atas, Bawah, Kiri, Kanan, + 4 Diagonal) dari setiap Firewall
        Vector2Int[] dirs8 = { 
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };
        
        foreach (Vector2Int fPos in firewallPositions)
        {
            foreach (Vector2Int dir in dirs8)
            {
                Vector2Int targetPos = fPos + dir;
                if (gridManager.gridMap.ContainsKey(targetPos))
                {
                    TileData tile = gridManager.gridMap[targetPos];
                    
                    // Jika itu Malware dan BELUM dinetralisir
                    if (tile.type == GridTileType.Malware && !tile.isNeutralized)
                    {
                        tile.isNeutralized = true;
                        ApplyNeutralizedVisual(tile.tileObject); // Panggil efek visual
                        Debug.Log($"Malware di {targetPos} berhasil dinetralisir oleh Firewall!");
                    }
                }
            }
        }
    }

    // --- FUNGSI BARU UNTUK MENGUBAH VISUAL ---
    private void ApplyNeutralizedVisual(GameObject virusObject)
    {
        if (virusObject == null) return;

        // Bikin visualnya jadi abu-abu kusam (Disable)
        MeshRenderer[] renderers = virusObject.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer r in renderers)
        {
            r.material.color = new Color(0.3f, 0.3f, 0.3f, 1f); 
            
            // Matikan efek menyala (emission) kalau material virusmu pakai glow
            r.material.DisableKeyword("_EMISSION"); 
        }
    }
}