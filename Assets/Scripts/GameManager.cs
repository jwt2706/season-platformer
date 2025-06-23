using UnityEngine;

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

    public int charmsCollected = 0;

    public Season currentSeason = Season.Spring; // Default to Spring

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

    public void AddCharm()
    {
        charmsCollected++;
        NextSeason();
        Debug.Log("Charms Collected: " + charmsCollected);
    }

    public void NextSeason()
    {
        currentSeason = (Season)(((int)currentSeason + 1) % 4);
        Debug.Log("Season changed to: " + currentSeason);
        // Optional: Trigger changes (e.g., update visuals, music)
    }

    public void SetSeason(Season newSeason)
    {
        currentSeason = newSeason;
        Debug.Log("Season manually set to: " + currentSeason);
    }
}
