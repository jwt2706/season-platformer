using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public enum Season { Spring, Summer, Autumn, Winter }

[System.Serializable]
public class SeasonalCharm
{
    public Season season;
    public GameObject charmPrefab;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    /* ─────────────────────────────  INSPECTOR  ───────────────────────────── */
    [Header("World & Generation")]
    [Tooltip("Tilemap where ground will be painted")]
    public Tilemap worldTilemap;                // single tilemap instead of four
    [Tooltip("Tile used for all ground blocks (can swap later per season)")]
    public TileBase groundTile;                 // one tile for now

    [Tooltip("Width (x) of generated world in tiles")]
    public int mapWidth = 128;
    [Tooltip("Max height (y) of terrain above 0 in tiles")]
    public int maxTerrainHeight = 12;
    [Tooltip("Vertical offset so terrain never dips below this level")]
    public int baseGroundHeight = 4;
    [Tooltip("Perlin‑noise scale; smaller = smoother hills")]
    public float noiseScale = .1f;

    // NEW ────────────────────────────────────────────────────────────────────
    [Header("Map Offset (cells)")]
    public Vector2Int mapOffset = Vector2Int.zero;   // e.g. (‑32,‑8)

    [Header("Charm Settings")]
    public int maxCharms = 3;
    public float charmHeightOffset = 1f;        // how high above ground to spawn
    public SeasonalCharm[] seasonalCharms = new SeasonalCharm[4];

    [Header("Score / Timer")]
    public float startTime = 60f;
    public float charmTimeBonus = 10f;

    /* — runtime state — */
    public Season currentSeason = Season.Spring;
    int charmsCollected = 0;
    int totalScore = 0;
    float timeRemaining = 0f;
    bool gameOver = false;

    /* ─────────────────────────────  AUDIO  ───────────────────────────── */
    [Header("Music")]
    public AudioClip springMusic, summerMusic, autumnMusic, winterMusic;
    [Range(0, 1)] public float musicVolume = 1;
    [Header("SFX")]
    public AudioClip charmCollectSFX, gameOverSFX;
    [Range(0, 1)] public float charmCollectVolume = 1, gameOverVolume = 1;

    AudioSource musicSource, sfxSource;

    /* ─────────────────────────────  LIFECYCLE  ───────────────────────────── */

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        // audio
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource = gameObject.AddComponent<AudioSource>();

        timeRemaining = startTime;

        BuildSeasonMap();      // generate tiles procedurally, now with offset
        SpawnCharms();         // charms spawned on offset map
        PlaySeasonMusic();
    }

    void Update()
    {
        if (gameOver) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f) { timeRemaining = 0; TriggerGameOver(); }
    }

    /* ─────────────────────────────  MAP GENERATION  ───────────────────────────── */

    /// <summary>Rebuilds the whole tilemap using Perlin noise and <c>mapOffset</c>.</summary>
    void BuildSeasonMap()
    {
        if (!worldTilemap || !groundTile)
        {
            Debug.LogError("Missing worldTilemap or groundTile reference.");
            return;
        }

        worldTilemap.ClearAllTiles();

        // Small season‑based offset so each season is noticeably different
        float seasonOffset = (int)currentSeason * 1000f;

        for (int x = 0; x < mapWidth; x++)
        {
            // Perlin returns 0‑1; scale to desired terrain height
            float noise = Mathf.PerlinNoise((x + seasonOffset) * noiseScale, 0f);
            int height = Mathf.RoundToInt(noise * maxTerrainHeight) + baseGroundHeight;

            for (int y = 0; y <= height; y++)
            {
                // APPLY GRID‑SPACE OFFSET HERE
                var cell = new Vector3Int(x + mapOffset.x, y + mapOffset.y, 0);
                worldTilemap.SetTile(cell, groundTile);
            }
        }
    }

    /* ─────────────────────────────  CHARMS  ───────────────────────────── */

    void SpawnCharms()
    {
        GameObject seasonPrefab = GetPrefabForSeason(currentSeason);
        if (!seasonPrefab) { Debug.LogError($"No prefab for {currentSeason}"); return; }

        // gather all top‑ground cells to use as spawn anchors
        List<Vector3Int> spawnCells = new();
        for (int x = 0; x < mapWidth; x++)
        {
            int worldX = x + mapOffset.x;               // honour X offset

            for (int y = worldTilemap.cellBounds.yMax; y >= worldTilemap.cellBounds.yMin; y--)
            {
                var cell = new Vector3Int(worldX, y, 0);
                if (worldTilemap.HasTile(cell))
                {
                    spawnCells.Add(cell + Vector3Int.up); // 1 tile above ground
                    break;
                }
            }
        }

        Shuffle(spawnCells);
        int spawned = 0;

        foreach (var cell in spawnCells)
        {
            if (spawned >= maxCharms) break;
            if (worldTilemap.HasTile(cell)) continue;   // something already there?

            // world position centred in cell, with charmHeightOffset lift
            Vector3 worldPos = worldTilemap.CellToWorld(cell) + new Vector3(0.25f, 0.25f, 0);
            Instantiate(seasonPrefab, worldPos, Quaternion.identity)
                .transform.localScale = Vector3.one * .5f;
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
        }
    }

    /* ─────────────────────────────  SEASONS  ───────────────────────────── */

    void NextSeason()
    {
        currentSeason = (Season)(((int)currentSeason + 1) % 4);
        BuildSeasonMap();   // rebuild tiles on same offset grid
        SpawnCharms();      // new charms on new map
        PlaySeasonMusic();
    }

    /* ─────────────────────────────  AUDIO / UTILS  ───────────────────────────── */

    void PlaySeasonMusic()
    {
        AudioClip clip = currentSeason switch
        {
            Season.Spring => springMusic,
            Season.Summer => summerMusic,
            Season.Autumn => autumnMusic,
            Season.Winter => winterMusic,
            _ => null
        };

        if (clip && musicSource.clip != clip)
        {
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    void PlaySFX(AudioClip clip, float vol = 1) { if (clip) sfxSource.PlayOneShot(clip, vol); }

    void TriggerGameOver()
    {
        if (gameOver) return;
        gameOver = true;
        PlaySFX(gameOverSFX, gameOverVolume);
        musicSource.Stop();
    }

    GameObject GetPrefabForSeason(Season s) =>
        System.Array.Find(seasonalCharms, c => c.season == s)?.charmPrefab;

    static void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
            (list[i], list[Random.Range(i, list.Count)]) = (list[Random.Range(i, list.Count)], list[i]);
    }

    /* ─────────────────────────────  PUBLIC GETTERS  ───────────────────────────── */
    public float GetTimeRemaining() => timeRemaining;
    public int GetScore() => totalScore;
}
