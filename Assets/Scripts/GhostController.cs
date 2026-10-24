using System.Collections.Generic;
using UnityEngine;

public class GhostController : MonoBehaviour
{
    public enum GhostState { Normal, Scared, Recovering, Dead }

    [SerializeField] private int ghostNumber = 1;
    [SerializeField] private string colourName = "Blue";   // matches the animator state names, e.g. Blue_Right
    [SerializeField] private float normalSpeed = 4.5f;      // 90% of PacStudent (5 tiles/s)
    [SerializeField] private float slowSpeed = 2.25f;       // scared, recovering and dead: 50% of normal

    public GhostState State { get; private set; } = GhostState.Normal;
    public int GhostNumber => ghostNumber;
    public bool HasLeftHouse => hasLeftHouse;
    public bool IsRunner { get; private set; }

    private RuntimeAnimatorController ghostAnimator;

    private static readonly Vector2Int[] Directions =
        { Vector2Int.up, Vector2Int.left, Vector2Int.down, Vector2Int.right };

    private Animator animator;
    private LevelGenerator level;
    private GameManager game;
    private Transform pacStudent;

    private string facing = "Right";
    private Vector3 startPosition;

    // Grid movement (directions are in world space: up = row - 1)
    private bool isLerping;
    private Vector3 fromPos;
    private Vector3 toPos;
    private float lerpProgress;
    private Vector2Int moveDir = Vector2Int.zero;
    private bool returningHome;

    private float CurrentSpeed => (State == GhostState.Normal || IsRunner) ? normalSpeed : slowSpeed;

    // Spawn area
    private bool hasLeftHouse;
    private readonly HashSet<Vector2Int> houseCells = new HashSet<Vector2Int>();   // (column, row)
    private readonly List<Vector2Int> gateCells = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> tunnelCells = new HashSet<Vector2Int>();
    private int centreRow;

    // Ghost 4: clockwise loop along the outside wall
    private readonly List<Vector2Int> outerLoop = new List<Vector2Int>();
    private readonly Dictionary<Vector2Int, int> outerLoopIndex = new Dictionary<Vector2Int, int>();

    void Awake()
    {
        animator = GetComponent<Animator>();
        ghostAnimator = animator.runtimeAnimatorController;
        startPosition = transform.position;
    }

    void Start()
    {
        level = FindAnyObjectByType<LevelGenerator>();
        game = FindAnyObjectByType<GameManager>();
        pacStudent = FindAnyObjectByType<PacStudentController>().transform;
        BuildMapInfo();
        BuildOuterLoop();
    }

