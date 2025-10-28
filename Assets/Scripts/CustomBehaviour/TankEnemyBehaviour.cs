using System.Collections.Generic;
using UnityEngine;

public class TankEnemyBehaviour : CustomBehaviour
{
    public float shieldRange;
    public float shieldAmount;        //% modifyer of how much of damage is used to heal (0.0 - 1.0)
    public SphereCollider shieldCollider;
    public List<string> shieldableTags;
    List<Health> shieldableTargets = new List<Health>();
    Health tankHealth;
    public bool debugShieldingLogs = false;
    
    void OnDisable()
    {
        // Unsubscribe from all tracked targets on disable to prevent leaks
        if (shieldableTargets != null)
        {
            for (int i = 0; i < shieldableTargets.Count; i++)
            {
                Health h = shieldableTargets[i];
                if (h != null) h.OnDamageTaken -= OnShieldableTargetDamaged;
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (shieldCollider == null)  shieldCollider = GetComponent<SphereCollider>();
        if (shieldCollider == null)
            shieldCollider = gameObject.AddComponent<SphereCollider>();

        shieldCollider.radius = shieldRange;
        shieldCollider.isTrigger = true;

        tankHealth = GetComponent<Health>();
    }

    // Update not needed for this behaviour

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // filter by allowed tags
        if (shieldableTags != null && shieldableTags.Count > 0 && !shieldableTags.Contains(other.tag))
            return;

        // get Health from collider or parent
        Health health = other.GetComponent<Health>();
        if (health == null)
            health = other.GetComponentInParent<Health>();
        if (health == null) return;
        if (health == tankHealth) return; // do not process self

        if (!shieldableTargets.Contains(health))
        {
            shieldableTargets.Add(health);
            health.OnDamageTaken += OnShieldableTargetDamaged;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other == null) return;

        Health health = other.GetComponent<Health>();
        if (health == null)
            health = other.GetComponentInParent<Health>();
        if (health == null) return;

        if (shieldableTargets.Contains(health))
        {
            health.OnDamageTaken -= OnShieldableTargetDamaged;
            shieldableTargets.Remove(health);
        }
    }

    void OnShieldableTargetDamaged(float damageTaken, Health damagedHealth)
    {
        ShieldTarget(damagedHealth, damageTaken);
    }

    void ShieldTarget(Health targetHealth, float incomingDamage)
    {
        if (debugShieldingLogs)
            Debug.Log($"TankEnemyBehaviour ShieldTarget(): Shielding {targetHealth.gameObject.name} for {incomingDamage * shieldAmount} damage, transferring to {this.name}.");

        if (targetHealth == null) return;
        if (targetHealth.isDead) return;

        float shieldValue = incomingDamage * shieldAmount;
        // Health.TakeDamage now applies damage before raising events; refund the shielded portion to the target
        targetHealth.Heal(shieldValue);
        // Transfer the shielded portion to the tank
        tankHealth.ApplyRawDamage(shieldValue);
    }

    protected override void AttackBehaviour(ObjectManager target, float damage)
    {
        throw new System.NotImplementedException();
    }

    protected override void DamageBehaviour(ObjectManager target, float damage)
    {
        throw new System.NotImplementedException();
    }
}
