using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns clouds just off the right edge of the camera, drifts them leftward,
/// and destroys them once they’re well off‑screen.
/// </summary>
public class CloudSpawner : MonoBehaviour
{
    /* ───────────────────  INSPECTOR CONFIG  ─────────────────── */

    [Header("Cloud Prefabs")]
    [Tooltip("Assign the cloud prefabs you want to spawn (e.g., 4 variants).")]
    public GameObject[] cloudPrefabs;

    [Header("Spawn Timing")]
    [Tooltip("Inclusive range: how many clouds spawn in one burst.")]
    public Vector2Int cloudsPerBurst = new(3, 6);

    [Tooltip("Random delay (seconds) between bursts.")]
    public Vector2 spawnDelay = new(4f, 8f);

    [Header("Spawn Area (world units)")]
    [Tooltip("Random Y range at which clouds appear.")]
    public Vector2 heightRange = new(10f, 20f);

    [Tooltip("Horizontal offset range relative to the right edge of the camera.\n"
           + "Example: (-2, 4) gives a 6‑unit‑wide band in which clouds spawn.")]
    public Vector2 horizontalSpawnRange = new(-2f, 4f);

    [Header("Cloud Motion")]
    [Tooltip("Random speed range (world‑units / sec).")]
    public Vector2 speedRange = new(0.5f, 2f);

    [Tooltip("Buffer past camera edges for spawning & culling.")]
    public float offscreenPadding = 2f;
    public float morekillX = 2f;

    [Header("Hierarchy")]
    [Tooltip("Optional parent transform to keep the hierarchy tidy.")]
    public Transform cloudParent;

    /* ───────────────────  PRIVATE FIELDS  ─────────────────── */

    float spawnX;   // where clouds spawn (right side)
    float killX;    // where clouds are destroyed (left side)

    /* ───────────────────  UNITY LIFECYCLE  ─────────────────── */

    void Start()
    {
        Camera cam = Camera.main;
        spawnX = cam.ViewportToWorldPoint(new Vector3(1, 0, 0)).x + offscreenPadding;
        killX = cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).x - offscreenPadding - morekillX;

        StartCoroutine(SpawnRoutine());
    }

    /* ───────────────────  SPAWN LOGIC  ─────────────────── */

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // how many clouds in this burst?
            int count = Random.Range(cloudsPerBurst.x, cloudsPerBurst.y + 1);

            for (int i = 0; i < count; i++)
            {
                // pick a random prefab
                GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];

                // random Y height
                float y = Random.Range(heightRange.x, heightRange.y);

                // random horizontal offset within the spawn band
                float xOffset = Random.Range(horizontalSpawnRange.x, horizontalSpawnRange.y);

                // final spawn position
                Vector3 spawnPos = new Vector3(spawnX + xOffset, y, 0);

                // instantiate and set up movement
                GameObject cloud = Instantiate(prefab, spawnPos, Quaternion.identity, cloudParent);
                float speed = Random.Range(speedRange.x, speedRange.y);
                cloud.AddComponent<CloudMover>().Init(speed, killX);
            }

            // wait before the next burst
            yield return new WaitForSeconds(Random.Range(spawnDelay.x, spawnDelay.y));
        }
    }
}

/* ───────────────────  HELPER COMPONENT  ─────────────────── */

/// <summary>Moves a cloud leftward at a fixed speed and self‑destructs off‑screen.</summary>
public class CloudMover : MonoBehaviour
{
    float speed;
    float killX;

    /// <param name="spd">Leftward speed in world‑units per second.</param>
    /// <param name="killPosX">X position at which the cloud is destroyed.</param>
    public void Init(float spd, float killPosX)
    {
        speed = spd;
        killX = killPosX;
    }

    void Update()
    {
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        if (transform.position.x < killX)
            Destroy(gameObject);
    }
}
