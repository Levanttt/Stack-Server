using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ClusterData
{
    public int clusterID;
    public int totalDatabase; 
    public List<Vector2Int> tiles = new List<Vector2Int>();
}

public class ClusterManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Cluster Rules")]
    public int baseHeatTolerance = 2; // Batas aman bawaan tanpa AC
    public int coolingPowerPerAC = 3; // Kuota hawa dingin per AC

    [Header("Cluster Info")]
    public List<ClusterData> activeClusters = new List<ClusterData>();

    public void CalculateClusters()
    {
        activeClusters.Clear();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        int currentClusterID = 1;

        // 1. CARI SEMUA KELOMPOK DATABASE
        foreach (var kvp in gridManager.gridMap)
        {
            Vector2Int pos = kvp.Key;
            TileData tile = kvp.Value;

            if (tile.type == GridTileType.Database && !visited.Contains(pos))
            {
                ClusterData newCluster = new ClusterData { clusterID = currentClusterID };
                FloodFillDBOnly(pos, visited, newCluster);
                activeClusters.Add(newCluster);
                currentClusterID++;
            }
        }

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        // 2. SIMULASI ALIRAN HAWA DINGIN (THERMAL FLOW)
        foreach (var cluster in activeClusters)
        {
            // Jika sejak awal kelompoknya kecil, aman semua!
            if (cluster.totalDatabase <= baseHeatTolerance)
            {
                SetClusterOverheatState(cluster, new HashSet<Vector2Int>()); // Kosong = aman semua
                continue;
            }

            // Cari semua AC yang nempel dengan kelompok ini
            HashSet<Vector2Int> attachedACs = new HashSet<Vector2Int>();
            foreach (Vector2Int p in cluster.tiles) 
            {
                foreach (Vector2Int d in dirs)
                {
                    Vector2Int neighborPos = p + d;
                    if (gridManager.gridMap.ContainsKey(neighborPos) && gridManager.gridMap[neighborPos].type == GridTileType.Cooling)
                    {
                        attachedACs.Add(neighborPos); 
                    }
                }
            }

            // Jika kelompok berlebih tapi TIDAK ADA AC sama sekali, semuanya merah!
            if (attachedACs.Count == 0)
            {
                SetClusterOverheatState(cluster, new HashSet<Vector2Int>(cluster.tiles)); // Semua masuk daftar overheat
                continue;
            }

            // --- ALGORITMA PENYEBARAN DINGIN ---
            int coolingQuota = baseHeatTolerance + (attachedACs.Count * coolingPowerPerAC);
            HashSet<Vector2Int> safeTiles = new HashSet<Vector2Int>(); // Daftar server yang selamat
            Queue<Vector2Int> queue = new Queue<Vector2Int>(); // Antrean penyebaran

            // Mulai dari server yang menempel langsung dengan AC
            foreach (Vector2Int acPos in attachedACs)
            {
                foreach (Vector2Int d in dirs)
                {
                    Vector2Int neighbor = acPos + d;
                    if (cluster.tiles.Contains(neighbor) && !safeTiles.Contains(neighbor))
                    {
                        if (coolingQuota > 0) 
                        {
                            safeTiles.Add(neighbor);
                            queue.Enqueue(neighbor);
                            coolingQuota--; // Kuota berkurang 1
                        }
                    }
                }
            }

            // Sebarkan dinginnya ke tetangga server secara merata sampai kuota habis
            while (queue.Count > 0 && coolingQuota > 0)
            {
                Vector2Int curr = queue.Dequeue();
                foreach (Vector2Int d in dirs)
                {
                    Vector2Int neighbor = curr + d;
                    if (cluster.tiles.Contains(neighbor) && !safeTiles.Contains(neighbor))
                    {
                        if (coolingQuota > 0)
                        {
                            safeTiles.Add(neighbor);
                            queue.Enqueue(neighbor);
                            coolingQuota--; // Kuota berkurang 1
                        }
                    }
                }
            }

            // Tentukan server mana yang tidak kebagian kuota dingin (Overheat)
            HashSet<Vector2Int> overheatedTiles = new HashSet<Vector2Int>();
            foreach (Vector2Int p in cluster.tiles)
            {
                if (!safeTiles.Contains(p)) 
                {
                    overheatedTiles.Add(p); // Ini yang ada di ujung dan kepanasan!
                }
            }

            // Eksekusi visualnya
            SetClusterOverheatState(cluster, overheatedTiles);
        }
    }

    // Fungsi pembantu untuk memicu lampu merah per kotak
    private void SetClusterOverheatState(ClusterData cluster, HashSet<Vector2Int> overheatedTiles)
    {
        foreach (Vector2Int p in cluster.tiles)
        {
            bool isOverheat = overheatedTiles.Contains(p);
            TileData tileData = gridManager.gridMap[p];
            
            if (tileData.tileObject != null)
            {
                TileVFX[] vfxList = tileData.tileObject.GetComponentsInChildren<TileVFX>();
                foreach (TileVFX vfx in vfxList)
                {
                    vfx.SetOverheatStatus(isOverheat);
                }
            }
        }
    }

    private void FloodFillDBOnly(Vector2Int startPos, HashSet<Vector2Int> visited, ClusterData cluster)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(startPos);
        visited.Add(startPos);

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            Vector2Int currentPos = queue.Dequeue();
            cluster.tiles.Add(currentPos);
            cluster.totalDatabase += 1;

            foreach (Vector2Int d in dirs)
            {
                Vector2Int neighborPos = currentPos + d;
                if (!visited.Contains(neighborPos) && gridManager.gridMap.ContainsKey(neighborPos))
                {
                    if (gridManager.gridMap[neighborPos].type == GridTileType.Database)
                    {
                        visited.Add(neighborPos);
                        queue.Enqueue(neighborPos);
                    }
                }
            }
        }
    }
}