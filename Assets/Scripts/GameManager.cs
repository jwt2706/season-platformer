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
    public int maxCharms = 3;
    public int charmsCollected = 0;

    [Tooltip("Assign charm prefabs for each season")]
    public SeasonalCharm[] seasonalCharms = new SeasonalCharm[4];

    [Header("Season Settings")]
    public Season currentSeason = Season.Spring;

    [Header("Seasonal Tilemaps")]
    public Tilemap springTilemap;
    public Tilemap summerTilemap;
    public Tilemap autumnTilemap;
    public Tilemap winterTilemap;

    [Header("Score and Timer")]
    public int totalScore = 0;
    public float startTime = 60f;
    public float charmTimeBonus = 10f;
    private float timeRemaining;
    private bool gameOver = false;

    [Header("Music Settings")]
    public AudioClip springMusic;
    public AudioClip summerMusic;
    public AudioClip autumnMusic;
    public AudioClip winterMusic;
    [Range(0f, 1f)] public float musicVolume = 1f;

    [Header("SFX Settings")]
    public AudioClip charmCollectSFX;
    [Range(0f, 1f)] public float charmCollectVolume = 1f;
    public AudioClip gameOverSFX;
    [Range(0f, 1f)] public float gameOverVolume = 1f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
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

    private void Start()
    {
        // Music source setup
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        // SFX source setup
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        timeRemaining = startTime;

        UpdateSeasonalTilemap();
        SpawnCharms();
        PlaySeasonMusic();
    }

    private void Update()
    {
        if (gameOver) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            TriggerGameOver();
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

        Tilemap activeTilemap = GetActiveTilemap();
        if (activeTilemap == null)
        {
            Debug.LogError("Active seasonal tilemap is null ‑ have you hooked up all references?");
            return;
        }

        // Collect every cell that contains a tile (our ground)
        List<Vector3Int> groundTiles = new List<Vector3Int>();
        BoundsInt bounds = activeTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                if (activeTilemap.HasTile(tilePos))
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

            // Spawn the charm on the tile directly above the ground tile so it sits nicely on top
            Vector3Int spawnTilePos = tilePos + Vector3Int.up;

            // Skip if the cell above already has a tile (e.g., a wall or another object)
            if (activeTilemap.HasTile(spawnTilePos)) continue;

            Vector3 worldPos = activeTilemap.CellToWorld(spawnTilePos) + new Vector3(0.25f, 0.25f, 0);
            Instantiate(seasonPrefab, worldPos, Quaternion.identity).transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            spawned++;
        }
    }

    public void AddCharm()
    {
        charmsCollected++;
        totalScore++;
        timeRemaining += charmTimeBonus;
        PlaySFX(charmCollectSFX, charmCollectVolume);

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
        UpdateSeasonalTilemap();
        PlaySeasonMusic();
    }

    private void UpdateSeasonalTilemap()
    {
        springTilemap.gameObject.SetActive(false);
        summerTilemap.gameObject.SetActive(false);
        autumnTilemap.gameObject.SetActive(false);
        winterTilemap.gameObject.SetActive(false);

        GetActiveTilemap()?.gameObject.SetActive(true);
    }

    private Tilemap GetActiveTilemap()
    {
        return currentSeason switch
        {
            Season.Spring => springTilemap,
            Season.Summer => summerTilemap,
            Season.Autumn => autumnTilemap,
            Season.Winter => winterTilemap,
            _ => null
        };
    }

    private void PlaySeasonMusic()
    {
        AudioClip clipToPlay = currentSeason switch
        {
            Season.Spring => springMusic,
            Season.Summer => summerMusic,
            Season.Autumn => autumnMusic,
            Season.Winter => winterMusic,
            _ => null
        };

        if (clipToPlay != null && musicSource.clip != clipToPlay)
        {
            musicSource.clip = clipToPlay;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    private void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }
    }

    private void TriggerGameOver()
    {
        if (gameOver) return; // safeguard
        gameOver = true;

        PlaySFX(gameOverSFX, gameOverVolume);
        musicSource.Stop();
    }

    private GameObject GetPrefabForSeason(Season season)
    {
        foreach (var c in seasonalCharms)
        {
            if (c.season == season)
            {
                return c.charmPrefab;
            }
        }
        return null;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    public float GetTimeRemaining() => timeRemaining;
    public int GetScore() => totalScore;
}
