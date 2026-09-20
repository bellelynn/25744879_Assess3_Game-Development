using UnityEngine;

public class PacStudentMovement : MonoBehaviour
{
    [SerializeField]
    private Transform[] points;

    [SerializeField]
    private float speed = 2.0f;

    private int currentPoint = 0;

    private Vector3 startPosition;
    private Vector3 endPosition;

    private float journeyLength;
    private float journeyTime;
    private float timer;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (points == null || points.Length == 0)
        {
            Debug.LogError("Movement points have not been assigned.");
            enabled = false;
            return;
        }

        if (speed <= 0f)
        {
            Debug.LogError("Speed must be greater than 0.");
            enabled = false;
            return;
        }

        // Start PacStudent at Point0
        transform.position = points[0].position;

        currentPoint = 0;

        StartNextSegment();
    }

    void Update()
    {
        timer += Time.deltaTime;

        float t = timer / journeyTime;

        t = Mathf.Clamp01(t);

        transform.position =
            startPosition + (endPosition - startPosition) * t;

        // Reached the next point
        if (t >= 1.0f)
        {
            currentPoint = (currentPoint + 1) % points.Length;

            StartNextSegment();
        }
    }

    void StartNextSegment()
    {
        startPosition = transform.position;

        int nextPoint = (currentPoint + 1) % points.Length;

        endPosition = points[nextPoint].position;

        Vector3 direction = endPosition - startPosition;

        if (animator != null)
        {
            if (direction.x > 0)
            {
                animator.Play("Right");
            }
            else if (direction.x < 0)
            {
                animator.Play("Left");
            }
            else if (direction.y > 0)
            {
                animator.Play("Up");
            }
            else if (direction.y < 0)
            {
                animator.Play("Down");
            }
        }

        //Keep the same speed on 4 side
        journeyLength = Vector3.Distance(startPosition, endPosition);
        journeyTime = journeyLength / speed;

        timer = 0f;
    }
}