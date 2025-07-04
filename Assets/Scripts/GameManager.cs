﻿using UnityEngine;
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
    public Tilemap worldTilemap;

    [Header("Seasonal ground tiles")]
    public TileBase springGroundTile;
    public TileBase summerGroundTile;
    public TileBase autumnGroundTile;
    public TileBase winterGroundTile;
    public TileBase dirtTile;

    [Header("Platform parameters")]
    [Range(0.05f, 1f)]
    public float platformDensity = 0.4f;
    [Range(0f, 1f)]
    public float chaosLevel = 0.0f;
    [Range(1, 4)]
    public int maxPlatformThickness = 2;
    public int minPlatformLength = 2;
    public int maxPlatformLength = 6;
    public int minGapLength = 2;
    public int maxGapLength = 6;

    [Header("Vertical layering")]
    public int firstLayerHeight = 4;          // y above ground
    public int lastLayerHeight = 14;         // highest layer
    public int layerStep = 3;          // vertical spacing between layers

    [Header("Player reachability")]
    public int maxJumpHorizontal = 8;
    public int maxJumpVertical = 6;

    [Tooltip("Width (x) of generated world in tiles")]
    public int mapWidth = 128;
    [Tooltip("Max height (y) of terrain above 0 in tiles")]
    public int maxTerrainHeight = 12;
    [Tooltip("Vertical offset so terrain never dips below this level")]
    public int baseGroundHeight = 4;
    [Tooltip("Perlin‑noise scale; smaller = smoother hills")]
    public float noiseScale = .1f;

    public SpriteRenderer wallpaperRendererSpring;
    public SpriteRenderer wallpaperRendererSummer;
    public SpriteRenderer wallpaperRendererFall;
    public SpriteRenderer wallpaperRendererWinter;

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
    public int seasonCounter = 0;
    public int yearCounter = 0;

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
        UpdateWallpaper();
    }

    void Update()
    {
        if (gameOver) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f) { timeRemaining = 0; TriggerGameOver(); }
    }

    /* ─────────────────────────────  MAP GENERATION  ───────────────────────────── */

    /// <summary>Rebuilds the whole tilemap using Perlin noise and <c>mapOffset</c>.</summary>
    public void BuildSeasonMap()
    {
        TileBase seasonGroundTile = GetGroundTileForSeason();
        if (!worldTilemap || !seasonGroundTile)
        {
            Debug.LogError("Missing worldTilemap or season tile reference");
            return;
        }

        worldTilemap.ClearAllTiles();

        /* ───────────────────────────
         * 1)  Flat ground strip
         * ───────────────────────────*/
        for (int x = 0; x < mapWidth; x++)
        {
            var cell = new Vector3Int(x + mapOffset.x, baseGroundHeight + mapOffset.y, 0);
            worldTilemap.SetTile(cell, seasonGroundTile);
        }

        /* ───────────────────────────
         * 2)  Multiple platform layers
         * ───────────────────────────*/
        for (int layerY = firstLayerHeight;
             layerY <= lastLayerHeight;
             layerY += layerStep)
        {
            int xCursor = 0;
            while (xCursor < mapWidth)
            {
                // Decide whether to spawn a platform chunk or leave a gap
                bool spawnPlatform = Random.value < (platformDensity + (0.05 * yearCounter));

                if (spawnPlatform)
                {
                    int length = Random.Range(minPlatformLength, maxPlatformLength + 1);
                    length = Mathf.Min(length, mapWidth - xCursor);           // don’t spill past edge

                    // Horizontal reach check against previous layer:
                    // Find a platform that is no farther than maxJumpHorizontal
                    // from *some* platform on the layer below (or ground).
                    // Simplest approach: require length ≤ maxJumpHorizontal.
                    length = Mathf.Min(length, maxJumpHorizontal);

                    PlaceInterestingPlatform(xCursor, layerY, length, seasonGroundTile);
                    xCursor += length;

                }
                else  // leave gap
                {
                    int gap = Random.Range(minGapLength, maxGapLength + 1);
                    gap = Mathf.Min(gap, maxJumpHorizontal);                  // keep jump‑able
                    xCursor += gap;
                }
            }
        }
    }

    void PlaceInterestingPlatform(int startX, int baseY, int length, TileBase surfaceTile)
    {
        int currentY = baseY;

        float currChaosLevel = chaosLevel + (0.1f * yearCounter);

        for (int i = 0; i < length; i++)
        {
            /* ── 1.  Random hole ───────────────────────────────────────────── */
            if (Random.value < 0.05f * currChaosLevel)  // more chaos ⇒ more holes
            {
                startX++;
                continue;
            }

            /* ── 2.  Tiny ups/downs for “wiggle” ───────────────────────────── */
            if (Random.value < 0.2f * currChaosLevel)   // more chaos ⇒ more bumps
            {
                int shift = Random.value < 0.5f ? -1 : 1;
                currentY = Mathf.Clamp(currentY + shift, baseY - 2, baseY + 2);
            }

            /* ── 3.  Thickness driven by chaosLevel ────────────────────────── */
            int thickness = 1;

            if (Random.value < 0.7f * currChaosLevel)
            {
                float t = Mathf.Pow(Random.value, 1f - currChaosLevel);
                thickness = 1 + Mathf.FloorToInt(t * (maxPlatformThickness - 1));
            }

            /* ── 4.  Paint the vertical stack ─────────────────────────────── */
            for (int h = 0; h < thickness; h++)
            {
                TileBase tileToUse = (h == 0) ? surfaceTile : dirtTile;
                var cell = new Vector3Int(
                    startX + mapOffset.x,
                    currentY - h + mapOffset.y,
                    0);

                worldTilemap.SetTile(cell, tileToUse);
            }

            startX++;
        }
    }

    TileBase GetGroundTileForSeason()
    {
        return currentSeason switch
        {
            Season.Spring => springGroundTile,
            Season.Summer => summerGroundTile,
            Season.Autumn => autumnGroundTile,
            Season.Winter => winterGroundTile,
            _ => null
        };
    }

    void UpdateWallpaper()
    {
        wallpaperRendererSpring.enabled = (currentSeason == Season.Spring);
        wallpaperRendererSummer.enabled = (currentSeason == Season.Summer);
        wallpaperRendererFall.enabled = (currentSeason == Season.Autumn);
        wallpaperRendererWinter.enabled = (currentSeason == Season.Winter);
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
        seasonCounter++;
        if (seasonCounter == 4)
        {
            seasonCounter = 0;
            yearCounter++;
        }
        currentSeason = (Season)(((int)currentSeason + 1) % 4);
        BuildSeasonMap();
        SpawnCharms();
        PlaySeasonMusic();
        UpdateWallpaper();
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
