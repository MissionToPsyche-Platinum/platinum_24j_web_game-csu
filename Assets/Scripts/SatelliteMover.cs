using UnityEngine;

public class SatelliteMover : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 0.5f;
    public float leftBound = -15f;
    public float rightBound = 15f;
    public float slowBobAmount = 0.2f;
    public float bobSpeed = 1f;

    private float startY;

    void Start()
    {
        startY = transform.position.y;
    }

    void Update()
    {
        // Move horizontally
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);

        // Slow floating bob
        transform.position = new Vector3(
            transform.position.x,
            startY + Mathf.Sin(Time.time * bobSpeed) * slowBobAmount,
            transform.position.z
        );

        // Loop back when it goes completely off screen
        if (transform.position.x > rightBound)
        {
            transform.position = new Vector3(leftBound, transform.position.y, transform.position.z);
        }
    }
}
