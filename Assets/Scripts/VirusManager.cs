using System.Collections.Generic;
using UnityEngine;

public class VirusManager : MonoBehaviour
{
    public GridManager gridManager;

    public void NeutralizeVirus()
    {
        List<Vector2Int> firewallPositions = new List<Vector2Int>();
        foreach (var kvp in gridManager.gridMap)
        {
            if (kvp.Value.type == GridTileType.Firewall)
                firewallPositions.Add(kvp.Key);
        }

        Vector2Int[] dirs8 = { 
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };
        
        foreach (Vector2Int fPos in firewallPositions)
        {
            foreach (Vector2Int dir in dirs8)
            {
                Vector2Int targetPos = fPos + dir;
                if (gridManager.gridMap.TryGetValue(targetPos, out TileData tile))
                {
                    if (tile.type == GridTileType.Malware && !tile.isNeutralized)
                    {
                        tile.isNeutralized = true;
                        
                        // FIX BUG: Lempar juga posisi akurat (targetPos) ke fungsi visual
                        ApplyNeutralizedVisual(tile.tileObject, targetPos); 
                        
                        Debug.Log($"Malware di {targetPos} berhasil dinetralisir oleh Firewall!");
                    }
                }
            }
        }
    }

    private void ApplyNeutralizedVisual(GameObject rootBlock, Vector2Int malwarePos)
    {
        if (rootBlock == null) return;

        // Ambil SEMUA kotak kecil di dalam blok prefab ini
        TileVFX[] allVFX = rootBlock.GetComponentsInChildren<TileVFX>();
        
        foreach (TileVFX vfx in allVFX)
        {
            // Cocokkan posisinya
            int vfxGridX = Mathf.RoundToInt(vfx.transform.position.x);
            int vfxGridY = Mathf.RoundToInt(vfx.transform.position.z);
            
            // HANYA MATIKAN kotak yang posisinya SAMA PERSIS dengan Malware
            if (vfxGridX == malwarePos.x && vfxGridY == malwarePos.y)
            {
                MeshRenderer[] renderers = vfx.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer r in renderers)
                {
                    r.material.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Ubah jadi abu-abu
                    r.material.DisableKeyword("_EMISSION"); 
                }
            }
        }
    }

    public void UpdateInfectionVisuals()
    {
        if (gridManager == null) return;

        // 1. Sapu bersih: Matikan semua ikon peringatan di lantai
        foreach (var kvp in gridManager.gridMap)
        {
            if (kvp.Value.tileObject != null)
            {
                // Gunakan GetComponentsInChildren agar semua kotak dalam 1 blok ikut mati ikonnya
                TileVFX[] allVFX = kvp.Value.tileObject.GetComponentsInChildren<TileVFX>();
                foreach (TileVFX vfx in allVFX)
                {
                    vfx.SetInfectedVisual(false);
                }
            }
        }

        Vector2Int[] dirs8 = { 
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        // 2. Cari Malware aktif dan nyalakan ikon HANYA di kotak sebelahnya
        foreach (var kvp in gridManager.gridMap)
        {
            if (kvp.Value.type == GridTileType.Malware && !kvp.Value.isNeutralized)
            {
                foreach (Vector2Int dir in dirs8)
                {
                    Vector2Int neighborPos = kvp.Key + dir;
                    
                    if (gridManager.gridMap.TryGetValue(neighborPos, out TileData neighborTile))
                    {
                        if (neighborTile.type != GridTileType.Malware && neighborTile.type != GridTileType.Firewall)
                        {
                            if (neighborTile.tileObject != null)
                            {
                                // Ambil semua kotak di dalam blok tetangga tersebut
                                TileVFX[] allNeighborVFX = neighborTile.tileObject.GetComponentsInChildren<TileVFX>();
                                
                                foreach (TileVFX nVfx in allNeighborVFX)
                                {
                                    // Cocokkan posisinya
                                    int vfxGridX = Mathf.RoundToInt(nVfx.transform.position.x);
                                    int vfxGridY = Mathf.RoundToInt(nVfx.transform.position.z);
                                    
                                    // HANYA NYALAKAN ikon di kotak (Tile 1x1) yang posisinya akurat!
                                    if (vfxGridX == neighborPos.x && vfxGridY == neighborPos.y)
                                    {
                                        nVfx.SetInfectedVisual(true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}