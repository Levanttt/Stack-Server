using System.Collections;
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
    private List<GameObject> currentBag = new List<GameObject>();

    [Header("Dynamic Scaling Stock")]
    public int baseRewardStock = 4; 
    public int rewardIncrementPerLevel = 1; 

    [Header("Hand Settings (3 Slot)")]
    public GameObject[] activeHand = new GameObject[3];
    public int currentSelectedSlot = -1;

    [Header("UI References - JANGAN SAMPAI TERTUKAR!")]
    public Image[] slotImages = new Image[3]; 
    public RectTransform[] cardFrames = new RectTransform[3]; 
    public TextMeshProUGUI totalStockText; 

    [Header("References")]
    public PlacementSystem placementSystem;

    [Header("Fly-In Animation (Sistem Animasi Baru)")]
    [Tooltip("Titik awal munculnya kartu baru. Letakkan Empty UI Object di dekat Stock Counter Panel.")]
    public RectTransform deckSpawnPoint; 
    
    public Image flyingIconPrefab;
    public RectTransform animationLayer;
    public float cardAppearDuration = 0.3f;
    [Range(0.05f, 1f)]
    public float cardPopStartScale = 0.3f;

    private bool isAnimating = false;
    private Vector2[] defaultSlotPositions = new Vector2[3]; 

    private struct CardMove
    {
        public int toSlot;
        public Sprite sprite;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            if (cardFrames[i] != null) defaultSlotPositions[i] = cardFrames[i].anchoredPosition;
        }
        
        InitializeDeck();
    }

    private void Update()
    {
        if (isAnimating) return; 

        bool needsEmergencyRefill = false;
        for (int i = 0; i < activeHand.Length; i++)
        {
            if (activeHand[i] == null && currentDeck.Count > 0)
            {
                needsEmergencyRefill = true;
                break;
            }
        }

        if (needsEmergencyRefill) RefillHand();
    }

    public void InitializeDeck()
    {
        currentDeck.Clear();
        currentlyUnlockedBlocks.Clear();

        UnlockBlocksByMilestone(0);
        for (int i = 0; i < initialDeckCapacity; i++) AddRandomUnlockedBlockToQueue();
        
        RefillHand();
        UpdateStockUI();
    }

    public void OnMilestoneReached(int currentMilestoneLevel)
    {
        StartCoroutine(HandleMilestoneRestock(currentMilestoneLevel));
    }

    private IEnumerator HandleMilestoneRestock(int level)
    {
        isAnimating = true;

        UnlockBlocksByMilestone(level);
        currentBag.Clear();
        int blocksToAdd = baseRewardStock + (level * rewardIncrementPerLevel);
        for (int i = 0; i < blocksToAdd; i++) AddRandomUnlockedBlockToQueue();
        ShuffleDeck();

        GameObject[] oldHand = new GameObject[3];
        for (int i = 0; i < 3; i++) oldHand[i] = activeHand[i];

        RefillHand();

        List<Coroutine> flights = new List<Coroutine>();
        
        for (int newSlot = 0; newSlot < 3; newSlot++)
        {
            if (activeHand[newSlot] == null) continue;

            if (slotImages[newSlot] != null) slotImages[newSlot].color = new Color(1f, 1f, 1f, 0f);
            
            Sprite sprite = activeHand[newSlot].GetComponent<BlockData>().blockIcon;
            
            int oldSlot = -1;
            for (int i = 0; i < 3; i++) 
            {
                if (oldHand[i] == activeHand[newSlot]) 
                {
                    oldSlot = i;
                    break;
                }
            }

            if (oldSlot != -1)
            {
                if (oldSlot != newSlot) 
                {
                    flights.Add(StartCoroutine(PlayCardAppear(sprite, cardFrames[oldSlot].position, cardFrames[newSlot].position, newSlot)));
                } 
                else 
                {
                    if (slotImages[newSlot] != null) slotImages[newSlot].color = Color.white;
                }
            }
            else
            {
                Vector3 startPos = deckSpawnPoint != null ? deckSpawnPoint.position : cardFrames[0].position;
                flights.Add(StartCoroutine(PlayCardAppear(sprite, startPos, cardFrames[newSlot].position, newSlot)));
            }
        }

        foreach (Coroutine flight in flights) yield return flight;

        ForceRefreshUI(); 
        isAnimating = false;
    }

    private void UnlockBlocksByMilestone(int level)
    {
        foreach (var tier in unlockTiers)
        {
            if (tier.unlockAtMilestoneLevel == level)
            {
                foreach(var block in tier.blockPrefabs)
                {
                    if (block != null) currentlyUnlockedBlocks.Add(block);
                }
            }
        }
    }

    private void AddRandomUnlockedBlockToQueue()
    {
        if (currentlyUnlockedBlocks.Count == 0) return;
        
        if (currentBag.Count == 0)
        {
            currentBag = new List<GameObject>(currentlyUnlockedBlocks);
            for (int i = 0; i < currentBag.Count; i++)
            {
                int randomIndex = Random.Range(i, currentBag.Count);
                GameObject temp = currentBag[i];
                currentBag[i] = currentBag[randomIndex];
                currentBag[randomIndex] = temp;
            }
        }

        while (currentBag.Count > 0)
        {
            GameObject drawnBlock = currentBag[0];
            currentBag.RemoveAt(0); 
            if (drawnBlock != null)
            {
                currentDeck.Enqueue(drawnBlock);
                break;
            }
        }
    }

    public void RefillHand()
    {
        ShiftHandRight();

        for (int i = 0; i < activeHand.Length; i++)
        {
            if (activeHand[i] == null && currentDeck.Count > 0) activeHand[i] = currentDeck.Dequeue(); 
            UpdateSlotUI(i);
        }
        UpdateStockUI();
    }

    private void ShiftHandRight()
    {
        List<GameObject> tempList = new List<GameObject>();
        for (int i = 0; i < activeHand.Length; i++)
        {
            if (activeHand[i] != null) tempList.Add(activeHand[i]);
        }

        int startIndex = activeHand.Length - tempList.Count;

        for (int i = 0; i < activeHand.Length; i++)
        {
            if (i >= startIndex) activeHand[i] = tempList[i - startIndex];
            else activeHand[i] = null; 
        }
    }

    private void UpdateSlotUI(int slotIndex)
    {
        if (cardFrames[slotIndex] != null)
        {
            if (activeHand[slotIndex] != null)
            {
                cardFrames[slotIndex].gameObject.SetActive(true); 

                if (slotImages[slotIndex] != null)
                {
                    BlockData data = activeHand[slotIndex].GetComponent<BlockData>();
                    if (data != null && data.blockIcon != null)
                    {
                        slotImages[slotIndex].sprite = data.blockIcon;
                        slotImages[slotIndex].color = Color.white; 
                    }
                    else
                    {
                        slotImages[slotIndex].sprite = null;
                        slotImages[slotIndex].color = new Color(1f, 1f, 1f, 0f); 
                    }
                }
            }
            else
            {
                cardFrames[slotIndex].gameObject.SetActive(false); 
                if (slotImages[slotIndex] != null)
                {
                    slotImages[slotIndex].sprite = null;
                    slotImages[slotIndex].color = new Color(1f, 1f, 1f, 0f); 
                }
            }
        }
    }

    public void ForceRefreshUI()
    {
        for (int i = 0; i < activeHand.Length; i++) UpdateSlotUI(i);
        UpdateStockUI();
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
        if (isAnimating) return; 

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
            int slotToEmpty = currentSelectedSlot;
            currentSelectedSlot = -1;
            if (placementSystem != null) placementSystem.blockPrefab = null;

            StartCoroutine(AnimateCardUsage(slotToEmpty));
        }
    }

    private IEnumerator AnimateCardUsage(int usedSlotIndex)
    {
        isAnimating = true;

        List<CardMove> movers = new List<CardMove>();
        for (int i = 0; i < usedSlotIndex; i++)
        {
            if (activeHand[i] != null && slotImages[i] != null && slotImages[i].sprite != null)
            {
                movers.Add(new CardMove { toSlot = i + 1, sprite = slotImages[i].sprite });
            }
        }

        // --- HAPUS ANIMASI MENGECIL: LANGSUNG HILANG INSTAN ---
        if (cardFrames[usedSlotIndex] != null)
        {
            cardFrames[usedSlotIndex].gameObject.SetActive(false);
            if (slotImages[usedSlotIndex] != null)
            {
                slotImages[usedSlotIndex].sprite = null;
                slotImages[usedSlotIndex].color = new Color(1f, 1f, 1f, 0f);
            }
        }

        activeHand[usedSlotIndex] = null;
        GameObject incomingBlock = currentDeck.Count > 0 ? currentDeck.Peek() : null;

        RefillHand();

        foreach (CardMove move in movers)
        {
            if (slotImages[move.toSlot] != null) slotImages[move.toSlot].color = new Color(1f, 1f, 1f, 0f);
        }

        bool newCardArrived = incomingBlock != null && activeHand[0] != null;
        if (newCardArrived && slotImages[0] != null) slotImages[0].color = new Color(1f, 1f, 1f, 0f);

        List<Coroutine> flights = new List<Coroutine>();

        // 1. Animasi Kartu Lama yang Bergeser
        foreach (CardMove move in movers)
        {
            Vector3 startPos = cardFrames[move.toSlot - 1].position;
            flights.Add(StartCoroutine(PlayCardAppear(move.sprite, startPos, cardFrames[move.toSlot].position, move.toSlot)));
        }

        // 2. Animasi Kartu Baru dari DeckSpawnPoint
        if (newCardArrived)
        {
            BlockData incomingData = incomingBlock.GetComponent<BlockData>();
            Sprite newSprite = incomingData != null ? incomingData.blockIcon : null;

            Vector3 startPos = deckSpawnPoint != null ? deckSpawnPoint.position : cardFrames[0].position;
            flights.Add(StartCoroutine(PlayCardAppear(newSprite, startPos, cardFrames[0].position, 0)));
        }

        foreach (Coroutine flight in flights) yield return flight;

        CheckGameOverCondition();
        isAnimating = false;
    }

    private Image SpawnGhost(Sprite sprite, Vector3 worldStartPos)
    {
        RectTransform parent = animationLayer != null ? animationLayer : (RectTransform)cardFrames[0].parent;

        Image ghost = Instantiate(flyingIconPrefab, parent);
        ghost.gameObject.SetActive(true);
        ghost.sprite = sprite;
        ghost.color = Color.white;
        ghost.raycastTarget = false;

        ghost.rectTransform.position = worldStartPos;
        ghost.rectTransform.SetAsLastSibling();

        return ghost;
    }

    private IEnumerator PlayCardAppear(Sprite sprite, Vector3 fromWorldPos, Vector3 toWorldPos, int targetSlot)
    {
        if (sprite == null || flyingIconPrefab == null)
        {
            if (slotImages[targetSlot] != null) slotImages[targetSlot].color = Color.white;
            yield break;
        }

        Image ghost = SpawnGhost(sprite, fromWorldPos);
        ghost.rectTransform.localScale = Vector3.one * cardPopStartScale;

        float elapsed = 0f;
        while (elapsed < cardAppearDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cardAppearDuration);
            float smoothT = 1f - Mathf.Pow(1f - t, 3f); 

            ghost.rectTransform.position = Vector3.Lerp(fromWorldPos, toWorldPos, smoothT);
            float currentScale = Mathf.Lerp(cardPopStartScale, 1f, smoothT);
            ghost.rectTransform.localScale = Vector3.one * currentScale;

            yield return null;
        }

        ghost.rectTransform.position = toWorldPos;
        ghost.rectTransform.localScale = Vector3.one;
        if (slotImages[targetSlot] != null) slotImages[targetSlot].color = Color.white;
        Destroy(ghost.gameObject);
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
                int finalScore = 0; 
                if(ScoreManager.Instance != null) finalScore = ScoreManager.Instance.totalScore;
                UIManager.Instance.ShowGameOverPanel(finalScore);
            }
        }
    }

    public List<GameObject> GetCurrentAvailableBlocks()
    {
        List<GameObject> currentBlocks = new List<GameObject>();
        for (int i = 0; i < activeHand.Length; i++)
        {
            if (activeHand[i] != null) currentBlocks.Add(activeHand[i]);
        }
        return currentBlocks;
    }

    public void ShuffleDeck()
    {
        if (currentDeck.Count <= 1) return; 

        List<GameObject> tempList = new List<GameObject>(currentDeck);
        currentDeck.Clear(); 

        int count = tempList.Count;
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(i, count);
            GameObject temp = tempList[i];
            tempList[i] = tempList[randomIndex];
            tempList[randomIndex] = temp;
        }

        foreach (GameObject block in tempList)
        {
            currentDeck.Enqueue(block);
        }
    }
}