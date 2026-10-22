using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState { Countdown, Playing, Dying, GameOver }

    [SerializeField] private HUDManager hud;
    [SerializeField] private AudioManager audioManager;

    public GameState State { get; private set; } = GameState.Countdown;

    private float gameTime;
    private int score;
    private int lives = 3;

    void Start()
    {
        hud.SetScore(score);
        hud.SetTime(0f);
        hud.SetLives(lives);
        hud.SetScaredTimer(0f);
        StartCoroutine(RoundStart());
    }

    void Update()
    {
        if (State != GameState.Playing)
            return;

        gameTime += Time.deltaTime;
        hud.SetTime(gameTime);
    }

    private IEnumerator RoundStart()
    {
        State = GameState.Countdown;
        audioManager.PlayIntro();

        string[] steps = { "3", "2", "1", "GO!" };
        foreach (string step in steps)
        {
            hud.ShowCenter(step);
            yield return new WaitForSeconds(1f);
        }

        hud.HideCenter();
        audioManager.PlayGhostNormal();
        State = GameState.Playing;
    }
}