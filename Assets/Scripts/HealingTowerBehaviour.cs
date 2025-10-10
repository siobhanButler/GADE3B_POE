using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class HealingTowerBehaviour : CustomBehaviour
{
    public float healRange;
    public float healAmount;        //% modifyer of how much of damage is used to heal (0.0 - 1.0)
    public SphereCollider healCollider;
    public List<string> healableTags;
    List<Health> healableTargets = new List<Health>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        healCollider = GetComponent<SphereCollider>();
        if (healCollider == null) 
            healCollider = gameObject.AddComponent<SphereCollider>();

        healCollider.radius = healRange;
        healCollider.isTrigger = true;
    }

    // Update not needed for this behaviour

    List<Health> SetHealableTargets()
    {
        healableTargets.Clear();

		// get all colliders within heal range
		Vector3 center = healCollider != null ? healCollider.transform.position : transform.position;
		float radius = healCollider != null ? healCollider.radius : healRange;
		Collider[] collidersInRange = Physics.OverlapSphere(center, radius);

		for (int i = 0; i < collidersInRange.Length; i++)
		{
			Collider collider = collidersInRange[i];
			if (collider == null) continue;

			// filter by allowed tags
			if (healableTags != null && healableTags.Count > 0 && !healableTags.Contains(collider.tag))
				continue;

			// try get Health component on this collider or its parents
			Health health = collider.GetComponent<Health>();
			if (health == null)
				health = collider.GetComponentInParent<Health>();

			if (health == null) continue;
			if (health.isDead) continue;
			if (health.currentHealth >= health.maxHealth) continue; // skip full health

			if (!healableTargets.Contains(health))
			{
				healableTargets.Add(health);
			}
		}

		// sort from lowest to highest current health
		healableTargets.Sort((a, b) => a.currentHealth.CompareTo(b.currentHealth));

		if (healableTargets.Count == 0)
			return null;

		return healableTargets;
    }

    protected override void AttackBehaviour(ObjectManager target, float damage)
    {
        if (target == null) return;
        if(SetHealableTargets() == null) return;

        Debug.Log($"HealingTowerBehaviour AttackBehaviour(): {this.name} is healing target {healableTargets[0].gameObject.name} for {damage * healAmount} health.");
        healableTargets[0].Heal(damage * healAmount);  //heal lowest-health target for a percentage of the damage dealt
    }

    protected override void DamageBehaviour(ObjectManager target, float damage)
    {
        throw new System.NotImplementedException();
    }
}
