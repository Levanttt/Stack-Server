using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; } 

    [Header("Grid Settings")]
    public int width = 10;
    public int height = 10;
    public float cellSize = 1f; 

    [Header("Prefabs")]
    public GameObject floorTilePrefab; 
    public Dictionary<Vector2Int, GameObject> gridData = new Dictionary<Vector2Int, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        GenerateGrid();
    }

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
                gridData.Add(pos, spawnedTile);
            }
        }
    }
}