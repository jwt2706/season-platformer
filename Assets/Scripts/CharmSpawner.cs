using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class CharmSpawner : MonoBehaviour
{
    public Tilemap baseTilemap; // assign this in the Inspector
    public GameObject charmPrefab; // assign your charm prefab here

    public int maxCharms = 3;

    void Start()
    {
        SpawnCharms();
    }

    void SpawnCharms()
    {
        List<Vector3Int> groundTiles = new List<Vector3Int>();

        // Scan tilemap bounds
        BoundsInt bounds = baseTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                if (baseTilemap.HasTile(tilePos))
                {
                    groundTiles.Add(tilePos);
                }
            }
        }

        // Shuffle and spawn up to maxCharms
        Shuffle(groundTiles);

        int spawned = 0;
        foreach (var tilePos in groundTiles)
        {
            if (spawned >= maxCharms) break;

            Vector3 worldPos = baseTilemap.CellToWorld(tilePos + Vector3Int.up);
            worldPos += new Vector3(0.5f, 0.5f, 0); // center on tile

            Instantiate(charmPrefab, worldPos, Quaternion.identity);
            spawned++;
        }
    }

    // Utility: Fisher–Yates shuffle
    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[rand];
            list[rand] = temp;
        }
    }
}
