using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

    public Dictionary<Vector2Int, BlockData> gridData = new Dictionary<Vector2Int, BlockData>();
    private float currentRotation = 0f;

    private void Update()
    {
        HandleBlockSelection();
        HandleRotation();
        DetectAndPlace();
    }

    private void HandleBlockSelection()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.currentState != GameState.Playing)
        {
            HidePreview();
            return;
        }

        if (queueManager == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) queueManager.SelectBlock(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) queueManager.SelectBlock(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) queueManager.SelectBlock(2);
    }

    private void HandleRotation()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.currentState != GameState.Playing)
        {
            HidePreview();
            return;
        }

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
        if (GameStateManager.Instance != null && GameStateManager.Instance.currentState != GameState.Playing)
        {
            HidePreview();
            return;
        }

        // Hapus pengecekan currentGlobalStock di sini, cukup cek blockPrefab == null
        if (EventSystem.current.IsPointerOverGameObject() || blockPrefab == null)
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

                if (gridData.ContainsKey(worldGridPos) || !gridManager.floorGrid.ContainsKey(worldGridPos))
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

        currentRotation = 0f;

        previewObject = Instantiate(blockPrefab);
        previewObject.name = "BlockPreview_Hologram";
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
        Vector3 spawnPos = new Vector3(baseGridPos.x * cellSize, 0.05f, baseGridPos.y * cellSize);
        GameObject newBlock = Instantiate(blockPrefab, spawnPos, Quaternion.Euler(0, currentRotation, 0));

        newBlock.AddComponent<BlockDropAnimator>();

        BlockData data = newBlock.GetComponent<BlockData>();

        foreach (TileOccupancy tile in rotatedTiles)
        {
            Vector2Int worldPos = baseGridPos + tile.position;
            if (!gridData.ContainsKey(worldPos)) gridData.Add(worldPos, data);
            if (gridManager != null) gridManager.AddTileToGrid(worldPos, tile.type, newBlock);
        }

        if (clusterManager != null) clusterManager.CalculateClusters();
        if (virusManager != null) virusManager.NeutralizeVirus();

        if (scoreManager != null) scoreManager.CalculateScore();

        if (queueManager != null) queueManager.OnBlockPlacedSuccessfully();

        if (queueManager != null)
        {
            List<GameObject> currentCards = queueManager.GetCurrentAvailableBlocks();
            bool isGameOver = CheckForGameOver(currentCards);

            if (isGameOver)
            {
                if (UIManager.Instance != null) UIManager.Instance.ShowGameOverPanel(scoreManager.totalScore);
                if (GameStateManager.Instance != null) GameStateManager.Instance.ChangeState(GameState.GameOver);
            }
        }
    }

    private bool CheckForGameOver(List<GameObject> availableBlockPrefabs)
    {
        if (gridManager == null || availableBlockPrefabs.Count == 0) return true;

        List<Vector2Int> emptyTiles = new List<Vector2Int>();
        foreach (Vector2Int floorPos in gridManager.floorGrid.Keys)
        {
            if (!gridData.ContainsKey(floorPos)) emptyTiles.Add(floorPos);
        }

        foreach (GameObject prefab in availableBlockPrefabs)
        {
            if (prefab == null) continue;
            BlockData blockData = prefab.GetComponent<BlockData>();
            if (blockData == null) continue;

            float[] rotations = { 0f, 90f, 180f, 270f };
            foreach (float rot in rotations)
            {
                List<TileOccupancy> rotatedTiles = blockData.GetRotatedTiles(rot);
                foreach (Vector2Int basePos in emptyTiles)
                {
                    bool canPlaceThis = true;
                    foreach (TileOccupancy tile in rotatedTiles)
                    {
                        Vector2Int checkPos = basePos + tile.position;
                        if (gridData.ContainsKey(checkPos) || !gridManager.floorGrid.ContainsKey(checkPos))
                        {
                            canPlaceThis = false;
                            break;
                        }
                    }
                    if (canPlaceThis) return false;
                }
            }
        }
        return true; // Kalau semua blok di tangan gak muat, Game Over
    }
}