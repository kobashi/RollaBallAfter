using UnityEngine;
using TMPro;

public class Ending : MonoBehaviour
{
    private TextMeshProUGUI scoreText;

    void Start()
    {
        scoreText = GetComponent<TextMeshProUGUI>();
        int totalScore = PlayerPrefs.GetInt("TotalScore", 0);
        if (scoreText != null)
        {
            scoreText.text = "Total Score: " + totalScore.ToString();
        }
    }
}
