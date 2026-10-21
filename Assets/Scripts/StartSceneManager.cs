using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class StartSceneManager : MonoBehaviour
{
    [SerializeField] private TMP_Text level1BestText;
    [SerializeField] private TMP_Text level2BestText;

    void Start()
    {
        ShowBest(level1BestText, "L1");
        ShowBest(level2BestText, "L2");
    }

    private void ShowBest(TMP_Text label, string levelKey)
    {
        if (label == null)
            return;

        int score = PlayerPrefs.GetInt(levelKey + "_HighScore", 0);
        float time = PlayerPrefs.GetFloat(levelKey + "_BestTime", 0f);

        string timeText = System.TimeSpan.FromSeconds(time).ToString(@"mm\:ss\:ff");
        label.text = "Best: " + score + "  Time: " + timeText;
    }

    public void LoadLevel1()
    {
        SceneManager.LoadScene("RecreatedLevel");
    }

    public void LoadLevel2()
    {
        SceneManager.LoadScene("InnovationScene");
    }
}