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
    [Header("Cooling Settings")]
    public int acCoolingRadius = 2;
    public int penaltyOverheat = 20; 

    [Header("Tetris Bonus Settings")]
    public int scoreFullLineBonus = 50; 
    
    [HideInInspector] public int totalScore = 0;
    private int permanentBonusScore = 0; 
    private HashSet<string> claimedLines = new HashSet<string>(); 

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
                if (tile.isNeutralized)
                {
                    localPlacementScore += scoreNeutralizedMalware;
                }
                else
                {
                    int infectedBlocksCount = 0;
                    Vector2Int[] eightDirs = {
                        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(1, -1),
                        new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1)
                    };

                    foreach (Vector2Int dir in eightDirs)
                    {
                        if (gridManager.gridMap.ContainsKey(pos + dir))
                        {
                            infectedBlocksCount++;
                        }
                    }

                    localPlacementScore -= (infectedBlocksCount * 10); 
                }
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
        
        for (int y = gridManager.minY; y < gridManager.maxY; y++)
        {
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
                    claimedLines.Add(lineKey); 
                    permanentBonusScore += scoreFullLineBonus; 
                    StartCoroutine(PlayLineClearVFX(true, y)); 
                }
            }
        }

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

        totalScore = localPlacementScore + permanentBonusScore;

        if (MilestoneManager.Instance != null) MilestoneManager.Instance.CheckMilestone(totalScore);
        if (UIManager.Instance != null && MilestoneManager.Instance != null)
            UIManager.Instance.UpdateHUDScore(totalScore, MilestoneManager.Instance.CurrentTargetMilestone);

        Debug.Log($"[SCORE] Dasar & Kabel Lokal: {localPlacementScore} | Tetris Bonus: {permanentBonusScore} | TOTAL: {totalScore}");
    }

    public void UpdateOverheatStatus()
    {
        // 1. Kumpulkan posisi semua AC di lantai
        List<Vector2Int> acPositions = new List<Vector2Int>();
        foreach (var kvp in gridManager.gridMap)
        {
            if (kvp.Value.type == GridTileType.Cooling)
            {
                acPositions.Add(kvp.Key);
            }
        }

        // 2. Cari semua grup Server menggunakan pelacakan sederhana (BFS)
        HashSet<Vector2Int> visitedDatabases = new HashSet<Vector2Int>();
        Vector2Int[] dirs4 = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var kvp in gridManager.gridMap)
        {
            if (kvp.Value.type == GridTileType.Database && !visitedDatabases.Contains(kvp.Key))
            {
                List<Vector2Int> currentGroup = new List<Vector2Int>();
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                
                queue.Enqueue(kvp.Key);
                visitedDatabases.Add(kvp.Key);

                while (queue.Count > 0)
                {
                    Vector2Int curr = queue.Dequeue();
                    currentGroup.Add(curr);

                    foreach (Vector2Int dir in dirs4)
                    {
                        Vector2Int neighborPos = curr + dir;
                        if (gridManager.gridMap.TryGetValue(neighborPos, out TileData neighborTile))
                        {
                            if (neighborTile.type == GridTileType.Database && !visitedDatabases.Contains(neighborPos))
                            {
                                visitedDatabases.Add(neighborPos);
                                queue.Enqueue(neighborPos);
                            }
                        }
                    }
                }

                // 3. Grup ini terancam Overheat HANYA JIKA berisi 3 Server atau lebih
                bool isGroupOverheating = currentGroup.Count >= 3;

                // 4. Terapkan status suhu ke setiap Server di dalam grup ini
                foreach (Vector2Int serverPos in currentGroup)
                {
                    bool isOverheating = false;

                    if (isGroupOverheating)
                    {
                        bool isCooledByAC = false;
                        foreach (Vector2Int acPos in acPositions)
                        {
                            if (Mathf.Abs(serverPos.x - acPos.x) <= 1 && Mathf.Abs(serverPos.y - acPos.y) <= 1)
                            {
                                isCooledByAC = true;
                                break;
                            }
                        }

                        if (!isCooledByAC)
                        {
                            isOverheating = true;
                        }
                    }

                    // =========================================================
                    // 5. FIX BUG VISUAL: Cocokkan posisi 3D VFX dengan posisi Grid
                    // =========================================================
                    if (gridManager.gridMap.TryGetValue(serverPos, out TileData sData))
                    {
                        if (sData.tileObject != null)
                        {
                            // Ambil SEMUA komponen TileVFX yang ada di dalam blok prefab ini
                            TileVFX[] allVFX = sData.tileObject.GetComponentsInChildren<TileVFX>();
                            
                            foreach (TileVFX vfx in allVFX)
                            {
                                // Ubah posisi 3D anak/cube tersebut menjadi kordinat Grid
                                // (Asumsi nilai cellSize kamu = 1f)
                                int vfxGridX = Mathf.RoundToInt(vfx.transform.position.x);
                                int vfxGridY = Mathf.RoundToInt(vfx.transform.position.z);
                                
                                // Jika koordinatnya SAMA PERSIS dengan server yang sedang dicek, nyalakan!
                                if (vfxGridX == serverPos.x && vfxGridY == serverPos.y)
                                {
                                    vfx.SetOverheatStatus(isOverheating);
                                }
                            }
                        }
                    }
                    // =========================================================
                }
            }
        }
    }

    private IEnumerator PlayLineClearVFX(bool isRow, int lineIndex)
    {
        List<Transform> tilesInLine = new List<Transform>();
        Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();

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

    // ==============================================================================
    // SISTEM PREDIKSI SKOR (SIMULASI MASA DEPAN)
    // ==============================================================================
    // ==============================================================================
    // SISTEM PREDIKSI SKOR (UNTUK TEKS PREVIEW & GHOST BAR)
    // ==============================================================================
    public int GetEstimatedPlacementScore(Vector2Int baseGridPos, List<TileOccupancy> rotatedTiles)
    {
        // 1. Buat Lantai Virtual (Masa Depan) = Lantai Asli + Blok Hologram
        Dictionary<Vector2Int, GridTileType> virtualGrid = new Dictionary<Vector2Int, GridTileType>();
        foreach (var kvp in gridManager.gridMap) virtualGrid[kvp.Key] = kvp.Value.type;

        foreach (TileOccupancy tile in rotatedTiles)
        {
            Vector2Int worldPos = baseGridPos + tile.position;
            // Jika ditaruh di luar batas atau menabrak blok lain, prediksi skor = 0
            if (virtualGrid.ContainsKey(worldPos)) return 0; 
            virtualGrid[worldPos] = tile.type;
        }

        // 2. Buat Lantai Virtual (Saat Ini) = Hanya Lantai Asli
        Dictionary<Vector2Int, GridTileType> currentGrid = new Dictionary<Vector2Int, GridTileType>();
        foreach (var kvp in gridManager.gridMap) currentGrid[kvp.Key] = kvp.Value.type;

        // 3. Simulasikan dan bandingkan skornya
        int futureScore = CalculateVirtualGridScore(virtualGrid);
        int currentScore = CalculateVirtualGridScore(currentGrid);

        return futureScore - currentScore; // Selisihnya adalah prediksi bonus murni!
    }

    // Fungsi Pembantu: Simulasi Masa Depan
    private int CalculateVirtualGridScore(Dictionary<Vector2Int, GridTileType> tempGrid)
    {
        int tempScore = 0;

        // A. SIMULASI OVERHEAT & VIRUS (Kumpulkan Data)
        HashSet<Vector2Int> overheatedTiles = new HashSet<Vector2Int>();
        List<Vector2Int> acPositions = new List<Vector2Int>();
        List<Vector2Int> firewallPositions = new List<Vector2Int>();
        HashSet<Vector2Int> visitedDB = new HashSet<Vector2Int>();
        HashSet<Vector2Int> neutralizedViruses = new HashSet<Vector2Int>();
        
        Vector2Int[] dirs4 = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Vector2Int[] dirs8 = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right, new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) };

        foreach (var kvp in tempGrid)
        {
            if (kvp.Value == GridTileType.Cooling) acPositions.Add(kvp.Key);
            if (kvp.Value == GridTileType.Firewall) firewallPositions.Add(kvp.Key);
        }

        // Cari grup Server yang kepanasan
        foreach (var kvp in tempGrid)
        {
            if (kvp.Value == GridTileType.Database && !visitedDB.Contains(kvp.Key))
            {
                List<Vector2Int> group = new List<Vector2Int>();
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                queue.Enqueue(kvp.Key);
                visitedDB.Add(kvp.Key);

                while (queue.Count > 0)
                {
                    Vector2Int curr = queue.Dequeue();
                    group.Add(curr);
                    foreach (Vector2Int dir in dirs4)
                    {
                        Vector2Int neighborPos = curr + dir;
                        if (tempGrid.TryGetValue(neighborPos, out GridTileType type) && type == GridTileType.Database && !visitedDB.Contains(neighborPos))
                        {
                            visitedDB.Add(neighborPos);
                            queue.Enqueue(neighborPos);
                        }
                    }
                }

                if (group.Count >= 3)
                {
                    foreach (Vector2Int dbPos in group)
                    {
                        bool isCooled = false;
                        foreach (Vector2Int ac in acPositions)
                        {
                            if (Mathf.Abs(dbPos.x - ac.x) <= 1 && Mathf.Abs(dbPos.y - ac.y) <= 1)
                            {
                                isCooled = true; break;
                            }
                        }
                        if (!isCooled) overheatedTiles.Add(dbPos);
                    }
                }
            }
        }

        // Cari Virus yang dimatikan
        foreach (Vector2Int fPos in firewallPositions)
        {
            foreach (Vector2Int dir in dirs8)
            {
                if (tempGrid.TryGetValue(fPos + dir, out GridTileType type) && type == GridTileType.Malware)
                    neutralizedViruses.Add(fPos + dir);
            }
        }

        // B. HITUNG BASE SKOR (Posisi, Tetangga, Penalti)
        foreach (var kvp in tempGrid)
        {
            Vector2Int pos = kvp.Key;
            GridTileType type = kvp.Value;
            bool isOverheating = overheatedTiles.Contains(pos);

            if (isOverheating) tempScore -= penaltyOverheat;

            if (type == GridTileType.Database)
            {
                if (!isOverheating) tempScore += scoreDatabaseSingle;
                
                foreach (Vector2Int d in dirs4)
                {
                    if (tempGrid.TryGetValue(pos + d, out GridTileType nType) && nType == GridTileType.Database)
                        tempScore += scoreDatabaseAdjacency;
                }
            }
            else if (type == GridTileType.Cooling) tempScore += scoreACPlacement;
            else if (type == GridTileType.Firewall) tempScore += scoreFirewallPlacement;
            else if (type == GridTileType.Malware)
            {
                if (neutralizedViruses.Contains(pos)) tempScore += scoreNeutralizedMalware;
                else
                {
                    int infCount = 0;
                    foreach (Vector2Int d in dirs8) if (tempGrid.ContainsKey(pos + d)) infCount++;
                    tempScore -= (infCount * 10);
                }
            }
            else if (IsCable(type))
            {
                foreach (Vector2Int d in dirs4)
                {
                    if (tempGrid.TryGetValue(pos + d, out GridTileType nType))
                    {
                        if (nType == GridTileType.Database) tempScore += scorePerDatabaseAdjacent;
                        if (IsCable(nType)) tempScore += scorePerCableAdjacent;
                    }
                }
            }
        }

        // =========================================================
        // C. SIMULASI BONUS TETRIS (LINE CLEAR)
        // =========================================================
        int projectedFullLines = 0;

        // 1. Ramal Baris Horizontal (Row)
        for (int y = gridManager.minY; y < gridManager.maxY; y++)
        {
            string lineKey = $"Row_{y}_Bounds_{gridManager.minX}_to_{gridManager.maxX}";
            
            // Jangan hitung garis yang sudah pecah di masa lalu!
            if (!claimedLines.Contains(lineKey))
            {
                bool isRowFull = true;
                for (int x = gridManager.minX; x < gridManager.maxX; x++)
                {
                    if (!tempGrid.ContainsKey(new Vector2Int(x, y)))
                    {
                        isRowFull = false;
                        break;
                    }
                }
                if (isRowFull) projectedFullLines++;
            }
        }

        // 2. Ramal Kolom Vertikal (Col)
        for (int x = gridManager.minX; x < gridManager.maxX; x++)
        {
            string lineKey = $"Col_{x}_Bounds_{gridManager.minY}_to_{gridManager.maxY}";
            
            if (!claimedLines.Contains(lineKey))
            {
                bool isColFull = true;
                for (int y = gridManager.minY; y < gridManager.maxY; y++)
                {
                    if (!tempGrid.ContainsKey(new Vector2Int(x, y)))
                    {
                        isColFull = false;
                        break;
                    }
                }
                if (isColFull) projectedFullLines++;
            }
        }

        tempScore += (projectedFullLines * scoreFullLineBonus);

        return tempScore;
    }
}