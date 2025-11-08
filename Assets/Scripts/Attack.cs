using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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
    public float attackSpeed = 1f;      //seconds between each attack
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
        /* Dont check in update, as it is checked in the coroutine
        if(!IsTargetValid(currentTarget))   //if target is invalid
        {
            StopAttack();
            StartAttack();
        }
        */

        //if not attacking, keep tring to find targets
        if (!isAttacking)
        {
            RefreshAttack();
        }
    }

    public void Setup(Rigidbody p_rigidBody, BoxCollider p_boxCollider, SphereCollider p_sphereCollider, float pAttackDamage, float pAttackSpeed, float pAttackRadius)
    {
        rigidBody = p_rigidBody;  //rigidbody is required for onTriggerEnter to work

        hitBox = p_boxCollider;

        // Apply incoming configuration
        attackDamage = pAttackDamage;
        attackSpeed = 1 / pAttackSpeed;     //convert pAttackSpeed (attacks per second) to attackSpeed (seconds per/between each attack)
        rangeRadius = pAttackRadius;

        rangeCollider = p_sphereCollider;
        rangeCollider.radius = rangeRadius;
        rangeCollider.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Attack OnTriggerEnter(): " + this.name + " detected " + other.name);
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
                StartAttack();
            }       
        }
    }

    public void RefreshAttack()  //refresh list of targets in range
    {
        //Debug.Log("Attack RefreshAttack(): " + this.name + " is refreshing its attack");
        targetsInRange.Clear();   //clear current list of targets
        Collider[] colliders = Physics.OverlapSphere(transform.position, rangeRadius);
        foreach (Collider other in colliders)
        {
            if (attackableTags.Contains(other.tag) && IsRangeIntersectingWithHitbox(other))  //is it a valid target
            {
                targetsInRange.Add(other);  //add as a target
            }
        }

        if(targetsInRange.Count != 0)
            StartAttack();   //start the attack with the new list
    }

    void StartAttack()  //get new target and start attacking (if not already attacking)
    {
        if (isAttacking == false && targetsInRange.Count > 0)    //start attacking if not already
        {
            currentTarget = GetBestTarget();
            if (currentTarget == null)
            {
                isAttacking = false;
                return;
            }
            attackRoutine = StartCoroutine(AttackLoop());
            isAttacking = true;

            //Debug.Log("Attack StartAttack(): " + this.name + " is starting to attack " );
        }
    }

    public void StopAttack()       //stop the attack
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);   //stop attacking
            //Debug.Log("Attack StopAttack(): " + this.name + " is stopping its attack");
        }
        isAttacking = false;
        currentTarget = null;           //clear current target  

        //StartAttack();
    }

    void RangeAttack(Collider other)
    {
        // Handle attack visuals (rotation and projectile)
        AttackVisuals();
        
        // Apply damage
        Health otherHealth = other.GetComponent<Health>();
        if (otherHealth != null)
        {        
            otherHealth.TakeDamage(attackDamage);
            //Debug.Log("Attack RangeAttack(): " + this.name + " attacked " + other.name + " for " + attackDamage + " damage. Remaining Health: " + otherHealth.currentHealth);

            // Execute custom behaviour if attached
            CustomBehaviour customBehaviour = GetComponent<CustomBehaviour>();
            if (customBehaviour != null)
            {
                customBehaviour.ExecuteCustomBehaviour(other.GetComponent<ObjectManager>(), attackDamage);
            }
        } 
    }

    IEnumerator AttackLoop()
    {
        while (IsTargetValid(currentTarget))    //while the target is valid, continue
        {
            // If focusing the MainTower and other valid targets exist, switch off the main tower
            if (currentTarget != null && currentTarget.CompareTag("MainTower"))
            {
                Collider alternative = GetBestNonMainTowerTarget();
                if (alternative != null)
                {
                    currentTarget = alternative;
                }
            }
            RangeAttack(currentTarget);
            yield return new WaitForSeconds(attackSpeed);
        }

        // Attempt to switch to another valid target if available, keep attacking state if possible
        currentTarget = null;
        // Clean up any invalid/stale colliders
        targetsInRange.RemoveAll(target => !IsTargetValid(target));

        Collider nextTarget = GetBestTarget();
        if (nextTarget != null)
        {
            currentTarget = nextTarget;
            // Keep isAttacking true and immediately continue attacking
            attackRoutine = StartCoroutine(AttackLoop());
        }
        else
        {
            // No targets available, fully stop
            StopAttack();
        }

        /*
        isAttacking = false;

        if (targetsInRange.Count > 0)   //if there are still targets in range --> start attacking new target
        {
            currentTarget = targetsInRange[0];  //set new target (no longer null, so coroutine will continue)
            isAttacking = true;
        }
        */
    }

    // Prefer any valid non-MainTower target if available (closest wins)
    Collider GetBestNonMainTowerTarget()
    {
        Collider bestTarget = null;
        float closestDistance = float.MaxValue;

        // Clean up invalid entries first
        targetsInRange.RemoveAll(target => !IsTargetValid(target));

        for (int i = 0; i < targetsInRange.Count; i++)
        {
            Collider target = targetsInRange[i];
            if (target == null) continue;
            if (target.CompareTag("MainTower")) continue; // skip main tower
            if (!IsTargetValid(target)) continue;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = target;
            }
        }

        return bestTarget;
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

    // Check if a target is still valid (correct tag, alive and in range)
    bool IsTargetValid(Collider target)
    {
        if (target == null) //dead target
            return false;

        if (attackableTags.Contains(target.tag) == false) //wrong tag
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
        
        // Calculate direction to target (ignore Y component for 2D-style rotation)
        Vector3 direction = (currentTarget.transform.position - transform.position);
        direction.y = 0; // Remove Y component to prevent pitch rotation
        direction = direction.normalized;
        
        // Calculate rotation to look at target (Y-axis only)
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        
        // Preserve current X and Z rotations, only change Y rotation
        Vector3 currentEuler = transform.rotation.eulerAngles;
        Vector3 targetEuler = targetRotation.eulerAngles;
        Vector3 newEuler = new Vector3(currentEuler.x, targetEuler.y, currentEuler.z);
        
        // Smoothly rotate towards target (Y-axis only)
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(newEuler), rotationSpeed * Time.deltaTime);
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
