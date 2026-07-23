using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardHoverSync : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Card Setup")]
    [Tooltip("Isi 0 untuk Card_1, 1 untuk Card_2, dan 2 untuk Card_3")]
    public int slotIndex; 

    [Header("Child References")]
    public Image shortcutBgImage; 

    [Header("Sprites")]
    public Sprite normalSprite;   
    public Sprite hoverSprite;    
    public Sprite selectedSprite; // Masukkan sprite saat kartu sedang terpilih (Selected) ke sini

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shortcutBgImage == null || BlockQueueManager.Instance == null) return;

        // Jangan lakukan hover jika kartu di slot ini kosong
        if (BlockQueueManager.Instance.activeHand[slotIndex] == null) return;

        // Jangan timpa dengan efek hover jika kartu ini sedang dipilih (Selected)
        if (BlockQueueManager.Instance.currentSelectedSlot == slotIndex) return;

        if (hoverSprite != null)
            shortcutBgImage.sprite = hoverSprite;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (shortcutBgImage == null || BlockQueueManager.Instance == null) return;

        // Jangan update jika slot ini sedang kosong
        if (BlockQueueManager.Instance.activeHand[slotIndex] == null) return;

        // Saat mouse keluar, cek apakah kartu ini berstatus Selected
        if (BlockQueueManager.Instance.currentSelectedSlot == slotIndex)
        {
            // Jika ya, pertahankan warna Selected-nya!
            if (selectedSprite != null) shortcutBgImage.sprite = selectedSprite;
        }
        else
        {
            // Jika tidak terpilih, kembalikan ke warna Normal
            if (normalSprite != null) shortcutBgImage.sprite = normalSprite;
        }
    }
    
    private void OnDisable()
    {
        // Reset aman saat kartu disembunyikan/direset
        if (shortcutBgImage != null && BlockQueueManager.Instance != null)
        {
            if (BlockQueueManager.Instance.currentSelectedSlot == slotIndex && selectedSprite != null)
                shortcutBgImage.sprite = selectedSprite;
            else if (normalSprite != null)
                shortcutBgImage.sprite = normalSprite;
        }
    }
}