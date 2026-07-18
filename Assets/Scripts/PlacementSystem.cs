using System.Collections.Generic;
using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("References")]
    public GameObject highlightIndicator;
    public LayerMask floorLayer;

    [Header("Placement Settings")]
    public GameObject blockPrefab;

    private HashSet<Vector3> occupiedTiles = new HashSet<Vector3>();
    
    private float currentRotation = 0f;

    private void Update()
    {
        HandleRotation();
        DetectAndPlace();
    }

    private void HandleRotation()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotation += 90f;
            highlightIndicator.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
        }
    }

    private void DetectAndPlace()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, floorLayer))
        {
            Vector3 tilePosition = hit.transform.position;

            if (occupiedTiles.Contains(tilePosition))
            {
                highlightIndicator.SetActive(false);
                return; 
            }

            highlightIndicator.SetActive(true);

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

        Instantiate(blockPrefab, spawnPos, Quaternion.Euler(0, currentRotation, 0));
        
        occupiedTiles.Add(position);
    }
}