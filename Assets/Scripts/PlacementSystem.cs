using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("References")]
    public GameObject highlightIndicator; 
    public LayerMask floorLayer;          

    private void Update()
    {
        DetectGridPosition();
    }

    private void DetectGridPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, floorLayer))
        {
            highlightIndicator.SetActive(true);

            Vector3 snapPosition = hit.transform.position;
            snapPosition.y += 0.1f; 

            highlightIndicator.transform.position = snapPosition;

        }
        else
        {
            highlightIndicator.SetActive(false);
        }
    }
}