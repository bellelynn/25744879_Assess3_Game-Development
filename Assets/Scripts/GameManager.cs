using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum GameState { Countdown, Playing, Dying, GameOver }

    [SerializeField] private HUDManager hud;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private GhostController[] ghosts;
    [SerializeField] private float scaredDuration = 10f;
    [SerializeField] private float recoveringTime = 3f;
    [SerializeField] private float deathDuration = 2f;
    [SerializeField] private PacStudentController pacStudent;
    [SerializeField] private string levelKey = "L1";      // PlayerPrefs prefix, "L2" for the innovation level
    [SerializeField] private float gameOverDelay = 3f;

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
        CheckAllPelletsEaten();
    }

    public void EatPowerPellet()
    {
        AddScore(50);
        pelletsLeft--;
        CheckAllPelletsEaten();

        scaredTimer = scaredDuration;
        SetLivingGhosts(GhostController.GhostState.Scared);
        hud.SetScaredTimer(scaredTimer);
        UpdateMusic();
    }

    public void EatGhost(GhostController ghost)
    {
        AddScore(300);
        ghost.SetState(GhostController.GhostState.Dead);
        audioManager.PlayGhostEat();
        UpdateMusic();
    }

    // A dead ghost that reaches its spawn point takes the state that matches the scared timer
    public void GhostReturnedHome(GhostController ghost)
    {
        if (scaredTimer > recoveringTime)
            ghost.SetState(GhostController.GhostState.Scared);
        else if (scaredTimer > 0f)
            ghost.SetState(GhostController.GhostState.Recovering);
        else
            ghost.SetState(GhostController.GhostState.Normal);

        UpdateMusic();
    }

    public void PacStudentCaught()
    {
        if (State != GameState.Playing)
            return;

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        State = GameState.Dying;   // stops movement, timers and collisions

        lives--;
        hud.SetLives(lives);
        audioManager.StopMusic();   // let the death sound play on its own
        pacStudent.Die();

        yield return new WaitForSeconds(deathDuration);

        if (lives <= 0)
        {
            StartCoroutine(GameOverSequence());
            yield break;
        }

        pacStudent.Respawn();

        foreach (GhostController ghost in ghosts)
            ghost.ResetGhost();

        scaredTimer = 0f;
        hud.SetScaredTimer(0f);
        UpdateMusic();

        State = GameState.Playing;
    }

    private void CheckAllPelletsEaten()
    {
        if (pelletsLeft <= 0)
            StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        if (State == GameState.GameOver)
            yield break;

        State = GameState.GameOver;   // stops player/ghost movement and the game timer

        pacStudent.Freeze();
        audioManager.StopMusic();
        hud.SetButtonsInteractable(false);
        hud.ShowCenter("Game Over");

        SaveHighScore();

        yield return new WaitForSeconds(gameOverDelay);
        SceneManager.LoadScene("StartScene");
    }

    // Save when the score beats the record, or ties it with a faster time
    private void SaveHighScore()
    {
        string scoreKey = levelKey + "_HighScore";
        string timeKey = levelKey + "_BestTime";

        bool hasRecord = PlayerPrefs.HasKey(scoreKey);
        int bestScore = PlayerPrefs.GetInt(scoreKey, 0);
        float bestTime = PlayerPrefs.GetFloat(timeKey, 0f);

        bool newRecord = !hasRecord
            || score > bestScore
            || (score == bestScore && gameTime < bestTime);

        if (!newRecord)
            return;

        PlayerPrefs.SetInt(scoreKey, score);
        PlayerPrefs.SetFloat(timeKey, gameTime);
        PlayerPrefs.Save();
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