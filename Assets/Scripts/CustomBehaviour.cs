using UnityEngine;

public abstract class CustomBehaviour : MonoBehaviour
{
    [SerializeField] protected BehaviourType behaviourType;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ExecuteCustomBehaviour(ObjectManager target, float damage)
    {
        if (target == null)
        {
            Debug.LogWarning("CustomBehaviour DoCustomBehaviour(): Target is null.");
            return;
        }

        switch (behaviourType)
        {
            case BehaviourType.None:
                return; // No behaviour to execute
            case BehaviourType.Attack:
                AttackBehaviour(target, damage);
                break;
            case BehaviourType.Damage:
                DamageBehaviour(target, damage);
                break;
            case BehaviourType.Other:
                // Implement other behaviour if needed
                break;
            default:
                Debug.LogWarning("CustomBehaviour DoCustomBehaviour(): No behaviour type set or unrecognized type.");
                break;
        }
    }

    protected abstract void AttackBehaviour(ObjectManager target, float damage);     //beahviour that happens when attacking

    protected abstract void DamageBehaviour(ObjectManager target, float damage);     //behaviour that happens when damaged
}

public enum BehaviourType
{
    None,
    Attack,
    Damage,
    Other
}