    void Update()
    {
        if (game.State != GameManager.GameState.Playing)
            return;

        if (State == GhostState.Dead)
        {
            ReturnHome();
            return;
        }

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

    // Walkable for a ghost that has left the spawn area
    private bool IsOpenOutside(Vector2Int cell)
    {
        int tile = level.GetTile(cell.y, cell.x);
        return (tile == 0 || tile == 5 || tile == 6)
            && !tunnelCells.Contains(cell)
            && !houseCells.Contains(cell);
    }

    // Left-hand wall follower from the top-left corridor heading right:
    // keeping the wall on the left traces the outer wall clockwise.
    private void BuildOuterLoop()
    {
        Vector2Int start = new Vector2Int(1, 1);
        if (!IsOpenOutside(start))
            return;

        Vector2Int cell = start;
        Vector2Int dir = Vector2Int.right;

        for (int i = 0; i < 10000; i++)
        {
            if (!outerLoopIndex.ContainsKey(cell))
                outerLoopIndex[cell] = outerLoop.Count;
            outerLoop.Add(cell);

            Vector2Int left = new Vector2Int(-dir.y, dir.x);
            Vector2Int right = new Vector2Int(dir.y, -dir.x);
            Vector2Int[] tries = { left, dir, right, -dir };

            foreach (Vector2Int t in tries)
            {
                if (IsOpenOutside(Step(cell, t)))
                {
                    dir = t;
                    break;
                }
            }

            cell = Step(cell, dir);

            if (cell == start)
                break;
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
        lerpProgress += CurrentSpeed * Time.deltaTime;

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

    // Dead ghosts fly straight back to their spawn point, through walls, at a constant speed
    private void ReturnHome()
    {
        if (!returningHome)
        {
            returningHome = true;
            fromPos = transform.position;
            toPos = startPosition;
            lerpProgress = 0f;
        }

        float distance = Vector3.Distance(fromPos, toPos);
        lerpProgress += distance > 0f ? slowSpeed * Time.deltaTime / distance : 1f;

        if (lerpProgress < 1f)
        {
            transform.position = Vector3.Lerp(fromPos, toPos, lerpProgress);
            return;
        }

        transform.position = toPos;
        returningHome = false;
        isLerping = false;
        moveDir = Vector2Int.zero;
        hasLeftHouse = false;
        game.GhostReturnedHome(this);
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

    // Valid directions that are not a backstep; backstep only when there is no other choice
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

        // Scared, recovering and runner ghosts all run away like ghost 1
        int behaviour = (State == GhostState.Normal && !IsRunner) ? ghostNumber : 1;

        switch (behaviour)
        {
            case 1: return ByDistance(cell, options, true);
            case 2: return ByDistance(cell, options, false);
            case 4: return FollowOuterWall(cell, options);
            default: return RandomOf(options);
        }
    }

    private static Vector2Int RandomOf(List<Vector2Int> options)
    {
        return options[Random.Range(0, options.Count)];
    }

    // Ghost 1: random move that keeps PacStudent further or equal (straight-line distance).
    // Ghost 2: random move that keeps PacStudent closer or equal.
    private Vector2Int ByDistance(Vector2Int cell, List<Vector2Int> options, bool away)
    {
        Vector2 here = new Vector2(cell.x, -cell.y);
        Vector2 pac = pacStudent.position;
        float current = Vector2.Distance(here, pac);

        List<Vector2Int> filtered = new List<Vector2Int>();

        foreach (Vector2Int dir in options)
        {
            float next = Vector2.Distance(here + (Vector2)dir, pac);

            if (away ? next >= current : next <= current)
                filtered.Add(dir);
        }

        return RandomOf(filtered.Count > 0 ? filtered : options);
    }

    // Ghost 4: follow the clockwise outer loop; when not on it, take the shortest path to it
    private Vector2Int FollowOuterWall(Vector2Int cell, List<Vector2Int> options)
    {
        if (outerLoop.Count == 0)
            return RandomOf(options);

        if (outerLoopIndex.TryGetValue(cell, out int index))
        {
            Vector2Int next = outerLoop[(index + 1) % outerLoop.Count];
            Vector2Int dir = new Vector2Int(next.x - cell.x, cell.y - next.y);

            if (options.Contains(dir))
                return dir;
        }

        // Breadth-first search to the nearest loop cell (first step must be one of the options)
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> firstStep = new Dictionary<Vector2Int, Vector2Int>();

        foreach (Vector2Int dir in options)
        {
            Vector2Int next = Step(cell, dir);
            if (firstStep.ContainsKey(next))
                continue;

            firstStep[next] = dir;
            queue.Enqueue(next);
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (outerLoopIndex.ContainsKey(current))
                return firstStep[current];

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = Step(current, dir);

                if (next == cell || firstStep.ContainsKey(next) || !IsOpenOutside(next))
                    continue;

                firstStep[next] = firstStep[current];
                queue.Enqueue(next);
            }
        }

        return RandomOf(options);
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
        returningHome = false;
        moveDir = Vector2Int.zero;
        hasLeftHouse = false;
        facing = "Right";
        SetState(GhostState.Normal);
    }

    // Role swap: this ghost becomes the AI-controlled PacStudent (or turns back)
    public void SetRunner(bool runner, RuntimeAnimatorController pacAnimator)
    {
        IsRunner = runner;
        animator.runtimeAnimatorController = runner ? pacAnimator : ghostAnimator;
        PlayAnimation();
    }

    public void SetState(GhostState newState)
    {
        if (newState == GhostState.Dead)
        {
            isLerping = false;          // stop the grid move and head home instead
            returningHome = false;
        }

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
        if (IsRunner)
        {
            animator.Play(facing);      // PacStudent animator: Up / Down / Left / Right
            return;
        }

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
