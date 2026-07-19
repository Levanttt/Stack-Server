using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public ClusterManager clusterManager;

    [Header("Score Settings")]
    public int scoreDatabaseSingle = 5;       
    public int scoreDatabaseAdjacency = 15;   
    public int scoreACPlacement = 25;        
    public int scorePerCableAdjacent = 5; 
    public int scorePerDatabaseAdjacent = 10;
    public int baseScorePerDatabaseInCluster = 20;

    [Header("Live Score")]
    public int totalScore = 0;

    // Helper untuk ngecek apakah kotak ini termasuk varian kabel
    private bool IsCable(GridTileType type)
    {
        return type == GridTileType.Kabel_I || type == GridTileType.Kabel_L || 
               type == GridTileType.Kabel_T || type == GridTileType.Kabel_Cross;
    }

    public void CalculateScore()
    {
        totalScore = 0;
        int localPlacementScore = 0;
        int connectionScore = 0;

        // 1. HITUNG POIN PENEMPATAN LOKAL (Database, AC, Kabel)
        foreach (var kvp in gridManager.gridMap)
        {
            Vector2Int pos = kvp.Key;
            TileData tile = kvp.Value;

            // Skor Database
            if (tile.type == GridTileType.Database)
            {
                localPlacementScore += scoreDatabaseSingle; 
                
                List<TileData> neighbors = gridManager.GetOrthogonalNeighbors(pos);
                foreach (TileData neighbor in neighbors)
                {
                    if (neighbor.type == GridTileType.Database)
                        localPlacementScore += scoreDatabaseAdjacency;
                }
            }
            // Skor AC
            else if (tile.type == GridTileType.Cooling)
            {
                localPlacementScore += scoreACPlacement;
            }
            // Skor Kabel
            else if (IsCable(tile.type))
            {
                List<TileData> neighbors = gridManager.GetOrthogonalNeighbors(pos);
                foreach (TileData neighbor in neighbors)
                {
                    if (neighbor.type == GridTileType.Database) localPlacementScore += scorePerDatabaseAdjacent;
                    if (IsCable(neighbor.type)) localPlacementScore += scorePerCableAdjacent; 
                }
            }
        }

        // 2. HITUNG POIN MULTIPLIER KONEKSI (BFS Jaringan Kabel)
        Dictionary<Vector2Int, int> tileToClusterID = new Dictionary<Vector2Int, int>();
        foreach (var cluster in clusterManager.activeClusters)
        {
            foreach (var pos in cluster.tiles)
            {
                tileToClusterID[pos] = cluster.clusterID;
            }
        }

        HashSet<Vector2Int> visitedCables = new HashSet<Vector2Int>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var kvp in gridManager.gridMap)
        {
            if (IsCable(kvp.Value.type) && !visitedCables.Contains(kvp.Key))
            {
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                queue.Enqueue(kvp.Key);
                visitedCables.Add(kvp.Key);

                HashSet<int> connectedClusters = new HashSet<int>();

                while (queue.Count > 0)
                {
                    Vector2Int currentPos = queue.Dequeue();

                    foreach (Vector2Int d in dirs)
                    {
                        Vector2Int neighborPos = currentPos + d;
                        
                        if (gridManager.gridMap.ContainsKey(neighborPos))
                        {
                            TileData neighborTile = gridManager.gridMap[neighborPos];

                            if (IsCable(neighborTile.type) && !visitedCables.Contains(neighborPos))
                            {
                                visitedCables.Add(neighborPos);
                                queue.Enqueue(neighborPos);
                            }
                            else if (tileToClusterID.ContainsKey(neighborPos))
                            {
                                connectedClusters.Add(tileToClusterID[neighborPos]);
                            }
                        }
                    }
                }

                if (connectedClusters.Count > 1)
                {
                    int combinedClusterScore = 0;
                    foreach (int id in connectedClusters)
                    {
                        ClusterData cData = clusterManager.activeClusters.Find(c => c.clusterID == id);
                        if (cData != null)
                        {
                            combinedClusterScore += (cData.totalDatabase * baseScorePerDatabaseInCluster);
                        }
                    }
                    connectionScore += (combinedClusterScore * 2);
                }
            }
        }

        totalScore = localPlacementScore + connectionScore;
        Debug.Log($"[SCORE UPDATE] Lokal: {localPlacementScore} | Multiplier: {connectionScore} | TOTAL: {totalScore}");
    }

    public void CalculateMalwarePenalty()
    {
        foreach (var kvp in gridManager.gridMap)
        {
            if (kvp.Value.type == GridTileType.Malware && !kvp.Value.isNeutralized)
            {
                // Deteksi 8-arah (termasuk diagonal)
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        
                        Vector2Int neighborPos = kvp.Key + new Vector2Int(x, y);
                        if (gridManager.gridMap.ContainsKey(neighborPos))
                        {
                            // Malware mengurangi skor Database di sekitarnya
                            if (gridManager.gridMap[neighborPos].type == GridTileType.Database)
                                totalScore -= 50; // Penalty
                        }
                    }
                }
            }
        }
    }
}