using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState { Countdown, Playing, Dying, GameOver }

    [SerializeField] private HUDManager hud;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private GhostController[] ghosts;
    [SerializeField] private float scaredDuration = 10f;
    [SerializeField] private float recoveringTime = 3f;

    public GameState State { get; private set; } = GameState.Countdown;

    private float gameTime;
    private int score;
    private int lives = 3;
    private int pelletsLeft;
    private float scaredTimer;

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

        UpdateScaredTimer();
    }

    private void UpdateScaredTimer()
    {
        if (scaredTimer <= 0f)
            return;

        float previous = scaredTimer;
        scaredTimer -= Time.deltaTime;

        // 3 seconds left: scared ghosts start recovering
        if (previous > recoveringTime && scaredTimer <= recoveringTime)
            SetLivingGhosts(GhostController.GhostState.Recovering);

        // Timer finished: back to normal (dead ghosts are not changed)
        if (scaredTimer <= 0f)
        {
            scaredTimer = 0f;
            SetLivingGhosts(GhostController.GhostState.Normal);
            UpdateMusic();
        }

        hud.SetScaredTimer(scaredTimer);
    }

    private void SetLivingGhosts(GhostController.GhostState state)
    {
        foreach (GhostController ghost in ghosts)
            if (ghost.State != GhostController.GhostState.Dead)
                ghost.SetState(state);
    }

    // Dead music if any ghost is dead, otherwise scared music while the timer runs, otherwise normal
    private void UpdateMusic()
    {
        foreach (GhostController ghost in ghosts)
            if (ghost.State == GhostController.GhostState.Dead)
            {
                audioManager.PlayGhostDead();
                return;
            }

        if (scaredTimer > 0f)
            audioManager.PlayGhostScared();
        else
            audioManager.PlayGhostNormal();
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

    public void EatPowerPellet()
    {
        AddScore(50);
        pelletsLeft--;

        scaredTimer = scaredDuration;
        SetLivingGhosts(GhostController.GhostState.Scared);
        hud.SetScaredTimer(scaredTimer);
        UpdateMusic();
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