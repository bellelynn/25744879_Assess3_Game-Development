using UnityEngine;

public class GhostController : MonoBehaviour
{
    public enum GhostState { Normal, Scared, Recovering, Dead }

    [SerializeField] private int ghostNumber = 1;
    [SerializeField] private string colourName = "Blue";   // matches the animator state names, e.g. Blue_Right

    public GhostState State { get; private set; } = GhostState.Normal;
    public int GhostNumber => ghostNumber;

    private Animator animator;
    private string facing = "Right";
    private Vector3 startPosition;

    void Awake()
    {
        animator = GetComponent<Animator>();
        startPosition = transform.position;
    }

    // Back to the spawn area in the Normal state (used when PacStudent dies)
    public void ResetGhost()
    {
        transform.position = startPosition;
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
