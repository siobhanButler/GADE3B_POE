using System.Collections.Generic;
using UnityEngine;

public abstract class ObjectManager : MonoBehaviour
{
    //All objects such as Towers and Enemies will inherit from this class

    [Header("Object Components")]
    public Rigidbody rigidBody;
    public BoxCollider hitBox;
    public SphereCollider rangeCollider;
    public GameObject objectUI;
    public ObjectUIManager uiManager;
    public Health health;
    public Attack attack;

    public float maxHealth;
    public float attackDamage = 2f;
    public float attackRadius = 30f;
    public float attackSpeed = 1f;
    [Min(0f)]
    public float spawnLikelihood = 1f; // weighting for enemy spawn selection (used for enemies)

    public List<StatusEffect> currentStatusEffects;
    private Coroutine statusEffectsCoroutine;

    public GameObject objectUiPrefab;  //assign in inspector

    public string objectTag;

    [Min(0)]
    public int cost; // unified value for enemy reward and tower purchase cost (serialized for prefabs)
    public float specialityModifier;

    // Start and Update methods removed - functionality handled in derived classes

    private void Awake()
    {
        //cost = Mathf.RoundToInt(((attackDamage * attackSpeed * attackRadius) + maxHealth) * specialityModifier +1);
    }

    // Keep prefab and scene instances updated in the editor
    private void OnValidate()
    {
        cost = Mathf.RoundToInt(((attackDamage * attackSpeed * attackRadius) + maxHealth) * (specialityModifier + 1));
        if (cost < 0) cost = 0;
        if (spawnLikelihood < 0f) spawnLikelihood = 0f;
    }

    public void Setup()
    {
        Debug.Log($"ObjectManager Setup() called for {gameObject.name}");

        // Acquire or create object UI safely
        if (objectUI == null)
        {
            var uiManagerComponent = GetComponentInChildren<ObjectUIManager>();
            if (uiManagerComponent != null)
            {
                uiManager = uiManagerComponent;
                objectUI = uiManagerComponent.gameObject; // assign the GameObject itself
            }
            else
            {
                Vector3 uiPosition = transform.position + new Vector3(0, 2, 0);
                objectUI = Instantiate(objectUiPrefab, uiPosition, Quaternion.identity);
            }
        }
        uiManager = objectUI.GetComponent<ObjectUIManager>();
        if(uiManager == null)
        {
            Debug.LogError("ObjectManager Setup(): ObjectUIManager component not found on the instantiated objectUI for " + name);
        }
        uiManager.nameText.text = objectTag;
        uiManager.UpdateHealthBar(1, 1); // Initialize health bar to full

        //Get or add required components
        rigidBody = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
        hitBox = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();
        rangeCollider = GetComponent<SphereCollider>() ?? gameObject.AddComponent<SphereCollider>();

        health = GetComponent<Health>() ?? gameObject.AddComponent<Health>();
        health.Setup(uiManager, maxHealth);
        attack = GetComponent<Attack>() ?? gameObject.AddComponent<Attack>();
        attack.Setup(rigidBody, hitBox, rangeCollider, attackDamage, attackSpeed, attackRadius);

        this.tag = objectTag;  //set tag to tower

        currentStatusEffects = new List<StatusEffect>();
    }

    public abstract void OnDeath();

    public void AddStatus(StatusEffectType newStatusType, float newDuration, float newEffectStrength)    //called by external classes
    {
        //if statuseffect of that type already eists, just refresh the duration
        foreach(StatusEffect status in currentStatusEffects)
        {
            if(status.type == newStatusType)
            {
                status.duration = newDuration;
                return;
            }
        }
        //If no status effects of this type were found, add the new status effect
        StatusEffect newStatusEffect = new StatusEffect(newStatusType, newDuration, newEffectStrength, this);
        currentStatusEffects.Add(newStatusEffect);

        //start coroutine if not already started
        if (currentStatusEffects == null || currentStatusEffects.Count == 0) return;
        if (statusEffectsCoroutine == null) statusEffectsCoroutine = StartCoroutine(StatusEffectsTickLoop()); 
    }

    void ApplyStatusEffects()   //called every 1 sec
    {
        // Iterate backwards to allow safe removal during iteration (instead of foreach)
        for (int i = currentStatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect status = currentStatusEffects[i];
            if (status.duration > 0f)
            {
                status.ApplyEffect();
                status.duration -= 1f;   // reduce duration by 1 sec
            }
            else
            {
                status.RemoveStatus();
                currentStatusEffects.RemoveAt(i); // drop reference so GC can collect
            }
        }

        // Stop ticking if no effects remain
        if (currentStatusEffects.Count == 0 && statusEffectsCoroutine != null)
        {
            StopCoroutine(statusEffectsCoroutine);
            statusEffectsCoroutine = null;
        }
    }

    void EnsureStatusTicking()  //start coroutine if not already ticking
    {
        
    }

    System.Collections.IEnumerator StatusEffectsTickLoop()
    {
        var wait = new WaitForSeconds(1f);
        while (currentStatusEffects != null && currentStatusEffects.Count > 0)
        {
            ApplyStatusEffects();
            yield return wait;
        }
        statusEffectsCoroutine = null;
    }

    void OnDisable()
    {
        if (statusEffectsCoroutine != null)
        {
            StopCoroutine(statusEffectsCoroutine);
            statusEffectsCoroutine = null;
        }
    }
}


