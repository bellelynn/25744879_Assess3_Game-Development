using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject scaredTimerObject;
    [SerializeField] private TMP_Text scaredTimerText;
    [SerializeField] private Image[] lifeIcons;
    [SerializeField] private GameObject blocker;
    [SerializeField] private TMP_Text centerText;
    [SerializeField] private Button exitButton;

    public void SetScore(int score)
    {
        scoreText.text = score.ToString("D6");
    }

    public void SetTime(float seconds)
    {
        timerText.text = System.TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss\:ff");
    }

    public void SetLives(int lives)
    {
        for (int i = 0; i < lifeIcons.Length; i++)
            lifeIcons[i].enabled = i < lives;
    }

    public void SetScaredTimer(float seconds)
    {
        int shown = Mathf.CeilToInt(seconds);
        scaredTimerObject.SetActive(shown > 0);
        scaredTimerText.text = shown.ToString();
    }

    public void ShowCenter(string message)
    {
        blocker.SetActive(true);
        centerText.gameObject.SetActive(true);
        centerText.text = message;
    }

    public void HideCenter()
    {
        blocker.SetActive(false);
        centerText.gameObject.SetActive(false);
    }

    public void SetButtonsInteractable(bool value)
    {
        exitButton.interactable = value;
    }

    public void ExitToStart()
    {
        SceneManager.LoadScene("StartScene");
    }
}