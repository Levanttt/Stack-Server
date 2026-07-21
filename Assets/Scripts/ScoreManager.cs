using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Manager References")]
    public GridManager gridManager;
    public ClusterManager clusterManager;

    [Header("Score Settings")]
    public int scoreDatabaseSingle = 5;       
    public int scoreDatabaseAdjacency = 15;   
    public int scoreACPlacement = 10;
    public int scoreFirewallPlacement = 20;
    public int scoreNeutralizedMalware = 50;
    public int scorePerDatabaseAdjacent = 5;
    public int scorePerCableAdjacent = 2;
    public int baseScorePerDatabaseInCluster = 10;
    
    public int penaltyOverheat = 5; // Penalti diturunkan agar lebih cozy

    [Header("Tetris Bonus Settings")]
    public int scoreFullLineBonus = 50; 
    
    // --- VARIABEL BARU UNTUK MENYIMPAN BONUS PERMANEN ---
    [HideInInspector] public int totalScore = 0;
    private int permanentBonusScore = 0; 
    private HashSet<string> claimedLines = new HashSet<string>(); // Buku catatan baris yang sudah diklaim

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private bool IsCable(GridTileType type)
    {
        return type == GridTileType.Kabel_I || type == GridTileType.Kabel_L || 
               type == GridTileType.Kabel_T || type == GridTileType.Kabel_Cross;
    }

    public void CalculateScore()
    {
        int localPlacementScore = 0;
        int connectionScore = 0;

        // 1. HITUNG POIN DASAR & OVERHEAT
        foreach (var kvp in gridManager.gridMap)
        {
            Vector2Int pos = kvp.Key;
            TileData tile = kvp.Value;

            bool isOverheating = false;
            if (tile.tileObject != null)
            {
                TileVFX vfx = tile.tileObject.GetComponentInChildren<TileVFX>();
                if (vfx != null && vfx.isOverheating)
                {
                    isOverheating = true;
                }
            }

            if (isOverheating) 
            {
                localPlacementScore -= penaltyOverheat; 
            }

            if (tile.type == GridTileType.Database)
            {
                if (!isOverheating) localPlacementScore += scoreDatabaseSingle; 
                
                List<TileData> neighbors = gridManager.GetOrthogonalNeighbors(pos);
                foreach (TileData neighbor in neighbors)
                {
                    if (neighbor.type == GridTileType.Database)
                        localPlacementScore += scoreDatabaseAdjacency;
                }
            }
            else if (tile.type == GridTileType.Cooling) localPlacementScore += scoreACPlacement;
            else if (tile.type == GridTileType.Firewall) localPlacementScore += scoreFirewallPlacement;
            else if (tile.type == GridTileType.Malware)
            {
                if (tile.isNeutralized) localPlacementScore += scoreNeutralizedMalware;
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

        // =========================================================
        // 2. SISTEM BONUS TETRIS (ONE-LINE FILL)
        // =========================================================
        
        // Cek Baris Mendatar (Horizontal / Sumbu X)
        for (int y = gridManager.minY; y < gridManager.maxY; y++)
        {
            // Buat ID unik berdasarkan ukuran lantai saat ini
            string lineKey = $"Row_{y}_Bounds_{gridManager.minX}_to_{gridManager.maxX}";
            
            if (!claimedLines.Contains(lineKey))
            {
                bool isRowFull = true;
                for (int x = gridManager.minX; x < gridManager.maxX; x++)
                {
                    if (!gridManager.gridMap.ContainsKey(new Vector2Int(x, y)))
                    {
                        isRowFull = false;
                        break;
                    }
                }
                if (isRowFull) 
                {
                    claimedLines.Add(lineKey); // Catat agar tidak diklaim ganda
                    permanentBonusScore += scoreFullLineBonus; // Masukkan ke tabungan permanen
                    StartCoroutine(PlayLineClearVFX(true, y)); // Mainkan efek animasi
                }
            }
        }

        // Cek Kolom Menurun (Vertikal / Sumbu Y)
        for (int x = gridManager.minX; x < gridManager.maxX; x++)
        {
            string lineKey = $"Col_{x}_Bounds_{gridManager.minY}_to_{gridManager.maxY}";
            
            if (!claimedLines.Contains(lineKey))
            {
                bool isColFull = true;
                for (int y = gridManager.minY; y < gridManager.maxY; y++)
                {
                    if (!gridManager.gridMap.ContainsKey(new Vector2Int(x, y)))
                    {
                        isColFull = false;
                        break;
                    }
                }
                if (isColFull) 
                {
                    claimedLines.Add(lineKey);
                    permanentBonusScore += scoreFullLineBonus;
                    StartCoroutine(PlayLineClearVFX(false, x));
                }
            }
        }

        // 3. HITUNG JARINGAN KABEL CLUSTER
        Dictionary<Vector2Int, int> tileToClusterID = new Dictionary<Vector2Int, int>();
        foreach (var cluster in clusterManager.activeClusters)
        {
            foreach (var pos in cluster.tiles) tileToClusterID[pos] = cluster.clusterID;
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

        // --- GABUNGKAN SEMUA SKOR (Termasuk Tabungan Permanen Tetris) ---
        totalScore = localPlacementScore + connectionScore + permanentBonusScore;

        if (MilestoneManager.Instance != null) MilestoneManager.Instance.CheckMilestone(totalScore);
        if (UIManager.Instance != null && MilestoneManager.Instance != null)
            UIManager.Instance.UpdateHUDScore(totalScore, MilestoneManager.Instance.CurrentTargetMilestone);

        Debug.Log($"[SCORE] Dasar: {localPlacementScore} | Kabel: {connectionScore} | Tetris Bonus: {permanentBonusScore} | TOTAL: {totalScore}");
    }

    // =========================================================
    // EFEK VISUAL: ANIMASI POP! ALA TETRIS CLEAR
    // =========================================================
    // =========================================================
    // EFEK VISUAL: ANIMASI POP! ALA TETRIS CLEAR (PER-KOTAK 1x1)
    // =========================================================
    private IEnumerator PlayLineClearVFX(bool isRow, int lineIndex)
    {
        List<Transform> tilesInLine = new List<Transform>();
        
        // Dictionary ini untuk mengingat ukuran asli baloknya (kalau-kalau tidak 1,1,1)
        Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();

        // Kumpulkan HANYA bagian kotak 1x1 yang ada di dalam baris/kolom tersebut
        if (isRow)
        {
            for (int x = gridManager.minX; x < gridManager.maxX; x++)
            {
                Vector2Int pos = new Vector2Int(x, lineIndex);
                if (gridManager.gridMap.TryGetValue(pos, out TileData tile))
                {
                    Transform specificTile = GetSpecificTileTransform(tile.tileObject, pos);
                    if (specificTile != null && !originalScales.ContainsKey(specificTile))
                    {
                        tilesInLine.Add(specificTile);
                        originalScales.Add(specificTile, specificTile.localScale);
                    }
                }
            }
        }
        else
        {
            for (int y = gridManager.minY; y < gridManager.maxY; y++)
            {
                Vector2Int pos = new Vector2Int(lineIndex, y);
                if (gridManager.gridMap.TryGetValue(pos, out TileData tile))
                {
                    Transform specificTile = GetSpecificTileTransform(tile.tileObject, pos);
                    if (specificTile != null && !originalScales.ContainsKey(specificTile))
                    {
                        tilesInLine.Add(specificTile);
                        originalScales.Add(specificTile, specificTile.localScale);
                    }
                }
            }
        }

        // Mainkan Animasi Membesar (Pop)
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Kurva PingPong: 0 -> 1 -> 0
            float scaleMultiplier = Mathf.PingPong(t * 2f, 1f);
            scaleMultiplier = scaleMultiplier * scaleMultiplier * (3f - 2f * scaleMultiplier); // Smoothstep
            
            foreach (Transform child in tilesInLine)
            {
                if (child != null) 
                {
                    Vector3 baseScale = originalScales[child];
                    Vector3 popScale = baseScale * 1.4f; // Membesar 40% dari ukuran aslinya
                    
                    // Skalakan HANYA balok 1x1 tersebut
                    child.localScale = Vector3.Lerp(baseScale, popScale, scaleMultiplier);
                }
            }
            yield return null;
        }

        // Pastikan kembali ke ukuran normal di akhir animasi
        foreach (Transform child in tilesInLine)
        {
            if (child != null) child.localScale = originalScales[child];
        }
    }

    // --- FUNGSI HELPER BARU: Mencari 1x1 spesifik di dalam blok besar (misal blok bentuk L) ---
    private Transform GetSpecificTileTransform(GameObject parentBlock, Vector2Int gridPos)
    {
        if (parentBlock == null) return null;

        // Hitung target posisi dunia (World Position) yang seharusnya untuk gridPos ini
        float targetX = gridPos.x * gridManager.cellSize;
        float targetZ = gridPos.y * gridManager.cellSize;
        float tolerance = gridManager.cellSize * 0.4f; // Toleransi jarak geser

        // Cari semua komponen visual (MeshRenderer) di dalam blok besar ini
        MeshRenderer[] renderers = parentBlock.GetComponentsInChildren<MeshRenderer>();
        
        foreach (MeshRenderer rend in renderers)
        {
            // Cek apakah posisi kotak visual ini cocok dengan target koordinat di lantai
            if (Mathf.Abs(rend.transform.position.x - targetX) < tolerance &&
                Mathf.Abs(rend.transform.position.z - targetZ) < tolerance)
            {
                return rend.transform; // Ketemu! Ini bagian spesifik 1x1 yang harus melompat
            }
        }

        // Kalau gagal (misal untuk balok 1x1 murni yang tidak punya anak GameObject), kembalikan utuh
        return parentBlock.transform;
    }
}