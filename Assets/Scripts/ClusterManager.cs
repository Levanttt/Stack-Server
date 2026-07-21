using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ClusterData
{
    public int clusterID;
    public int totalDatabase; // Jumlah murni database
    public int totalCoolingPower; // Total daya pendingin dari AC
    public List<Vector2Int> tiles = new List<Vector2Int>();
}

public class ClusterManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Cluster Rules")]
    [Tooltip("Jumlah batas maksimal Database berdempetan sebelum OVERHEAT (tanpa AC)")]
    public int baseHeatTolerance = 2; 
    
    [Tooltip("Satu kotak AC/Cooling bisa menahan berapa Database tambahan?")]
    public int coolingPowerPerAC = 3;

    [Header("Cluster Info")]
    public List<ClusterData> activeClusters = new List<ClusterData>();

    public void CalculateClusters()
    {
        activeClusters.Clear();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        int currentClusterID = 1;

        foreach (var kvp in gridManager.gridMap)
        {
            Vector2Int pos = kvp.Key;
            TileData tile = kvp.Value;

            if ((tile.type == GridTileType.Database || tile.type == GridTileType.Cooling) && !visited.Contains(pos))
            {
                ClusterData newCluster = new ClusterData { clusterID = currentClusterID };
                
                FloodFill(pos, visited, newCluster);
                
                activeClusters.Add(newCluster);
                currentClusterID++;
            }
        }

        // --- BAGIAN DEBUG LOG YANG DIPERBARUI ---
        foreach (var cluster in activeClusters)
        {
            int maxSafeCapacity = baseHeatTolerance + cluster.totalCoolingPower;
            bool isOverheat = cluster.totalDatabase > maxSafeCapacity;
            string status = isOverheat ? "OVERHEAT! Butuh AC!" : "Aman & Stabil";

            string koordinat = "";
            foreach(Vector2Int p in cluster.tiles) {
                koordinat += $"({p.x}, {p.y}) ";
            }

            // Sekarang status daya AC-nya ikut diprint ke Console!
            Debug.Log($"Cluster {cluster.clusterID} | DB: {cluster.totalDatabase} | Daya AC: {cluster.totalCoolingPower} | Max Aman: {maxSafeCapacity} | Status: {status} | Posisi: {koordinat}");
        }
    }

    private void FloodFill(Vector2Int startPos, HashSet<Vector2Int> visited, ClusterData cluster)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(startPos);
        visited.Add(startPos);

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            Vector2Int currentPos = queue.Dequeue();
            cluster.tiles.Add(currentPos);

            TileData currentTile = gridManager.gridMap[currentPos];
            
            // Hitung jumlah DB dan kekuatan AC
            if (currentTile.type == GridTileType.Database) cluster.totalDatabase += 1;
            if (currentTile.type == GridTileType.Cooling) cluster.totalCoolingPower += coolingPowerPerAC;

            foreach (Vector2Int d in dirs)
            {
                Vector2Int neighborPos = currentPos + d;

                if (!visited.Contains(neighborPos) && gridManager.gridMap.ContainsKey(neighborPos))
                {
                    TileData neighborTile = gridManager.gridMap[neighborPos];
                    
                    if (neighborTile.type == GridTileType.Database || neighborTile.type == GridTileType.Cooling)
                    {
                        visited.Add(neighborPos);
                        queue.Enqueue(neighborPos);
                    }
                }
            }
        }
    }
}