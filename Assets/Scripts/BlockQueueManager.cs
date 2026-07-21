using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

[System.Serializable]
public class BlockUnlockTier
{
    public int unlockAtMilestoneLevel; 
    public List<GameObject> blockPrefabs;
}

public class BlockQueueManager : MonoBehaviour
{
    public static BlockQueueManager Instance { get; private set; }

    [Header("Progression Tiers (Katalog)")]
    public List<BlockUnlockTier> unlockTiers = new List<BlockUnlockTier>();
    private List<GameObject> currentlyUnlockedBlocks = new List<GameObject>();

    [Header("Deck Settings")]
    public int initialDeckCapacity = 6; 
    private Queue<GameObject> currentDeck = new Queue<GameObject>();

    [Header("Dynamic Scaling Stock")]
    public int baseRewardStock = 4; 
    public int rewardIncrementPerLevel = 1; 

    [Header("Hand Settings (3 Slot)")]
    public GameObject[] activeHand = new GameObject[3];
    public int currentSelectedSlot = -1;

    [Header("UI References")]
    public Image[] slotImages = new Image[3]; 
    public TextMeshProUGUI totalStockText; 

    [Header("References")]
    public PlacementSystem placementSystem;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeDeck();
    }

    public void InitializeDeck()
    {
        currentDeck.Clear();
        currentlyUnlockedBlocks.Clear();

        UnlockBlocksByMilestone(0);

        for (int i = 0; i < initialDeckCapacity; i++)
        {
            AddRandomUnlockedBlockToQueue();
        }
        
        RefillHand();
        UpdateStockUI();
    }

    public void OnMilestoneReached(int currentMilestoneLevel)
    {
        UnlockBlocksByMilestone(currentMilestoneLevel);

        int blocksToAdd = baseRewardStock + (currentMilestoneLevel * rewardIncrementPerLevel);

        for (int i = 0; i < blocksToAdd; i++)
        {
            AddRandomUnlockedBlockToQueue();
        }
        
        RefillHand(); 
        UpdateStockUI();
    }

    private void UnlockBlocksByMilestone(int level)
    {
        foreach (var tier in unlockTiers)
        {
            if (tier.unlockAtMilestoneLevel == level)
            {
                currentlyUnlockedBlocks.AddRange(tier.blockPrefabs);
            }
        }
    }

    private void AddRandomUnlockedBlockToQueue()
    {
        if (currentlyUnlockedBlocks.Count == 0) return;
        
        int randomIndex = Random.Range(0, currentlyUnlockedBlocks.Count);
        currentDeck.Enqueue(currentlyUnlockedBlocks[randomIndex]);
    }

    public void RefillHand()
    {
        ShiftHandLeft();

        for (int i = 0; i < activeHand.Length; i++)
        {
            if (activeHand[i] == null && currentDeck.Count > 0)
            {
                activeHand[i] = currentDeck.Dequeue(); 
            }
            
            UpdateSlotUI(i);
        }
    }

    private void ShiftHandLeft()
    {
        int targetIndex = 0;
        for (int i = 0; i < activeHand.Length; i++)
        {
            if (activeHand[i] != null)
            {
                if (i != targetIndex)
                {
                    activeHand[targetIndex] = activeHand[i]; 
                    activeHand[i] = null; 
                }
                targetIndex++;
            }
        }
    }

    private void UpdateSlotUI(int slotIndex)
    {
        if (slotImages[slotIndex] != null)
        {
            if (activeHand[slotIndex] != null)
            {
                BlockData data = activeHand[slotIndex].GetComponent<BlockData>();
                if (data != null && data.blockIcon != null)
                {
                    slotImages[slotIndex].sprite = data.blockIcon;
                    slotImages[slotIndex].color = Color.white; 
                }
            }
            else
            {
                slotImages[slotIndex].sprite = null;
                slotImages[slotIndex].color = new Color(1, 1, 1, 0f); 
            }
        }
    }

    private void UpdateStockUI()
    {
        if (totalStockText != null)
        {
            totalStockText.text = currentDeck.Count.ToString();
            totalStockText.color = currentDeck.Count == 0 ? Color.red : Color.white;
        }
    }

    public void SelectBlock(int slotIndex)
    {
        if (activeHand[slotIndex] != null)
        {
            currentSelectedSlot = slotIndex;
            placementSystem.blockPrefab = activeHand[slotIndex];
        }
    }

    public void OnBlockPlacedSuccessfully()
    {
        if (currentSelectedSlot != -1)
        {
            activeHand[currentSelectedSlot] = null;
            currentSelectedSlot = -1;
            placementSystem.blockPrefab = null;
            
            RefillHand(); 
            UpdateStockUI(); 
            CheckGameOverCondition();
        }
    }

    private void CheckGameOverCondition()
    {
        int blocksInHand = 0;
        for (int i = 0; i < activeHand.Length; i++)
        {
            if (activeHand[i] != null) blocksInHand++;
        }

        if (currentDeck.Count == 0 && blocksInHand == 0)
        {
            Debug.Log("GAME OVER! Stok blok habis.");
            if (UIManager.Instance != null)
            {
                int finalScore = ScoreManager.Instance.totalScore; 
                UIManager.Instance.ShowGameOverPanel(finalScore);
            }
        }
    }

    public List<GameObject> GetCurrentAvailableBlocks()
    {
        List<GameObject> currentBlocks = new List<GameObject>();
        for (int i = 0; i < activeHand.Length; i++)
        {
            if (activeHand[i] != null)
            {
                currentBlocks.Add(activeHand[i]);
            }
        }
        return currentBlocks;
    }
}