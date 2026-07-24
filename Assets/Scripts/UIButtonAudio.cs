using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class UIButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("UI Audio Settings")]
    public SoundFX hoverSFX;
    public SoundFX clickSFX;

    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && selectable.interactable)
        {
            if (AudioManager.Instance != null && hoverSFX != null && hoverSFX.clip != null)
            {
                AudioManager.Instance.PlaySFX(hoverSFX);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (selectable != null && selectable.interactable)
        {
            if (AudioManager.Instance != null && clickSFX != null && clickSFX.clip != null)
            {
                AudioManager.Instance.PlaySFX(clickSFX);
            }
        }
    }
}