using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class PlacementSystem : MonoBehaviour
{
    [Header("Manager References")]
    public GridManager gridManager; 
    public ClusterManager clusterManager;
    public ScoreManager scoreManager;
    public VirusManager virusManager;
    public BlockQueueManager queueManager;

    [Header("Placement References")]
    public LayerMask floorLayer;
    public GameObject blockPrefab; 
    public float cellSize = 1f;

    [Header("Preview Settings (Hologram)")]
    public Material validMaterial; 
    public Material invalidMaterial; 
    
    private GameObject previewObject;
    private GameObject lastBlockPrefab;

    [Header("Global Stock System")]
    public int currentGlobalStock = 15; 
    public TextMeshProUGUI totalStockText;

    public Dictionary<Vector2Int, BlockData> gridData = new Dictionary<Vector2Int, BlockData>();
    
    private float currentRotation = 0f;

    private void Start()
    {
        UpdatePreviewUI();
    }

    private void Update()
    {
        HandleBlockSelection(); // BUG FIX: Mengembalikan fungsi tombol 1, 2, 3
        HandleRotation();
        DetectAndPlace();
    }

    // BUG FIX: Fungsi shortcut keyboard diaktifkan kembali
    private void HandleBlockSelection()
    {
        if (queueManager == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) queueManager.SelectBlock(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) queueManager.SelectBlock(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) queueManager.SelectBlock(2);
    }

    private void HandleRotation()
    {
        // FITUR BARU: Menggunakan tombol Spasi
        if (blockPrefab != null && Input.GetKeyDown(KeyCode.Space))
        {
            currentRotation += 90f;
            
            if (previewObject != null)
            {
                previewObject.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
            }
        }
    }

    private void DetectAndPlace()
    {
        if (EventSystem.current.IsPointerOverGameObject() || blockPrefab == null || currentGlobalStock <= 0)
        {
            HidePreview();
            return;
        }

        if (blockPrefab != lastBlockPrefab)
        {
            CreatePreviewObject();
            lastBlockPrefab = blockPrefab;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, floorLayer))
        {
            int gridX = Mathf.RoundToInt(hit.transform.position.x / cellSize);
            int gridY = Mathf.RoundToInt(hit.transform.position.z / cellSize);
            Vector2Int baseGridPos = new Vector2Int(gridX, gridY);

            BlockData blockData = blockPrefab.GetComponent<BlockData>();
            if (blockData == null) return;

            List<TileOccupancy> rotatedTiles = blockData.GetRotatedTiles(currentRotation);
            bool canPlace = true;

            foreach (TileOccupancy tile in rotatedTiles)
            {
                Vector2Int worldGridPos = baseGridPos + tile.position;

                if (gridData.ContainsKey(worldGridPos) || 
                    worldGridPos.x < 0 || worldGridPos.x >= 10 || 
                    worldGridPos.y < 0 || worldGridPos.y >= 10)
                {
                    canPlace = false;
                    break;
                }
            }

            if (previewObject != null)
            {
                previewObject.SetActive(true);
                previewObject.transform.position = new Vector3(baseGridPos.x * cellSize, 0.1f, baseGridPos.y * cellSize);
                SetPreviewColor(canPlace);
            }

            if (canPlace && Input.GetMouseButtonDown(0))
            {
                PlaceBlock(baseGridPos, rotatedTiles);
                HidePreview(); 
            }
        }
        else
        {
            HidePreview();
        }
    }

    private void CreatePreviewObject()
    {
        if (previewObject != null) Destroy(previewObject);
        if (blockPrefab == null) return;

        // BUG FIX: Reset rotasi setiap kali memegang blok baru agar logika dan visual sinkron
        currentRotation = 0f; 

        previewObject = Instantiate(blockPrefab);
        previewObject.name = "BlockPreview_Hologram";
        
        // Terapkan rotasi awal ke hologram
        previewObject.transform.rotation = Quaternion.Euler(0, currentRotation, 0);

        Destroy(previewObject.GetComponent<BlockData>());
        Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            Destroy(col);
        }
    }

    private void SetPreviewColor(bool isValid)
    {
        if (previewObject == null) return;

        Material targetMat = isValid ? validMaterial : invalidMaterial;
        MeshRenderer[] renderers = previewObject.GetComponentsInChildren<MeshRenderer>();
        
        foreach (MeshRenderer r in renderers)
        {
            r.material = targetMat;
        }
    }

    private void HidePreview()
    {
        if (previewObject != null)
        {
            previewObject.SetActive(false);
        }
    }

    private void PlaceBlock(Vector2Int baseGridPos, List<TileOccupancy> rotatedTiles)
    {
        Vector3 spawnPos = new Vector3(baseGridPos.x * cellSize, 0.1f, baseGridPos.y * cellSize);
        GameObject newBlock = Instantiate(blockPrefab, spawnPos, Quaternion.Euler(0, currentRotation, 0));
        
        BlockData data = newBlock.GetComponent<BlockData>();

        foreach (TileOccupancy tile in rotatedTiles)
        {
            Vector2Int worldPos = baseGridPos + tile.position;
            
            if (!gridData.ContainsKey(worldPos)) gridData.Add(worldPos, data);
            if (gridManager != null) gridManager.AddTileToGrid(worldPos, tile.type, newBlock);
        }

        if (queueManager != null) queueManager.OnBlockPlacedSuccessfully();

        currentGlobalStock--;
        UpdatePreviewUI();

        if (clusterManager != null) clusterManager.CalculateClusters();
        if (scoreManager != null) scoreManager.CalculateScore();
        if (virusManager != null) virusManager.NeutralizeVirus();
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
    }
}