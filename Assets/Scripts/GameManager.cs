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
    public SeasonalCharm[] seasonalCharms = new SeasonalCharm[4];

    public int maxCharms = 3;
    public int charmsCollected = 0;

    [Header("Season Settings")]
    public Season currentSeason = Season.Spring;

    [Header("Seasonal Tilemaps")]
    public Tilemap springTilemap;
    public Tilemap summerTilemap;
    public Tilemap autumnTilemap;
    public Tilemap winterTilemap;

    [Header("Score and Timer")]
    public int totalScore = 0;
    public float startTime = 60f;          // total game time at start
    public float charmTimeBonus = 10f;     // how much time each charm adds

    private float timeRemaining;
    private bool gameOver = false;

    [Header("Music Settings")]
    public AudioClip springMusic;
    public AudioClip summerMusic;
    public AudioClip autumnMusic;
    public AudioClip winterMusic;

    private AudioSource musicSource;


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
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        timeRemaining = startTime;
        UpdateSeasonalTilemap();
        PlaySeasonMusic();
        SpawnCharms();
    }


    void Update()
    {
        if (gameOver) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            gameOver = true;
            Debug.Log("Time's up! Game over.");
            // TODO: Trigger end screen, restart, etc.
        }
    }

    public void AddCharm()
    {
        charmsCollected++;
        totalScore++;

        timeRemaining += charmTimeBonus;

        Debug.Log($"Charms Collected: {charmsCollected}/{maxCharms} | Score: {totalScore} | Time Left: {Mathf.FloorToInt(timeRemaining)}s");

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
        UpdateSeasonalTilemap();
        PlaySeasonMusic();
    }


    private void UpdateSeasonalTilemap()
    {
        springTilemap.gameObject.SetActive(false);
        summerTilemap.gameObject.SetActive(false);
        autumnTilemap.gameObject.SetActive(false);
        winterTilemap.gameObject.SetActive(false);

        switch (currentSeason)
        {
            case Season.Spring: springTilemap.gameObject.SetActive(true); break;
            case Season.Summer: summerTilemap.gameObject.SetActive(true); break;
            case Season.Autumn: autumnTilemap.gameObject.SetActive(true); break;
            case Season.Winter: winterTilemap.gameObject.SetActive(true); break;
        }
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

            Vector3Int spawnTilePos = tilePos + Vector3Int.up;

            if (baseTilemap.HasTile(spawnTilePos))
                continue;

            Vector3 worldPos = baseTilemap.CellToWorld(spawnTilePos) + new Vector3(0.5f, 0.5f, 0);
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

    // Optional: expose time remaining and score to UI
    public float GetTimeRemaining() => timeRemaining;
    public int GetScore() => totalScore;

    private void PlaySeasonMusic()
    {
        AudioClip clipToPlay = null;

        switch (currentSeason)
        {
            case Season.Spring:
                clipToPlay = springMusic;
                break;
            case Season.Summer:
                clipToPlay = summerMusic;
                break;
            case Season.Autumn:
                clipToPlay = autumnMusic;
                break;
            case Season.Winter:
                clipToPlay = winterMusic;
                break;
        }

        if (clipToPlay != null && musicSource.clip != clipToPlay)
        {
            musicSource.clip = clipToPlay;
            musicSource.Play();
        }
    }

}
