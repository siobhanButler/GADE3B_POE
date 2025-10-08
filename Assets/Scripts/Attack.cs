using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Required Components
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(SphereCollider))]

public class Attack : MonoBehaviour
{
    [Header("Attack Components")]
    public Rigidbody rigidBody;
    public BoxCollider hitBox;
    public SphereCollider rangeCollider;

    [Header("Attack Settings")]
    public float attackDamage = 2f;
    public float rangeRadius = 30f;
    public float attackSpeed = 1f;
    public List<string> attackableTags;
    
    [Header("Attack Visuals")]
    public GameObject projectilePrefab;     //Must have collider and rigidbody
    public Transform firePoint;             //empty game object fire point
    public float projectileSpeed = 800f;
    public float rotationSpeed = 5f;
    public float maxProjectileLifetime = 10f; // Safety net - maximum time projectiles can exist 

    [Header("Attack State")]
    public bool isAttacking = false;
    
    private List<Collider> targetsInRange = new List<Collider>();
    private Collider currentTarget;
    private Coroutine attackRoutine; 
    

    // Start method removed - initialization handled in Setup()

    // Update is called once per frame
    void Update()
    {
        if(!IsTargetValid(currentTarget))   //if target is invalid
        {
            StopAttack();
            StartAttack();
        }
    }

    public void Setup(Rigidbody p_rigidBody, BoxCollider p_boxCollider, SphereCollider p_sphereCollider)
    {
        rigidBody = p_rigidBody;  //rigidbody is required for onTriggerEnter to work

        hitBox = p_boxCollider;
        
        rangeCollider = p_sphereCollider;
        rangeCollider.radius = rangeRadius;
        rangeCollider.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (attackableTags.Contains(other.tag) && IsRangeIntersectingWithHitbox(other))  //is it a valid target
        {
            targetsInRange.Add(other);  //add as a target

            StartAttack();
        }   
    }

    void OnTriggerExit(Collider other)
    {
        if (targetsInRange.Contains(other))
        {
            targetsInRange.Remove(other);   //Remove other from list

            if (other == currentTarget)     //is the target the one exiting --> Stop attacking
            {
                StopAttack();
            }       
        }
    }

    void StartAttack()
    {
            if(isAttacking == false && targetsInRange.Count > 0)    //start attacking if not already
            {
                currentTarget = GetBestTarget();
                attackRoutine = StartCoroutine(AttackLoop());
                isAttacking = true;
            }
    }

