using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Charm Settings")]
    public GameObject charmPrefab;
    public Tilemap baseTilemap;
    public int maxCharms = 3;
    public int charmsCollected = 0;

    [Header("Season Settings")]
    public Season currentSeason = Season.Spring;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SpawnCharms();
    }

    public void AddCharm()
    {
        charmsCollected++;
        Debug.Log("Charms Collected: " + charmsCollected + "/" + maxCharms);

        if (charmsCollected >= maxCharms)
        {
            charmsCollected = 0;
            NextSeason();
            SpawnCharms();
        }
    }

    public void NextSeason()
    {
        currentSeason = (Season)(((int)currentSeason + 1) % 4);
        Debug.Log("Season changed to: " + currentSeason);
        // You can trigger season-based visuals or logic here
    }

    public void SetSeason(Season newSeason)
    {
        currentSeason = newSeason;
        Debug.Log("Season manually set to: " + currentSeason);
    }

    private void SpawnCharms()
    {
        List<Vector3Int> groundTiles = new List<Vector3Int>();

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

        Shuffle(groundTiles);

        int spawned = 0;
        foreach (var tilePos in groundTiles)
        {
            if (spawned >= maxCharms) break;

            Vector3 worldPos = baseTilemap.CellToWorld(tilePos + Vector3Int.up);
            worldPos += new Vector3(0.5f, 0.5f, 0); // center charm

            Instantiate(charmPrefab, worldPos, Quaternion.identity);
            spawned++;
        }
    }

    private void Shuffle<T>(List<T> list)
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
