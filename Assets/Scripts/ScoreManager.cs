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

    [Header("Milestone Settings")]
    // Fase Awal: Target yang diatur manual
    public List<int> milestoneTiers = new List<int> { 320, 800, 1500, 3000, 5000 };
    
    // Fase Endless: Penambahan target otomatis kalau list di atas sudah habis
    public int infiniteMilestoneStep = 2500; 

    private int currentMilestoneIndex = 0;
    private int currentTargetMilestone;

    [Header("Live Score")]
    public int totalScore = 0;

    private void Start()
    {
        // Set target pertama saat game mulai
        if (milestoneTiers.Count > 0)
            currentTargetMilestone = milestoneTiers[0];
        else
            currentTargetMilestone = infiniteMilestoneStep;
    }

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

        foreach (var kvp in gridManager.gridMap)
        {
            Vector2Int pos = kvp.Key;
            TileData tile = kvp.Value;

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
            else if (tile.type == GridTileType.Cooling)
            {
                localPlacementScore += scoreACPlacement;
            }
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
                            combinedClusterScore += (cData.totalDatabase * baseScorePerDatabaseInCluster);
                    }
                    connectionScore += (combinedClusterScore * 2);
                }
            }
        }

        totalScore = localPlacementScore + connectionScore;

        // LOGIKA ENDLESS MILESTONE
        while (totalScore >= currentTargetMilestone)
        {
            currentMilestoneIndex++;
            
            // Cek apakah masih dalam batas List manual
            if (currentMilestoneIndex < milestoneTiers.Count)
            {
                currentTargetMilestone = milestoneTiers[currentMilestoneIndex];
            }
            else
            {
                // Mode Endless: Tambahkan secara flat (misal +2500) ke target sebelumnya
                currentTargetMilestone += infiniteMilestoneStep;
            }

            Debug.Log($"[LEVEL UP] Milestone ke-{currentMilestoneIndex} Tercapai! Target baru: {currentTargetMilestone}");
            
            // --- EKSEKUSI REWARD LEVEL UP DI SINI ---
            // Nanti kamu bisa panggil fungsi tambah stock dan expand grid di sini
            TriggerMilestoneRewards();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHUDScore(totalScore, currentTargetMilestone);
        }

        Debug.Log($"[SCORE UPDATE] TOTAL: {totalScore}");
    }

    // Fungsi khusus untuk menampung efek setelah mencapai milestone
    private void TriggerMilestoneRewards()
    {
        // Contoh pemanggilan (uncomment kalau scriptnya sudah siap):
        
        /*
        PlacementSystem placement = FindObjectOfType<PlacementSystem>();
        if (placement != null)
        {
            placement.currentGlobalStock += 5; // Nambah stok block
        }

        if (gridManager != null)
        {
            gridManager.ExpandGrid(1); // Perluas grid 1 tile ke segala arah
        }
        */
    }

    public void ResetScore()
    {
        totalScore = 0;
        currentMilestoneIndex = 0;
        
        if (milestoneTiers.Count > 0)
            currentTargetMilestone = milestoneTiers[0];
        else
            currentTargetMilestone = infiniteMilestoneStep;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHUDScore(totalScore, currentTargetMilestone);
        }
    }
}