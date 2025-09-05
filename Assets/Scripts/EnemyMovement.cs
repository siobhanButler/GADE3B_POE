using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class EnemyMovement : MonoBehaviour
{
    List<SubCell> pathFromSpawner;
    private int currentPathIndex = 0;
    private bool isMoving = false;

    public float speed = 2f;
    public float speedWhenAttacking = 0.3f;
    public Transform target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving && pathFromSpawner != null && pathFromSpawner.Count > 0)
        {
            MoveAlongPath();
        }
    }

    public void Setup()
    {
        pathFromSpawner = GetComponent<EnemyManager>().pathFromSpawner;
        if (pathFromSpawner != null && pathFromSpawner.Count > 0)
        {
            currentPathIndex = 0;
            isMoving = true;
            // Set initial position to first path point
            transform.position = pathFromSpawner[0].worldPosition;
        }
    }

    public void MoveAlongPath()
    {
        // Check if enemy has reached the end of the path
        if (currentPathIndex >= pathFromSpawner.Count)
        {
            isMoving = false;
            return;
        }

        // Get the current target position
        Vector3 targetPosition = pathFromSpawner[currentPathIndex].worldPosition;
        
        // Check if enemy is attacking and adjust speed accordingly
        Attack attackComponent = GetComponent<Attack>();
        float currentSpeed = speed;
        if (attackComponent != null && attackComponent.isAttacking)
        {
            currentSpeed = speedWhenAttacking; // Slow down when attacking
        }
        else
        {
            currentSpeed = speed;
        }
        
        // Move towards the current target
        float step = currentSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
        
        // Check if we've reached the current target
        if (Vector3.Distance(transform.position, targetPosition) < 0.7f)
        {
            // Move to the next path point
            currentPathIndex++;
        }
    }

}
