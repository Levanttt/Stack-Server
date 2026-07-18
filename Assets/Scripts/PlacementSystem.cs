using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("References")]
    public GameObject highlightIndicator;
    public LayerMask floorLayer;

    [Header("Placement Settings")]
    public GameObject blockPrefab; 

    private void Update()
    {
        DetectAndPlace();
    }

    private void DetectAndPlace()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, floorLayer))
        {
            highlightIndicator.SetActive(true);

            Vector3 tilePosition = hit.transform.position;
            
            Vector3 indicatorPos = tilePosition;
            indicatorPos.y += 0.2f; 
            highlightIndicator.transform.position = indicatorPos;

            if (Input.GetMouseButtonDown(0))
            {
                PlaceBlock(tilePosition);
            }
        }
        else
        {
            highlightIndicator.SetActive(false);
        }
    }

    private void PlaceBlock(Vector3 position)
    {
        Vector3 spawnPos = position;
        spawnPos.y += 0.5f; 

        Instantiate(blockPrefab, spawnPos, Quaternion.identity);
    }
}