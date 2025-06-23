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

[System.Serializable]
public class SeasonalCharm
{
    public Season season;
    public GameObject charmPrefab;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Charm Settings")]
    public Tilemap baseTilemap;

    [Tooltip("Assign charm prefabs for each season")]
    public SeasonalCharm[] seasonalCharms = new SeasonalCharm[4]; // Assign in inspector with proper season names!

    public int maxCharms = 3;
    public int charmsCollected = 0;

    [Header("Season Settings")]
    public Season currentSeason = Season.Spring;

    void Awake()
    {
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
        Debug.Log($"Charms Collected: {charmsCollected}/{maxCharms}");

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
        Debug.Log($"Season changed to: {currentSeason}");
        // Optional: update visuals or music here
    }

    private void SpawnCharms()
    {
        GameObject seasonPrefab = GetPrefabForSeason(currentSeason);
        if (seasonPrefab == null)
        {
            Debug.LogError($"No prefab assigned for season: {currentSeason}");
            return;
        }

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
            worldPos += new Vector3(0.5f, 0.5f, 0); // center charm on tile

            Instantiate(seasonPrefab, worldPos, Quaternion.identity);
            spawned++;
        }
    }

    GameObject GetPrefabForSeason(Season season)
    {
        foreach (var seasonalCharm in seasonalCharms)
        {
            if (seasonalCharm.season == season)
                return seasonalCharm.charmPrefab;
        }
        return null;
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
