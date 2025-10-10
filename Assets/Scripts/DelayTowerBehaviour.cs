using UnityEngine;

public class DelayTowerBehaviour : CustomBehaviour
{
    public float delayDuration = 3.0f;   // Duration to delay the enemy
    public float speedModifier = 0.5f;   // Strength of the delay effect (0.0 - 1.0)

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected override void AttackBehaviour(ObjectManager target, float damage)
    {
        Debug.Log($"DelayTowerBehaviour AttackBehaviour(): {this.name} is delaying target {target.gameObject.name} for {delayDuration} seconds.");
        target.AddStatus(StatusEffectType.Slowed, delayDuration, speedModifier);

        //TODO: swap attack target to a new target that isnt the one that was just attacked
        //Attack attack = GetComponent<Attack>();
        //attack.targetsInRange.Remove(attack.currentTarget);
        //attack.GetBestTarget();
        //attack.StopAttack();
    }

    protected override void DamageBehaviour(ObjectManager target, float damage)
    {
        throw new System.NotImplementedException();
    }
}
