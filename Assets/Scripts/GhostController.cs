using System.Collections.Generic;
using UnityEngine;

public class GhostController : MonoBehaviour
{
    public enum GhostState { Normal, Scared, Recovering, Dead }

    [SerializeField] private int ghostNumber = 1;
    [SerializeField] private string colourName = "Blue";   // matches the animator state names, e.g. Blue_Right
    [SerializeField] private float speed = 4.5f;            // tiles per second

    public GhostState State { get; private set; } = GhostState.Normal;
    public int GhostNumber => ghostNumber;

    private static readonly Vector2Int[] Directions =
        { Vector2Int.up, Vector2Int.left, Vector2Int.down, Vector2Int.right };

    private Animator animator;
    private LevelGenerator level;
    private GameManager game;

    private string facing = "Right";
    private Vector3 startPosition;

    // Grid movement (directions are in world space: up = row - 1)
    private bool isLerping;
    private Vector3 fromPos;
    private Vector3 toPos;
    private float lerpProgress;
    private Vector2Int moveDir = Vector2Int.zero;

    // Spawn area
    private bool hasLeftHouse;
    private readonly HashSet<Vector2Int> houseCells = new HashSet<Vector2Int>();   // (column, row)
    private readonly List<Vector2Int> gateCells = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> tunnelCells = new HashSet<Vector2Int>();
    private int centreRow;

    void Awake()
    {
        animator = GetComponent<Animator>();
        startPosition = transform.position;
    }

    void Start()
    {
        level = FindAnyObjectByType<LevelGenerator>();
        game = FindAnyObjectByType<GameManager>();
        BuildMapInfo();
    }

    void Update()
    {
        if (game.State != GameManager.GameState.Playing)
            return;

        if (isLerping)
            ContinueLerp();

        if (!isLerping)
            ChooseNextMove();
    }

    // ---------------------------------------------------------------- map info

    private void BuildMapInfo()
    {
        // Spawn area: flood fill the empty cells around the centre of the map
        Vector2Int centre = new Vector2Int(level.Columns / 2, level.Rows / 2);
        centreRow = centre.y;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(centre);
        houseCells.Add(centre);

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = Step(cell, dir);

                if (houseCells.Contains(next) || level.GetTile(next.y, next.x) != 0)
                    continue;

                houseCells.Add(next);
                queue.Enqueue(next);
            }
        }

        // Ghost exit gates and the left/right teleport tunnels
        for (int row = 0; row < level.Rows; row++)
        {
            for (int column = 0; column < level.Columns; column++)
                if (level.GetTile(row, column) == 8)
                    gateCells.Add(new Vector2Int(column, row));

            for (int column = 0; column < level.Columns && level.GetTile(row, column) == 0; column++)
                tunnelCells.Add(new Vector2Int(column, row));

            for (int column = level.Columns - 1; column >= 0 && level.GetTile(row, column) == 0; column--)
                tunnelCells.Add(new Vector2Int(column, row));
        }
    }

    private static Vector2Int Step(Vector2Int cell, Vector2Int dir)
    {
        return new Vector2Int(cell.x + dir.x, cell.y - dir.y);
    }

    private Vector2Int CurrentCell()
    {
        return new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(-transform.position.y));
    }

    private bool CanEnter(Vector2Int cell)
    {
        int tile = level.GetTile(cell.y, cell.x);

        if (tile == 8)
            return !hasLeftHouse;               // gates only work from the inside

        if (tile != 0 && tile != 5 && tile != 6)
            return false;                       // walls and outside the map

        if (tunnelCells.Contains(cell))
            return false;                       // ghosts cannot use the teleporters

        if (hasLeftHouse && houseCells.Contains(cell))
            return false;                       // cannot re-enter the spawn area

        return true;
    }

    // ---------------------------------------------------------------- movement

    private void ContinueLerp()
    {
        lerpProgress += speed * Time.deltaTime;

        if (lerpProgress >= 1f)
        {
            transform.position = toPos;
            isLerping = false;
        }
        else
        {
            transform.position = Vector3.Lerp(fromPos, toPos, lerpProgress);
        }
    }

    private void ChooseNextMove()
    {
        Vector2Int cell = CurrentCell();

        if (!hasLeftHouse && !houseCells.Contains(cell) && level.GetTile(cell.y, cell.x) != 8)
            hasLeftHouse = true;

        Vector2Int dir = hasLeftHouse ? ChooseDirection(cell) : ExitDirection(cell);

        if (dir != Vector2Int.zero)
            StartMove(dir);
    }

    // Ghosts 1 and 3 leave through the top gate, ghosts 2 and 4 through the bottom gate
    private Vector2Int ExitDirection(Vector2Int cell)
    {
        bool useTopGate = ghostNumber == 1 || ghostNumber == 3;
        Vector2Int vertical = useTopGate ? Vector2Int.up : Vector2Int.down;

        Vector2Int gate = cell;
        int bestDistance = int.MaxValue;

        foreach (Vector2Int g in gateCells)
        {
            bool isTop = g.y < centreRow;

            if (isTop != useTopGate)
                continue;

            int distance = Mathf.Abs(g.x - cell.x);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                gate = g;
            }
        }

        if (gate.x != cell.x)
            return gate.x > cell.x ? Vector2Int.right : Vector2Int.left;

        return vertical;
    }

    // Any valid direction that is not a backstep; backstep only when there is no other choice
    private Vector2Int ChooseDirection(Vector2Int cell)
    {
        List<Vector2Int> options = new List<Vector2Int>();

        foreach (Vector2Int dir in Directions)
            if (dir != -moveDir && CanEnter(Step(cell, dir)))
                options.Add(dir);

        if (options.Count == 0 && moveDir != Vector2Int.zero && CanEnter(Step(cell, -moveDir)))
            options.Add(-moveDir);

        if (options.Count == 0)
            return Vector2Int.zero;

        return options[Random.Range(0, options.Count)];
    }

    private void StartMove(Vector2Int dir)
    {
        moveDir = dir;
        fromPos = transform.position;
        toPos = fromPos + new Vector3(dir.x, dir.y, 0f);
        lerpProgress = 0f;
        isLerping = true;

        SetFacing(DirectionName(dir));
    }

    private static string DirectionName(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return "Up";
        if (dir == Vector2Int.down) return "Down";
        if (dir == Vector2Int.left) return "Left";
        return "Right";
    }

    // ---------------------------------------------------------------- state and animation

    // Back to the spawn area in the Normal state (used when PacStudent dies)
    public void ResetGhost()
    {
        transform.position = startPosition;
        isLerping = false;
        moveDir = Vector2Int.zero;
        hasLeftHouse = false;
        facing = "Right";
        SetState(GhostState.Normal);
    }

    public void SetState(GhostState newState)
    {
        State = newState;
        PlayAnimation();
    }

    public void SetFacing(string direction)
    {
        facing = direction;
        PlayAnimation();
    }

    private void PlayAnimation()
    {
        switch (State)
        {
            case GhostState.Normal:
                animator.Play(colourName + "_" + facing);
                break;
            case GhostState.Scared:
                animator.Play("Scared_" + facing);
                break;
            case GhostState.Recovering:
                animator.Play("Recovering_" + facing);
                break;
            case GhostState.Dead:
                animator.Play("Ghost_Dead");
                break;
        }
    }
}
