using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private ParticleSystem dustEffect;
    [SerializeField] private float speed = 5f;   // tiles per second

    private Vector2Int lastInput = Vector2Int.zero;
    private Vector2Int currentInput = Vector2Int.zero;

    private Animator animator;
    private bool isLerping;
    private bool isMoving;
    private Vector3 startPos;
    private Vector3 targetPos;
    private float lerpProgress;

    void Start()
    {
        animator = GetComponent<Animator>();

        // Snap to the grid and face right, waiting for input
        transform.position = new Vector3(
            Mathf.Round(transform.position.x),
            Mathf.Round(transform.position.y),
            0f);

        animator.Play("Right");
        animator.speed = 0f;
    }

    void Update()
    {
        ReadInput();

        if (isLerping)
            ContinueLerp();

        if (!isLerping)
            TryMove();
    }

    private void ReadInput()
    {
        if (Input.GetKeyDown(KeyCode.W)) lastInput = Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.A)) lastInput = Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.S)) lastInput = Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.D)) lastInput = Vector2Int.right;
    }

    private void ContinueLerp()
    {
        lerpProgress += speed * Time.deltaTime;

        if (lerpProgress >= 1f)
        {
            transform.position = targetPos;
            isLerping = false;
        }
        else
        {
            transform.position = Vector3.Lerp(startPos, targetPos, lerpProgress);
        }
    }

    private void TryMove()
    {
        if (lastInput != Vector2Int.zero && CanMove(lastInput))
        {
            currentInput = lastInput;
            StartLerp(currentInput);
        }
        else if (currentInput != Vector2Int.zero && CanMove(currentInput))
        {
            StartLerp(currentInput);
        }
        else
        {
            StopMoving();
        }
    }

    // x = column, y = row
    private Vector2Int CurrentCell()
    {
        return new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(-transform.position.y));
    }

    private bool CanMove(Vector2Int direction)
    {
        Vector2Int cell = CurrentCell();
        return levelGenerator.IsWalkable(cell.y - direction.y, cell.x + direction.x);
    }

    private void StartLerp(Vector2Int direction)
    {
        startPos = transform.position;
        targetPos = startPos + new Vector3(direction.x, direction.y, 0f);
        lerpProgress = 0f;
        isLerping = true;
        isMoving = true;

        animator.speed = 1f;
        animator.Play(DirectionName(direction));

        Vector2Int cell = CurrentCell();
        int nextTile = levelGenerator.GetTile(cell.y - direction.y, cell.x + direction.x);
        bool eating = nextTile == 5 || nextTile == 6;

        if (audioManager != null)
            audioManager.PlayMoveLoop(eating);

        if (dustEffect != null && !dustEffect.isPlaying)
            dustEffect.Play();
    }

    private void StopMoving()
    {
        if (!isMoving)
            return;

        isMoving = false;
        animator.speed = 0f;

        if (audioManager != null)
            audioManager.StopMovement();

        if (dustEffect != null)
            dustEffect.Stop();
    }

    private string DirectionName(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return "Up";
        if (direction == Vector2Int.down) return "Down";
        if (direction == Vector2Int.left) return "Left";
        return "Right";
    }
}