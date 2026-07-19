using System.Collections.Generic;
using UnityEngine;

// KTP untuk tiap tile/kotak
public enum GridTileType { Kosong, Database, Cooling, Kabel_I, Kabel_L, Kabel_T, Kabel_Cross, Malware, Firewall }

[System.Serializable]
public class TileData
{
    public GridTileType type;
    public GameObject tileObject;
    public bool isNeutralized;

    public TileData(GridTileType _type, GameObject _obj)
    {
        type = _type;
        tileObject = _obj;
        isNeutralized = false;
    }
}

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; } 

    [Header("Grid Settings")]
    public int width = 10;
    public int height = 10;
    public float cellSize = 1f; 

    [Header("Prefabs")]
    public GameObject floorTilePrefab; 
    
    // PETA 1: Khusus menyimpan ubin lantai/grid dasar
    public Dictionary<Vector2Int, GameObject> floorGrid = new Dictionary<Vector2Int, GameObject>();

    // PETA 2 (RADAR): Khusus menyimpan balok poliomino yang ditaruh pemain
    public Dictionary<Vector2Int, TileData> gridMap = new Dictionary<Vector2Int, TileData>();

    // Kompas 4-Arah
    private readonly Vector2Int[] orthogonalDirs = new Vector2Int[]
    {
        Vector2Int.up,    
        Vector2Int.down,  
        Vector2Int.left,  
        Vector2Int.right  
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        GenerateGrid();
    }

    // Fungsi LAMA: Membuat lantai
    private void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Vector3 worldPosition = new Vector3(x * cellSize, 0, y * cellSize);

                GameObject spawnedTile = Instantiate(floorTilePrefab, worldPosition, Quaternion.identity);
                spawnedTile.name = $"FloorTile ({x}, {y})";
                spawnedTile.transform.SetParent(transform); 
                floorGrid.Add(pos, spawnedTile);
            }
        }
    }

    // ==========================================
    // FUNGSI RADAR BARU
    // ==========================================

    public void AddTileToGrid(Vector2Int pos, GridTileType type, GameObject obj)
    {
        if (!gridMap.ContainsKey(pos))
        {
            gridMap.Add(pos, new TileData(type, obj));
        }
    }

    public List<TileData> GetOrthogonalNeighbors(Vector2Int pos)
    {
        List<TileData> neighbors = new List<TileData>();

        foreach (Vector2Int dir in orthogonalDirs)
        {
            Vector2Int neighborPos = pos + dir;
            
            if (gridMap.TryGetValue(neighborPos, out TileData neighborTile))
            {
                neighbors.Add(neighborTile);
            }
        }

        return neighbors;
    }
}