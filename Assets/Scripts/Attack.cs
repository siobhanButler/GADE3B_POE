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

    [Header("Attack State")]
    public bool isAttacking = false;
    
    private List<Collider> targetsInRange = new List<Collider>();
    private Collider currentTarget;
    private Coroutine attackRoutine; 
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
        if (attackableTags.Contains(other.tag))  //is it a valid target
        {
            targetsInRange.Add(other);  //add as a target

            if(isAttacking == false)    //start attacking if not already
            {
                currentTarget = targetsInRange[0];
                attackRoutine = StartCoroutine(AttackLoop());
                isAttacking = true;
            }
        }   
    }

    void OnTriggerExit(Collider other)
    {
        if (targetsInRange.Contains(other))
        {
            targetsInRange.Remove(other);   //Remove other from list

            if (other == currentTarget)     //is the target the one exiting --> Stop attacking
            {
                StopCoroutine(attackRoutine);   //stop attacking
                isAttacking = false;
                currentTarget = null;           //clear current target  

                if (targetsInRange.Count > 0)   //if there are still targets in range --> start attacking new target
                {
                    currentTarget = targetsInRange[0];  //set new target
                    attackRoutine = StartCoroutine(AttackLoop()); //start attacking new target
                    isAttacking = true;
                }
            }       
        }
    }

    void RangeAttack(Collider other)
    {
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
    }
}
