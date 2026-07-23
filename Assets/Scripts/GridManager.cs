using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GridTileType { Kosong, Database, Cooling, Kabel_I, Kabel_L, Kabel_T, Kabel_Cross, Malware, Firewall }

[System.Serializable]
public class TileData
{
    public GridTileType type;
    public GameObject tileObject;
    public bool isNeutralized;

    public TileData(GridTileType _type, GameObject _obj)
    {
        type = _type;
        tileObject = _obj;
        isNeutralized = false;
    }
}

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Boundaries")]
    public int minX = 0;
    public int maxX = 4;
    public int minY = 0;
    public int maxY = 4;
    public float cellSize = 1f;

    [Header("Prefabs & References")]
    public GameObject floorTilePrefab;
    public Transform floorContainer;
    public Transform cameraTargetObject;

    [Header("Juice Animation Settings")]
    public float spawnInterval = 0.05f;
    public float animDuration = 0.3f;
    public float startOffsetY = -2f;

    [Header("Progression Settings")]
    public int currentMilestone = 1;

    public Dictionary<Vector2Int, GameObject> floorGrid = new Dictionary<Vector2Int, GameObject>();
    public Dictionary<Vector2Int, TileData> gridMap = new Dictionary<Vector2Int, TileData>();

    private readonly Vector2Int[] orthogonalDirs = new Vector2Int[]
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private enum ExpansionDirection { North, South, East, West }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        List<Vector2Int> initialTiles = GetNewFloorPositions(minX, maxX, minY, maxY);
        StartCoroutine(SpawnTilesAnim(initialTiles, true, 0));
        UpdateCenterTarget();

        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetInitialOffset();
        }
    }

    private void Update()
    {
        
    }

    public void ExpandGridBasedOnProgression()
    {
        int maxDirections = Mathf.Clamp(2 + (currentMilestone / 2), 2, 4);
        int directionsCount = Random.Range(2, maxDirections + 1);

        List<ExpansionDirection> allDirs = new List<ExpansionDirection> {
            ExpansionDirection.North, ExpansionDirection.South,
            ExpansionDirection.East, ExpansionDirection.West
        };

        for (int i = 0; i < allDirs.Count; i++)
        {
            int rnd = Random.Range(i, allDirs.Count);
            ExpansionDirection temp = allDirs[rnd];
            allDirs[rnd] = allDirs[i];
            allDirs[i] = temp;
        }

        int targetMinX = minX;
        int targetMaxX = maxX;
        int targetMinY = minY;
        int targetMaxY = maxY;

        for (int i = 0; i < directionsCount; i++)
        {
            switch (allDirs[i])
            {
                case ExpansionDirection.North: targetMaxY += 1; break;
                case ExpansionDirection.South: targetMinY -= 1; break;
                case ExpansionDirection.East:  targetMaxX += 1; break;
                case ExpansionDirection.West:  targetMinX -= 1; break;
            }
        }

        List<Vector2Int> newTilesToSpawn = new List<Vector2Int>();

        for (int x = targetMinX; x < targetMaxX; x++)
        {
            for (int y = targetMinY; y < targetMaxY; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!floorGrid.ContainsKey(pos))
                {
                    newTilesToSpawn.Add(pos);
                }
            }
        }

        minX = targetMinX;
        maxX = targetMaxX;
        minY = targetMinY;
        maxY = targetMaxY;

        if (newTilesToSpawn.Count > 0)
        {
            StartCoroutine(SpawnTilesAnim(newTilesToSpawn, false, directionsCount));
        }

        UpdateCenterTarget();
    }

    private List<Vector2Int> GetOutermostEdgeTiles(ExpansionDirection dir)
    {
        List<Vector2Int> edgeTiles = new List<Vector2Int>();
        
        int currentMinX = int.MaxValue, currentMaxX = int.MinValue;
        int currentMinY = int.MaxValue, currentMaxY = int.MinValue;

        foreach (var pos in floorGrid.Keys)
        {
            if (pos.x < currentMinX) currentMinX = pos.x;
            if (pos.x > currentMaxX) currentMaxX = pos.x;
            if (pos.y < currentMinY) currentMinY = pos.y;
            if (pos.y > currentMaxY) currentMaxY = pos.y;
        }

        if (dir == ExpansionDirection.East || dir == ExpansionDirection.West)
        {
            for (int y = currentMinY; y <= currentMaxY; y++)
            {
                int extremeX = (dir == ExpansionDirection.East) ? int.MinValue : int.MaxValue;
                bool found = false;
                foreach (var pos in floorGrid.Keys)
                {
                    if (pos.y == y)
                    {
                        if (dir == ExpansionDirection.East && pos.x > extremeX) { extremeX = pos.x; found = true; }
                        if (dir == ExpansionDirection.West && pos.x < extremeX) { extremeX = pos.x; found = true; }
                    }
                }
                if (found) edgeTiles.Add(new Vector2Int(extremeX, y));
            }
        }
        else
        {
            for (int x = currentMinX; x <= currentMaxX; x++)
            {
                int extremeY = (dir == ExpansionDirection.North) ? int.MinValue : int.MaxValue;
                bool found = false;
                foreach (var pos in floorGrid.Keys)
                {
                    if (pos.x == x)
                    {
                        if (dir == ExpansionDirection.North && pos.y > extremeY) { extremeY = pos.y; found = true; }
                        if (dir == ExpansionDirection.South && pos.y < extremeY) { extremeY = pos.y; found = true; }
                    }
                }
                if (found) edgeTiles.Add(new Vector2Int(x, extremeY));
            }
        }

        return edgeTiles;
    }

    private void UpdateCenterTarget()
    {
        if (cameraTargetObject == null) return;

        float centerX = (minX + maxX) / 2f * cellSize - (cellSize / 2f);
        float centerZ = (minY + maxY) / 2f * cellSize - (cellSize / 2f);

        cameraTargetObject.position = new Vector3(centerX, 0, centerZ);

        if (CameraController.Instance != null)
        {
            CameraController.Instance.CalculateAutoZoom();
        }
    }

    private List<Vector2Int> GetNewFloorPositions(int startX, int endX, int startY, int endY)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!floorGrid.ContainsKey(pos))
                {
                    positions.Add(pos);
                }
            }
        }
        return positions;
    }

    private IEnumerator SpawnTilesAnim(List<Vector2Int> tilesToSpawn, bool isInitialSpawn, int expandDirections = 0)
    {
        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;

        if (!isInitialSpawn && expandDirections >= 4)
        {
            List<Vector2Int> leftGroup = tilesToSpawn.FindAll(t => t.x < centerX);
            List<Vector2Int> rightGroup = tilesToSpawn.FindAll(t => t.x >= centerX);

            leftGroup.Sort((a, b) =>
            {
                float ringA = Mathf.Max(Mathf.Abs(a.x - centerX), Mathf.Abs(a.y - centerY));
                float ringB = Mathf.Max(Mathf.Abs(b.x - centerX), Mathf.Abs(b.y - centerY));
                int ringComp = ringA.CompareTo(ringB);
                if (ringComp != 0) return ringComp;
                return (a.x - a.y).CompareTo(b.x - b.y);
            });

            rightGroup.Sort((a, b) =>
            {
                float ringA = Mathf.Max(Mathf.Abs(a.x - centerX), Mathf.Abs(a.y - centerY));
                float ringB = Mathf.Max(Mathf.Abs(b.x - centerX), Mathf.Abs(b.y - centerY));
                int ringComp = ringA.CompareTo(ringB);
                if (ringComp != 0) return ringComp;
                return (b.x - b.y).CompareTo(a.x - a.y);
            });

            StartCoroutine(SpawnGroup(leftGroup));
            StartCoroutine(SpawnGroup(rightGroup));
        }
        else
        {
            tilesToSpawn.Sort((a, b) =>
            {
                if (!isInitialSpawn)
                {
                    float ringA = Mathf.Max(Mathf.Abs(a.x - centerX), Mathf.Abs(a.y - centerY));
                    float ringB = Mathf.Max(Mathf.Abs(b.x - centerX), Mathf.Abs(b.y - centerY));
                    int ringComp = ringA.CompareTo(ringB);
                    if (ringComp != 0) return ringComp;
                }
                return (a.x - a.y).CompareTo(b.x - b.y);
            });

            StartCoroutine(SpawnGroup(tilesToSpawn));
        }
        yield break;
    }

    private IEnumerator SpawnGroup(List<Vector2Int> group)
    {
        foreach (Vector2Int pos in group)
        {
            if (!floorGrid.ContainsKey(pos))
            {
                Vector3 finalPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
                Vector3 startPos = new Vector3(finalPos.x, startOffsetY, finalPos.z);

                GameObject spawnedTile = Instantiate(floorTilePrefab, startPos, Quaternion.identity, floorContainer);
                spawnedTile.name = $"FloorTile ({pos.x}, {pos.y})";
                
                floorGrid.Add(pos, spawnedTile);

                StartCoroutine(AnimateTile(spawnedTile.transform, startPos, finalPos));

                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }

    private IEnumerator AnimateTile(Transform tile, Vector3 startPos, Vector3 endPos)
    {
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            if (tile == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;
            float easeT = t * t * (3f - 2f * t);

            tile.position = Vector3.Lerp(startPos, endPos, easeT);
            yield return null;
        }

        if (tile != null)
        {
            tile.position = endPos;
        }
    }

    public void AddTileToGrid(Vector2Int pos, GridTileType type, GameObject obj)
    {
        if (!gridMap.ContainsKey(pos)) gridMap.Add(pos, new TileData(type, obj));
    }

    public List<TileData> GetOrthogonalNeighbors(Vector2Int pos)
    {
        List<TileData> neighbors = new List<TileData>();
        foreach (Vector2Int dir in orthogonalDirs)
        {
            Vector2Int neighborPos = pos + dir;
            if (gridMap.TryGetValue(neighborPos, out TileData neighborTile))
            {
                neighbors.Add(neighborTile);
            }
        }
        return neighbors;
    }
}