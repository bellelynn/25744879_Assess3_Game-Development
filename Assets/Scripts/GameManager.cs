using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState { Countdown, Playing, Dying, GameOver }

    [SerializeField] private HUDManager hud;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private LevelGenerator levelGenerator;

    public GameState State { get; private set; } = GameState.Countdown;

    private float gameTime;
    private int score;
    private int lives = 3;
    private int pelletsLeft;

    void Start()
    {
        for (int row = 0; row < levelGenerator.Rows; row++)
            for (int column = 0; column < levelGenerator.Columns; column++)
            {
                int tile = levelGenerator.GetTile(row, column);
                if (tile == 5 || tile == 6)
                    pelletsLeft++;
            }

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

    public void AddScore(int amount)
    {
        score += amount;
        hud.SetScore(score);
    }

    public void EatPellet()
    {
        AddScore(10);
        pelletsLeft--;
    }

    public void EatCherry()
    {
        AddScore(100);
        audioManager.PlayBonusEat();
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