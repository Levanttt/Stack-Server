using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardHoverSync : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Card Setup")]
    [Tooltip("Isi 0 untuk Card_1, 1 untuk Card_2, dan 2 untuk Card_3")]
    public int slotIndex; 

    [Header("=== MAIN CARD VISUAL ===")]
    public Image mainCardImage; 
    public Sprite mainNormal;
    public Sprite mainHover;
    public Sprite mainSelected;
    public Sprite mainSelectedHover; 

    [Header("=== SHORTCUT BG VISUAL ===")]
    public Image shortcutBgImage; 
    public Sprite shortcutNormal;   
    public Sprite shortcutHover;    
    public Sprite shortcutSelected; 
    public Sprite shortcutSelectedHover; 

    private bool isHovering = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        UpdateVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        UpdateVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Invoke(nameof(UpdateVisual), 0.01f);
    }

    private void UpdateVisual()
    {
        if (BlockQueueManager.Instance == null) return;
        if (BlockQueueManager.Instance.activeHand[slotIndex] == null) return;

        bool isSelected = (BlockQueueManager.Instance.currentSelectedSlot == slotIndex);

        if (mainCardImage != null)
        {
            if (isSelected)
                mainCardImage.sprite = isHovering ? mainSelectedHover : mainSelected;
            else
                mainCardImage.sprite = isHovering ? mainHover : mainNormal;
        }

        if (shortcutBgImage != null)
        {
            if (isSelected)
                shortcutBgImage.sprite = isHovering ? shortcutSelectedHover : shortcutSelected;
            else
                shortcutBgImage.sprite = isHovering ? shortcutHover : shortcutNormal;
        }
    }
    
    private void OnDisable()
    {
        isHovering = false;
        
        if (mainCardImage != null && mainNormal != null)
            mainCardImage.sprite = mainNormal;

        if (shortcutBgImage != null && shortcutNormal != null)
            shortcutBgImage.sprite = shortcutNormal;
    }
}