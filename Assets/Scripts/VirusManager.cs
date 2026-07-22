using System.Collections.Generic;
using UnityEngine;

public class VirusManager : MonoBehaviour
{
    public GridManager gridManager;

    public void NeutralizeVirus()
    {
        List<Vector2Int> firewallPositions = new List<Vector2Int>();
        foreach (var kvp in gridManager.gridMap)
        {
            if (kvp.Value.type == GridTileType.Firewall)
                firewallPositions.Add(kvp.Key);
        }

        Vector2Int[] dirs8 = { 
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };
        
        foreach (Vector2Int fPos in firewallPositions)
        {
            foreach (Vector2Int dir in dirs8)
            {
                Vector2Int targetPos = fPos + dir;
                if (gridManager.gridMap.TryGetValue(targetPos, out TileData tile))
                {
                    if (tile.type == GridTileType.Malware && !tile.isNeutralized)
                    {
                        tile.isNeutralized = true;
                        
                        ApplyNeutralizedVisual(tile.tileObject, targetPos); 
                        
                        Debug.Log($"Malware di {targetPos} berhasil dinetralisir oleh Firewall!");
                    }
                }
            }
        }
    }

    private void ApplyNeutralizedVisual(GameObject rootBlock, Vector2Int malwarePos)
    {
        if (rootBlock == null) return;

        MeshRenderer[] renderers = rootBlock.GetComponentsInChildren<MeshRenderer>();
        
        Vector2 targetGrid = new Vector2(malwarePos.x, malwarePos.y);

        foreach (MeshRenderer r in renderers)
        {
            Vector2 piecePos = new Vector2(r.transform.position.x, r.transform.position.z);
            
            float distance = Vector2.Distance(piecePos, targetGrid);

            string objName = r.gameObject.name.ToLower();
            if (distance <= 0.8f && (objName.Contains("virus") || objName.Contains("malware")))
            {
                Material[] mats = r.materials; 
                
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i].DisableKeyword("_EMISSION");
                    mats[i].globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                    
                    if (mats[i].HasProperty("_EmissionColor")) 
                    {
                        mats[i].SetColor("_EmissionColor", Color.black);
                    }

                    if (mats[i].HasProperty("_BaseColor")) 
                    {
                        Color currentColor = mats[i].GetColor("_BaseColor");
                        Color desaturatedColor = Color.Lerp(currentColor, Color.gray, 0.7f);
                        mats[i].SetColor("_BaseColor", desaturatedColor * 0.4f); 
                    }
                }
                
                r.materials = mats; 
            }
        }
    }

    public void UpdateInfectionVisuals()
    {
        if (gridManager == null) return;

        foreach (var kvp in gridManager.gridMap)
        {
            if (kvp.Value.tileObject != null)
            {
                TileVFX[] allVFX = kvp.Value.tileObject.GetComponentsInChildren<TileVFX>();
                foreach (TileVFX vfx in allVFX)
                {
                    vfx.SetInfectedVisual(false);
                }
            }
        }

        Vector2Int[] dirs8 = { 
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (var kvp in gridManager.gridMap)
        {
            if (kvp.Value.type == GridTileType.Malware && !kvp.Value.isNeutralized)
            {
                foreach (Vector2Int dir in dirs8)
                {
                    Vector2Int neighborPos = kvp.Key + dir;
                    
                    if (gridManager.gridMap.TryGetValue(neighborPos, out TileData neighborTile))
                    {
                        if (neighborTile.type != GridTileType.Malware && neighborTile.type != GridTileType.Firewall)
                        {
                            if (neighborTile.tileObject != null)
                            {
                                TileVFX[] allNeighborVFX = neighborTile.tileObject.GetComponentsInChildren<TileVFX>();
                                
                                foreach (TileVFX nVfx in allNeighborVFX)
                                {
                                    int vfxGridX = Mathf.RoundToInt(nVfx.transform.position.x);
                                    int vfxGridY = Mathf.RoundToInt(nVfx.transform.position.z);
                                    
                                    if (vfxGridX == neighborPos.x && vfxGridY == neighborPos.y)
                                    {
                                        nVfx.SetInfectedVisual(true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}