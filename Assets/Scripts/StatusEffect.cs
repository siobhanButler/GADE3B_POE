using UnityEngine;

public class StatusEffect
{
    public StatusEffectType type;
    public float duration;
    float effectStrength;
    ObjectManager target;
    bool isApplied;
    float originalSpeed;

    public StatusEffect(StatusEffectType p_status, float p_duration, float p_effectStrength, ObjectManager p_target)
    {
        type = p_status;
        duration = p_duration;
        target = p_target;
        effectStrength = p_effectStrength;
    }

    public void ApplyEffect()
    {
        switch (type)
        {
            case StatusEffectType.None:
                // No status effect to apply
                break;
            case StatusEffectType.Slowed:
                // Implement slowing effect logic here
                EnemyMovement enemyMovement = target != null ? target.GetComponent<EnemyMovement>() : null;
                if (enemyMovement != null && !isApplied)
                {
                    originalSpeed = enemyMovement.speed; // cache original once
                    enemyMovement.speed = originalSpeed * effectStrength; // apply slow once
                    isApplied = true;
                }
                else    //it would then presumably be a tower
                {
                    Attack attack = target != null ? target.GetComponent<Attack>() : null;
                    if (attack != null)
                    {
                        originalSpeed = attack.attackSpeed; // cache original once
                        attack.attackSpeed = originalSpeed / effectStrength; // apply slow once NEED TO TEST THIS
                        isApplied = true;
                    }
                }
                    break;
            // Add cases for other status effects as needed
            default:
                Debug.LogWarning("StatusEffect ApplyStatus(): Unrecognized status effect.");
                break;
        }
    }

    public void RemoveStatus()
    {
        if(target == null) return;

        switch (type)
        {
            case StatusEffectType.None:
                // No status effect to apply
                break;
            case StatusEffectType.Slowed:
                // Implement slowing effect logic here
                EnemyMovement enemyMovement = target.GetComponent<EnemyMovement>();
                if (enemyMovement != null && isApplied)
                {
                    enemyMovement.speed = originalSpeed; // restore on removal
                    isApplied = false;
                }
                break;
            // Add cases for other status effects as needed
            default:
                Debug.LogWarning("StatusEffect ApplyStatus(): Unrecognized status effect.");
                break;
        }
    }
}

public enum StatusEffectType
{
    None,
    Slowed,     //enemy-only status effect
    //Poisoned,
    //Shielded,
    //Stunned
}
