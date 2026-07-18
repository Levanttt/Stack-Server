using System.Collections.Generic;
using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Masukkan Prefab HighlightIndicator ke sini")]
    public GameObject indicatorPrefab; 
    public LayerMask floorLayer;

    [Header("Placement Settings")]
    public GameObject blockPrefab;
    public float cellSize = 1f;

    private HashSet<Vector2Int> occupiedTiles = new HashSet<Vector2Int>();
    private float currentRotation = 0f;

    // Menyimpan daftar kotak indikator yang sedang aktif
    private List<GameObject> activeIndicators = new List<GameObject>();
    private Transform indicatorContainer;

    private void Start()
    {
        // Membuat wadah kosong di Hierarchy agar indikator tidak berantakan
        indicatorContainer = new GameObject("IndicatorContainer").transform;
    }

    private void Update()
    {
        HandleRotation();
        DetectAndPlace();
    }

    private void HandleRotation()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotation += 90f;
        }
    }

    private void DetectAndPlace()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, floorLayer))
        {
            int gridX = Mathf.RoundToInt(hit.transform.position.x / cellSize);
            int gridY = Mathf.RoundToInt(hit.transform.position.z / cellSize);
            Vector2Int baseGridPos = new Vector2Int(gridX, gridY);

            BlockData blockData = blockPrefab.GetComponent<BlockData>();
            if (blockData == null) return;

            List<Vector2Int> rotatedTiles = blockData.GetRotatedTiles(currentRotation);
            bool canPlace = true;

            foreach (Vector2Int localPos in rotatedTiles)
            {
                Vector2Int worldGridPos = baseGridPos + localPos;

                if (occupiedTiles.Contains(worldGridPos) || 
                    worldGridPos.x < 0 || worldGridPos.x >= 10 || 
                    worldGridPos.y < 0 || worldGridPos.y >= 10)
                {
                    canPlace = false;
                    break;
                }
            }

            if (canPlace)
            {
                UpdateIndicators(baseGridPos, rotatedTiles);

                if (Input.GetMouseButtonDown(0))
                {
                    PlaceBlock(baseGridPos, rotatedTiles);
                }
            }
            else
            {
                ClearIndicators();
            }
        }
        else
        {
            ClearIndicators();
        }
    }

    private void UpdateIndicators(Vector2Int baseGridPos, List<Vector2Int> rotatedTiles)
    {
        while (activeIndicators.Count < rotatedTiles.Count)
        {
            GameObject newIndicator = Instantiate(indicatorPrefab, indicatorContainer);
            activeIndicators.Add(newIndicator);
        }

        for (int i = 0; i < activeIndicators.Count; i++)
        {
            activeIndicators[i].SetActive(i < rotatedTiles.Count);
        }

        for (int i = 0; i < rotatedTiles.Count; i++)
        {
            Vector2Int tilePos = baseGridPos + rotatedTiles[i];
            Vector3 worldPos = new Vector3(tilePos.x * cellSize, 0.2f, tilePos.y * cellSize);
            
            activeIndicators[i].transform.position = worldPos;
            activeIndicators[i].transform.rotation = Quaternion.identity; 
        }
    }

    private void ClearIndicators()
    {
        foreach (GameObject indicator in activeIndicators)
        {
            indicator.SetActive(false);
        }
    }

    private void PlaceBlock(Vector2Int baseGridPos, List<Vector2Int> rotatedTiles)
    {
        Vector3 spawnPos = new Vector3(baseGridPos.x * cellSize, 0.5f, baseGridPos.y * cellSize);
        Instantiate(blockPrefab, spawnPos, Quaternion.Euler(0, currentRotation, 0));

        foreach (Vector2Int localPos in rotatedTiles)
        {
            occupiedTiles.Add(baseGridPos + localPos);
        }
    }
}