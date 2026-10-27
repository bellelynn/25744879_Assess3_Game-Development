using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Design innovation (Level 2): eating a Swap Token turns PacStudent into a ghost for a
// short time while a random ghost becomes an AI-controlled PacStudent that runs away.
// Catch it before the timer ends for bonus points.
public class RoleSwapController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject tokenPrefab;
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private PacStudentController pacStudent;
    [SerializeField] private GhostController[] ghosts;
    [SerializeField] private TMP_Text swapTimerText;

    [Header("Looks")]
    [SerializeField] private RuntimeAnimatorController pacStudentAnimator;
    [SerializeField] private RuntimeAnimatorController playerGhostAnimator;
    [SerializeField] private string playerGhostPrefix = "Red_";

    [Header("Rules")]
    [SerializeField] private float swapDuration = 10f;
    [SerializeField] private float tokenDelay = 15f;
    [SerializeField] private int catchScore = 500;

    public bool IsSwapped { get; private set; }

    private readonly List<Vector3> spawnCells = new List<Vector3>();
    private GhostController runner;
    private float swapTimer;

    void Start()
    {
        // Token spots: every corridor cell that starts with a pellet
        for (int row = 0; row < levelGenerator.Rows; row++)
            for (int column = 0; column < levelGenerator.Columns; column++)
                if (levelGenerator.GetTile(row, column) == 5)
                    spawnCells.Add(new Vector3(column, -row, 0f));

        swapTimerText.gameObject.SetActive(false);
        StartCoroutine(SpawnToken());
    }

    void Update()
    {
        if (!IsSwapped)
            return;

        // PacStudent died or the game ended: cancel the swap
        if (gameManager.State == GameManager.GameState.GameOver || gameManager.State == GameManager.GameState.Dying)
        {
            EndSwap();
            return;
        }

        if (gameManager.State != GameManager.GameState.Playing)
            return;

        swapTimer -= Time.deltaTime;
        swapTimerText.text = "SWAP " + Mathf.CeilToInt(Mathf.Max(swapTimer, 0f));

        if (swapTimer <= 0f)
            EndSwap();
    }

    private IEnumerator SpawnToken()
    {
        yield return new WaitForSeconds(tokenDelay);

        while (gameManager.State != GameManager.GameState.Playing)
            yield return null;

        Vector3 position = spawnCells[Random.Range(0, spawnCells.Count)];
        Instantiate(tokenPrefab, position, Quaternion.identity);
    }

    public void TokenCollected(GameObject token)
    {
        Destroy(token);

        if (!IsSwapped)
            StartSwap();

        StartCoroutine(SpawnToken());
    }

    private void StartSwap()
    {
        // Prefer a living ghost that is already out in the maze
        List<GhostController> candidates = new List<GhostController>();
        foreach (GhostController ghost in ghosts)
            if (ghost.State != GhostController.GhostState.Dead && ghost.HasLeftHouse)
                candidates.Add(ghost);

        if (candidates.Count == 0)
            foreach (GhostController ghost in ghosts)
                if (ghost.State != GhostController.GhostState.Dead)
                    candidates.Add(ghost);

        if (candidates.Count == 0)
            return;

        runner = candidates[Random.Range(0, candidates.Count)];
        runner.SetRunner(true, pacStudentAnimator);
        pacStudent.SetForm(playerGhostAnimator, playerGhostPrefix);

        IsSwapped = true;
        swapTimer = swapDuration;
        swapTimerText.gameObject.SetActive(true);
        audioManager.PlayGhostScared();
    }

    public void RunnerCaught()
    {
        if (!IsSwapped)
            return;

        GhostController caught = runner;
        gameManager.AddScore(catchScore);
        audioManager.PlayGhostEat();
        EndSwap();

        // The caught runner turns back into a ghost and flies home
        caught.SetState(GhostController.GhostState.Dead);
        gameManager.UpdateMusic();
    }

    private void EndSwap()
    {
        if (!IsSwapped)
            return;

        IsSwapped = false;
        swapTimerText.gameObject.SetActive(false);

        if (runner != null)
            runner.SetRunner(false, pacStudentAnimator);
        runner = null;

        pacStudent.SetForm(pacStudentAnimator, "");

        if (gameManager.State == GameManager.GameState.Playing)
            gameManager.UpdateMusic();
    }
}