    void StopAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);   //stop attacking
        }
        isAttacking = false;
        currentTarget = null;           //clear current target  

        StartAttack();
    }

    void RangeAttack(Collider other)
    {
        Debug.Log(this.name + " is attacking " + other.name);
        
        // Handle attack visuals (rotation and projectile)
        AttackVisuals();
        
        // Apply damage
        Health otherHealth = other.GetComponent<Health>();
        if (otherHealth != null)
        {        
            otherHealth.TakeDamage(attackDamage);
        }
    }

    IEnumerator AttackLoop()
    {
        while (currentTarget != null)
        {
            RangeAttack(currentTarget);
            yield return new WaitForSeconds(attackSpeed);
        }

        //If currentTarget is null (aka dead) so coroutine will stop --> stop attacking and pick new target if available
        isAttacking = false;

        if (targetsInRange.Count > 0)   //if there are still targets in range --> start attacking new target
        {
            currentTarget = targetsInRange[0];  //set new target (no longer null, so coroutine will continue)
            isAttacking = true;
        }
    }

    bool IsRangeIntersectingWithHitbox(Collider target)
    {
        // Get the target's hitbox (assuming it has a BoxCollider for hitbox)
        BoxCollider targetHitbox = target.GetComponent<BoxCollider>();
        if (targetHitbox == null)
        {
            // If no BoxCollider, use the target's main collider
            targetHitbox = target as BoxCollider;
        }
        
        if (targetHitbox == null)
        {
            return false; // No hitbox found
        }
        
        // Check if the range collider bounds intersect with the target's hitbox bounds
        return rangeCollider.bounds.Intersects(targetHitbox.bounds);
    }

    // Check if a target is still valid (alive and in range)
    bool IsTargetValid(Collider target)
    {
        if (target == null)
            return false;

        // Check if target still exists and has health
        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth == null || targetHealth.currentHealth <= 0)
            return false;

        // Check if target is still in range
        if (!IsRangeIntersectingWithHitbox(target))
            return false;

        return true;
    }

    Collider GetBestTarget()
    {
        if (targetsInRange.Count == 0)
            return null;

        Collider bestTarget = null;
        float closestDistance = float.MaxValue;

        // Remove invalid targets first
        targetsInRange.RemoveAll(target => !IsTargetValid(target));

        foreach (Collider target in targetsInRange)
        {
            if (IsTargetValid(target))
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = target;
                }
            }
        }

        return bestTarget;
    }

    private void AttackVisuals()
    {
        if (currentTarget == null) return;
        
        // Rotate towards target
        RotateTowardsTarget();
        
        // Spawn and launch projectile
        SpawnProjectile();
    }
    
    private void RotateTowardsTarget()
    {
        if (currentTarget == null) return;
        
        // Calculate direction to target
        Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
        
        // Calculate rotation to look at target
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        
        // Smoothly rotate towards target
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
    
    private void SpawnProjectile()
    {
        if (projectilePrefab == null || currentTarget == null) return;
        
        // Determine spawn position
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        
        // Instantiate projectile
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        
        // Calculate direction to target
        Vector3 direction = (currentTarget.transform.position - spawnPosition).normalized;
        
        // Set projectile rotation to face target
        projectile.transform.rotation = Quaternion.LookRotation(direction);
        
        // Calculate travel time based on distance and speed
        float distanceToTarget = Vector3.Distance(spawnPosition, currentTarget.transform.position);
        float travelTime = distanceToTarget / projectileSpeed;
        
        // Use the smaller of calculated travel time or max lifetime as safety net
        float actualLifetime = Mathf.Min(travelTime, maxProjectileLifetime);
        
        // Add velocity to projectile (assuming it has a Rigidbody)
        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
        if (projectileRb != null)
        {
            projectileRb.linearVelocity = direction * projectileSpeed;
            // Start coroutine to destroy projectile when it reaches target or after calculated travel time
            StartCoroutine(DestroyProjectileOnReach(projectile, currentTarget.transform.position, actualLifetime));
        }
        else
        {
            // If no Rigidbody, use a script to move the projectile
            //StartCoroutine(MoveProjectile(projectile, direction, currentTarget.transform.position, actualLifetime));
        }
    }
/*    
    private IEnumerator MoveProjectile(GameObject projectile, Vector3 direction, Vector3 targetPosition, float travelTime)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = projectile.transform.position;
        
        while (projectile != null && elapsedTime < travelTime)
        {
            // Move projectile
            projectile.transform.Translate(direction * projectileSpeed * Time.deltaTime, Space.World);
            
            // Check if projectile has reached target (within 0.5 units)
            float currentDistance = Vector3.Distance(projectile.transform.position, targetPosition);
            if (currentDistance < 0.5f)
            {
                Destroy(projectile);
                yield break;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Destroy projectile if travel time exceeded
        if (projectile != null)
        {
            Destroy(projectile);
        }
    }
 */   
    private IEnumerator DestroyProjectileOnReach(GameObject projectile, Vector3 targetPosition, float travelTime)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = projectile.transform.position;
        
        while (projectile != null && elapsedTime < travelTime)
        {
            // Check if projectile has reached target (within 0.5 units)
            float currentDistance = Vector3.Distance(projectile.transform.position, targetPosition);
            if (currentDistance < 0.5f)
            {
                Destroy(projectile);
                yield break;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Destroy projectile if travel time exceeded
        if (projectile != null)
        {
            Destroy(projectile);
        }
    }
}
