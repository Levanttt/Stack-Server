using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class BlockQueueManager : MonoBehaviour
{
    [Header("Block Catalog (Katalog)")]
    public List<GameObject> availableBlocks = new List<GameObject>(); 
    
    [Header("Deck Settings")]
    public int deckCapacity = 15; 
    private Queue<GameObject> currentDeck = new Queue<GameObject>();

    [Header("Hand Settings (3 Slot)")]
    public GameObject[] activeHand = new GameObject[3];
    public int currentSelectedSlot = -1;

    [Header("UI References")]
    [Tooltip("Masukkan Card_1, Card_2, Card_3 yang ada komponen Image-nya")]
    public Image[] slotImages = new Image[3]; 
    // emptySlotSprite sudah dihapus sesuai permintaan

    [Header("References")]
    public PlacementSystem placementSystem;

    private void Start()
    {
        InitializeDeck();
    }

    public void InitializeDeck()
    {
        currentDeck.Clear();
        for (int i = 0; i < deckCapacity; i++)
        {
            int randomIndex = Random.Range(0, availableBlocks.Count);
            currentDeck.Enqueue(availableBlocks[randomIndex]);
        }
        RefillHand();
    }

    public void RefillHand()
    {
        // 1. Geser kartu yang tersisa maju ke depan untuk menutup celah kosong (Shift)
        ShiftHandLeft();

        // 2. Tarik kartu baru dari deck untuk mengisi slot kosong di ujung (jika masih ada)
        for (int i = 0; i < activeHand.Length; i++)
        {
            if (activeHand[i] == null && currentDeck.Count > 0)
            {
                activeHand[i] = currentDeck.Dequeue(); 
            }
            
            // 3. Update gambar UI
            UpdateSlotUI(i);
        }
    }

    // Fungsi baru untuk menggeser kartu
    private void ShiftHandLeft()
    {
        int targetIndex = 0;
        for (int i = 0; i < activeHand.Length; i++)
        {
            if (activeHand[i] != null)
            {
                if (i != targetIndex)
                {
                    activeHand[targetIndex] = activeHand[i]; // Pindah ke slot kosong di depannya
                    activeHand[i] = null; // Kosongkan slot yang lama
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
                    slotImages[slotIndex].color = Color.white; // Tampilkan dengan jelas
                }
            }
            else
            {
                // Jika benar-benar habis, buat transparan / tak terlihat
                slotImages[slotIndex].sprite = null;
                slotImages[slotIndex].color = new Color(1, 1, 1, 0f); 
            }
        }
    }

    public void SelectBlock(int slotIndex)
    {
        if (activeHand[slotIndex] != null)
        {
            currentSelectedSlot = slotIndex;
            placementSystem.blockPrefab = activeHand[slotIndex];
            Debug.Log($"[Queue] Player memegang blok dari Slot {slotIndex + 1}");
        }
    }

    public void OnBlockPlacedSuccessfully()
    {
        if (currentSelectedSlot != -1)
        {
            activeHand[currentSelectedSlot] = null;
            currentSelectedSlot = -1;
            placementSystem.blockPrefab = null;
            
            RefillHand(); // Ini otomatis memicu geser kartu lalu menarik kartu baru
        }
    }
}