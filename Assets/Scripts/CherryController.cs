using System.Collections;
using UnityEngine;

public class CherryController : MonoBehaviour
{
    [SerializeField] private GameObject cherryPrefab;
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private float spawnDelay = 5f;
    [SerializeField] private float speed = 2.5f;   // units per second

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnDelay);
            yield return StartCoroutine(MoveCherry());
        }
    }

    private IEnumerator MoveCherry()
    {
        Camera cam = Camera.main;

        Vector3 centre = new Vector3(
            (levelGenerator.Columns - 1) / 2f,
            -(levelGenerator.Rows - 1) / 2f,
            0f);

        // Start just outside the level map, at a random angle around the centre
        float levelRadius = Mathf.Sqrt(levelGenerator.Columns * levelGenerator.Columns
                                       + levelGenerator.Rows * levelGenerator.Rows) / 2f + 1f;

        // Finish on the opposite side, beyond the camera view
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float viewRadius = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + 1f;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

        Vector3 start = centre + direction * levelRadius;
        Vector3 end = centre - direction * Mathf.Max(viewRadius, levelRadius);   // passes through the centre

        GameObject cherry = Instantiate(cherryPrefab, start, Quaternion.identity);

        float duration = Vector3.Distance(start, end) / speed;
        float elapsed = 0f;

        while (elapsed < duration && cherry != null)
        {
            elapsed += Time.deltaTime;
            cherry.transform.position = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        if (cherry != null)
            Destroy(cherry);
    }
}
