using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class PlacementSystem : MonoBehaviour
{
    [Header("References")]
    public GameObject indicatorPrefab; 
    public LayerMask floorLayer;

    [Header("Placement Settings")]
    public GameObject[] blockRoster;
    private GameObject blockPrefab;
    public float cellSize = 1f;

    [Header("Global Stock System")]
    public int currentGlobalStock = 15; // Stok total global
    public Sprite[] blockSprites;      // Ikon untuk preview blok
    
    [Header("UI References")]
    public Image nextBlockImage;       // Panel gambar di pojok kiri atas
    public TextMeshProUGUI totalStockText;
    public GameObject[] uiCards; // Teks sisa stok di pojok kiri atas

    public Dictionary<Vector2Int, BlockData> gridData = new Dictionary<Vector2Int, BlockData>();
    
    private float currentRotation = 0f;
    private int currentBlockIndex = 0; 
    private List<GameObject> activeIndicators = new List<GameObject>();
    private Transform indicatorContainer;

    private void Start()
    {
        indicatorContainer = new GameObject("IndicatorContainer").transform;
        if (blockRoster.Length > 0) 
        {
            SelectBlock(0);
        }
        UpdatePreviewUI();
    }

    private void Update()
    {
        HandleBlockSelection();
        HandleRotation();
        DetectAndPlace();
    }

    private void HandleBlockSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectBlock(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectBlock(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectBlock(2);
    }

    public void SelectBlock(int rosterIndex)
    {
        if (rosterIndex >= 0 && rosterIndex < blockRoster.Length)
        {
            currentBlockIndex = rosterIndex;
            blockPrefab = blockRoster[rosterIndex];
            UpdatePreviewUI();
        }
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
        if (EventSystem.current.IsPointerOverGameObject())
        {
            ClearIndicators();
            return;
        }

        // Cek stok global
        if (currentGlobalStock <= 0)
        {
            ClearIndicators();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, floorLayer))
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

                if (gridData.ContainsKey(worldGridPos) || 
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

    private void PlaceBlock(Vector2Int baseGridPos, List<Vector2Int> rotatedTiles)
    {
        Vector3 spawnPos = new Vector3(baseGridPos.x * cellSize, 0.5f, baseGridPos.y * cellSize);
        GameObject newBlock = Instantiate(blockPrefab, spawnPos, Quaternion.Euler(0, currentRotation, 0));
        
        BlockData data = newBlock.GetComponent<BlockData>();

        foreach (Vector2Int localPos in rotatedTiles)
        {
            Vector2Int worldPos = baseGridPos + localPos;
            gridData.Add(worldPos, data);
        }

        currentGlobalStock--;
        UpdatePreviewUI();
    }

    private void UpdatePreviewUI()
    {
        if (totalStockText != null)
        {
            int displayStock = currentGlobalStock - 3;
            if (displayStock < 0) displayStock = 0;

            totalStockText.text = displayStock.ToString();
            totalStockText.color = displayStock <= 0 ? Color.red : Color.white;
        }

        if (currentBlockIndex < blockSprites.Length && nextBlockImage != null)
        {
            nextBlockImage.sprite = blockSprites[currentBlockIndex];
        }

        if (uiCards != null)
        {
            for (int i = 0; i < uiCards.Length; i++)
            {
                if (uiCards[i] != null)
                {
                    uiCards[i].SetActive(currentGlobalStock >= (i + 1));
                }
            }
        }

        if (currentGlobalStock > 0 && currentGlobalStock < (currentBlockIndex + 1))
        {
            SelectBlock(currentGlobalStock - 1);
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
        }
    }

    private void ClearIndicators()
    {
        foreach (GameObject indicator in activeIndicators)
        {
            indicator.SetActive(false);
        }
    }
}