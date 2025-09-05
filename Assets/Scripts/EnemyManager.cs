using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Enemy Components")]
    public Rigidbody rigidBody;
    public BoxCollider hitBox;
    public SphereCollider rangeCollider;
    public Canvas towerUI;
    public ObjectUIManager UIManager;
    public Health health;
    public Attack attack;
    public EnemyMovement movement;

    public Canvas canvasPrefab;  //assign in inspector

    public string enemyType = "Enemy";

    public List<SubCell> pathFromSpawner;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Get or add required components
        rigidBody = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
        hitBox = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();
        rangeCollider = GetComponent<SphereCollider>() ?? gameObject.AddComponent<SphereCollider>();

        towerUI = GetComponentInChildren<Canvas>() ?? Instantiate(canvasPrefab, transform);
        UIManager = towerUI.GetComponent<ObjectUIManager>();

        health = GetComponent<Health>() ?? gameObject.AddComponent<Health>();
        health.Setup(UIManager);
        attack = GetComponent<Attack>() ?? gameObject.AddComponent<Attack>();
        attack.Setup(rigidBody, hitBox, rangeCollider);
        movement = GetComponent<EnemyMovement>() ?? gameObject.AddComponent<EnemyMovement>();
        movement.Setup();

        this.tag = enemyType;  //set tag to tower
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
