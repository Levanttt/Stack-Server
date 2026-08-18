using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class PlacementSystem : MonoBehaviour
{
    [Header("Manager References")]
    public GridManager gridManager;
    public ScoreManager scoreManager;
    public VirusManager virusManager;
    public BlockQueueManager queueManager;

    [Header("Placement References")]
    public LayerMask floorLayer;
    public GameObject blockPrefab;
    public float cellSize = 1f;

    [Header("Audio Settings")]
    public SoundFX placeBlockSFX; 

    [Header("Preview Settings (Hologram)")]
    public Material validMaterial;
    public Material invalidMaterial;
    public TextMeshPro staticPreviewText; 
    
    [Header("Preview Text Colors")]
    public Color textPositiveColor = Color.green;
    public Color textNegativeColor = Color.red;
    public Color textNeutralColor = Color.gray;
    
    [Header("Dynamic Offset Settings")]
    public Vector3 defaultTextOffset = new Vector3(0f, 2f, 0f); 
    public Vector3 doubleBlockTextOffset = new Vector3(-0.5f, 2f, 0f);

    private Vector3 currentDynamicTextOffset; 

    private GameObject previewObject;
    private GameObject lastBlockPrefab;

    public Dictionary<Vector2Int, BlockData> gridData = new Dictionary<Vector2Int, BlockData>();
    private float currentRotation = 0f;

    private Vector2Int lastHoveredPos = new Vector2Int(-999, -999);
    private float lastRotation = -1f;

    private bool isWaitingForScore = false;

    private void Update()
    {
        if (isWaitingForScore) return;

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

            if (canPlace)
            {
                if (baseGridPos != lastHoveredPos || currentRotation != lastRotation)
                {
                    lastHoveredPos = baseGridPos;
                    lastRotation = currentRotation;

                    int estimatedScore = scoreManager.GetEstimatedPlacementScore(baseGridPos, rotatedTiles);

                    if (staticPreviewText != null)
                    {
                        staticPreviewText.gameObject.SetActive(true);
                        staticPreviewText.transform.position = previewObject.transform.position + currentDynamicTextOffset;
                        
                        if (Camera.main != null) staticPreviewText.transform.rotation = Camera.main.transform.rotation;
                        
                        if (estimatedScore > 0) 
                        { 
                            staticPreviewText.text = $"+{estimatedScore}"; 
                            if (scoreManager != null && estimatedScore >= scoreManager.scoreFullLineBonus)
                                staticPreviewText.color = new Color(1f, 0.84f, 0f, 1f);
                            else
                                staticPreviewText.color = textPositiveColor; 
                        }
                        else if (estimatedScore < 0) { staticPreviewText.text = $"{estimatedScore}"; staticPreviewText.color = textNegativeColor; }
                        else { staticPreviewText.text = "0"; staticPreviewText.color = textNeutralColor; }
                    }

                    if (UIManager.Instance != null && MilestoneManager.Instance != null)
                    {
                        UIManager.Instance.ShowScorePreview(
                            scoreManager.totalScore, 
                            estimatedScore, 
                            MilestoneManager.Instance.CurrentTargetMilestone
                        );
                    }
                }
            }
            else
            {
                if (staticPreviewText != null) staticPreviewText.gameObject.SetActive(false);
                lastHoveredPos = new Vector2Int(-999, -999);
                
                if (UIManager.Instance != null) UIManager.Instance.HideScorePreview();
            }

            if (canPlace && Input.GetMouseButtonDown(0))
            {
                isWaitingForScore = true; 

                if (AudioManager.Instance != null && placeBlockSFX != null)
                {
                    AudioManager.Instance.PlaySFX(placeBlockSFX);
                }
                
                int finalScoreGained = scoreManager.GetEstimatedPlacementScore(baseGridPos, rotatedTiles);
                Vector3 popUpPos = previewObject.transform.position + currentDynamicTextOffset;
                
                if (previewObject != null) previewObject.SetActive(false);
                if (staticPreviewText != null) staticPreviewText.gameObject.SetActive(false);

                PlaceBlock(baseGridPos, rotatedTiles);

                if (FloatingTextManager.Instance != null && UIManager.Instance != null && UIManager.Instance.scoreValueText != null) 
                {
                    FloatingTextManager.Instance.SpawnFinalScore(
                        popUpPos, 
                        finalScoreGained, 
                        UIManager.Instance.scoreValueText.rectTransform,
                        () => 
                        {
                            UIManager.Instance.HideScorePreview(); 
                            UIManager.Instance.UpdateHUDScore(scoreManager.totalScore, MilestoneManager.Instance.CurrentTargetMilestone);
                            
                            isWaitingForScore = false; 
                        }
                    );
                }
                else
                {
                    UIManager.Instance.HideScorePreview();
                    isWaitingForScore = false;
                }

                lastHoveredPos = new Vector2Int(-999, -999); 
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

        BlockData dataForOffset = blockPrefab.GetComponent<BlockData>();
        if (dataForOffset != null)
        {
            int blockSize = dataForOffset.localTiles.Count;
            if (blockSize == 2)
            {
                currentDynamicTextOffset = doubleBlockTextOffset;
            }
            else
            {
                currentDynamicTextOffset = defaultTextOffset;
            }
        }
        else
        {
            currentDynamicTextOffset = defaultTextOffset; 
        }

        currentRotation = 0f;
        previewObject = Instantiate(blockPrefab);
        previewObject.name = "BlockPreview_Hologram";
        previewObject.transform.rotation = Quaternion.Euler(0, currentRotation, 0);

        Destroy(previewObject.GetComponent<BlockData>());
        Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders) Destroy(col);

        TileVFX[] vfxComponents = previewObject.GetComponentsInChildren<TileVFX>();
        foreach (TileVFX vfx in vfxComponents)
        {
            if (vfx.warningIcon != null)
            {
                Destroy(vfx.warningIcon);
            }
            Destroy(vfx);
        }
        
        lastHoveredPos = new Vector2Int(-999, -999); 
    }

    private void SetPreviewColor(bool isValid)
    {
        if (previewObject == null) return;
        Material targetMat = isValid ? validMaterial : invalidMaterial;
        
        MeshRenderer[] renderers = previewObject.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer r in renderers) 
        {
            Material[] holoMaterials = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < holoMaterials.Length; i++)
            {
                holoMaterials[i] = targetMat;
            }
            r.materials = holoMaterials;
        }
    }

    private void HidePreview()
    {
        if (previewObject != null) previewObject.SetActive(false);
        if (staticPreviewText != null) staticPreviewText.gameObject.SetActive(false);
        if (UIManager.Instance != null) UIManager.Instance.HideScorePreview();
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

        if (virusManager != null) 
        {
            virusManager.NeutralizeVirus();
            virusManager.UpdateInfectionVisuals();
        }
        
        if (scoreManager != null) 
        {
            scoreManager.UpdateOverheatStatus(); 
            scoreManager.CalculateScore();       
        }
        
        if (queueManager != null) 
        {
            queueManager.OnBlockPlacedSuccessfully();

            List<GameObject> currentCards = queueManager.GetCurrentAvailableBlocks();
            bool isGameOver = CheckForGameOver(currentCards);

            if (isGameOver)
            {
                ScoreManager.Instance.CheckAndSaveHighScore();
                if (currentCards.Count == 0)
                {
                    if (UIManager.Instance != null) UIManager.Instance.ShowGameOverPanel(scoreManager.totalScore, "OUT OF BLOCKS");
                }
                else
                {
                    if (UIManager.Instance != null) UIManager.Instance.ShowGameOverPanel(scoreManager.totalScore, "SYSTEM OVERLOADED");
                }

                if (GameStateManager.Instance != null) GameStateManager.Instance.ChangeState(GameState.GameOver);
            }
        }
    }

    public bool CheckForGameOver(List<GameObject> availableBlockPrefabs)
    {
        if (gridManager == null || availableBlockPrefabs.Count == 0) return true;

        List<Vector2Int> emptyTiles = new List<Vector2Int>();
        foreach (Vector2Int floorPos in gridManager.floorGrid.Keys)
        {
            if (!gridData.ContainsKey(floorPos) && !gridManager.gridMap.ContainsKey(floorPos)) 
                emptyTiles.Add(floorPos);
        }

        foreach (GameObject prefab in availableBlockPrefabs)
        {
            if (prefab == null) continue;
            BlockData blockData = prefab.GetComponent<BlockData>();
            
            if (blockData == null) 
            {
                if (emptyTiles.Count > 0) return false; 
                continue;
            }

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
                        if (gridData.ContainsKey(checkPos) || (gridManager != null && gridManager.gridMap.ContainsKey(checkPos)) || !gridManager.floorGrid.ContainsKey(checkPos))
                        {
                            canPlaceThis = false;
                            break;
                        }
                    }
                    if (canPlaceThis) return false;
                }
            }
        }
        return true;
    }
}