using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles enemy movement along generated paths with smart pathfinding.
/// Features intelligent path recovery when enemies are knocked off their intended route.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    PathManager pathManager;
    List<SubCell> pathFromSpawner => pathManager?.path;
    private int currentPathIndex = 0;
    private bool isMoving = false;

    public float speed = 2f;
    public float speedWhenAttacking = 0.3f;
    public Transform target;
    public float pathRecoveryDistance = 2.0f;

    // Start method removed - initialization handled in Setup()

    // Update is called once per frame
    void Update()
    {
        if (isMoving && pathFromSpawner != null && pathFromSpawner.Count > 0)
        {
            MoveAlongPath();
        }
    }

    /// <summary>
/// Initializes enemy movement with path data from spawner.
/// Sets up initial position and begins movement along the assigned path.
/// </summary>
    public void Setup()
    {
        pathManager = GetComponent<EnemyManager>().pathManager;
        if (pathManager != null && pathManager.path != null && pathManager.path.Count > 0)
        {
            currentPathIndex = 0;
            isMoving = true;
            // Set initial position to first path point
            transform.position = pathManager.path[0].worldPosition;
        }
    }

    /// <summary>
/// Handles per-frame movement along the assigned path.
/// Includes smart pathfinding to recover from being knocked off path.
/// Adjusts speed based on attack state.
/// </summary>
    public void MoveAlongPath()
    {
        // Check if enemy has reached the end of the path
        if (currentPathIndex >= pathManager.path.Count)
        {
            isMoving = false;
            return;
        }

        // Get the current target position (with smart pathfinding)
        Vector3 targetPosition = GetTargetPosition();
        
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

    // Smart pathfinding to avoid backtracking
    private Vector3 GetTargetPosition()
    {
        // Check if we have a valid path and current index
        if (pathManager == null || pathManager.path == null || pathManager.path.Count == 0 || currentPathIndex >= pathManager.path.Count)
        {
            return transform.position; // Return current position if no valid path
        }

        // Get the current target position (default behavior)
        Vector3 currentTargetPosition = pathManager.path[currentPathIndex].worldPosition;
        float currentTargetDistance = Vector3.Distance(transform.position, currentTargetPosition);

        if(currentTargetDistance < pathRecoveryDistance)    //if the enemy is within the path recovery distance, return the current target position
        {
            return currentTargetPosition;
        }
        
        // Find the closest path subcell that's ahead of current path index
        Vector3 closestPosition = currentTargetPosition;
        float closestDistance = currentTargetDistance;
        int closestIndex = currentPathIndex;
        
        // Search through path points ahead of current index
        for (int i = currentPathIndex; i < pathManager.path.Count; i++)
        {
            Vector3 pathPosition = pathManager.path[i].worldPosition;
            float pathDistance = Vector3.Distance(transform.position, pathPosition);
            
            // Check if this path point is closer than our current closest
            if (pathDistance < closestDistance)
            {
                closestPosition = pathPosition;
                closestDistance = pathDistance;
                closestIndex = i;
            }
        }
        
        // If the closest position is less than 2 times the current target's distance, use it
        if (closestDistance < (2f * currentTargetDistance))
        {
            // Update currentPathIndex to the closest valid index to avoid walking backwards
            currentPathIndex = closestIndex;
            return closestPosition;
        }
        
        // Otherwise, return the current target position
        return currentTargetPosition;
    }

}
