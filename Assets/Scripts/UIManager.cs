using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    void Update()
    {
        if (GameManager.Instance == null) return;

        float timeLeft = GameManager.Instance.GetTimeRemaining();

        int minutes = Mathf.FloorToInt(timeLeft / 60f);
        int seconds = Mathf.FloorToInt(timeLeft % 60f);
        int milliseconds = Mathf.FloorToInt((timeLeft * 1000f) % 1000f);

        timeText.text = $"Time Left: {minutes:00}:{seconds:00}.{milliseconds:000}";
    }
}
