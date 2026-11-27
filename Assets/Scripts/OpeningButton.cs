using UnityEngine;
using UnityEngine.SceneManagement;

public class OpeningButton : MonoBehaviour
{
    public void StartGame()
    {
        PlayerPrefs.SetInt("TotalScore", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene("MiniGame");
    }

    public void StartPistonPractice()
    {
        PlayerPrefs.SetInt("TotalScore", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene("MiniGame3");
    }
}
